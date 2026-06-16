using System.Collections; // Для IEnumerator
using System;
using UnityEngine;
using UnityEngine.Pool; // Для использования Object Pool

[RequireComponent(typeof(PlayerMovement))] // Убедимся, что PlayerMovement есть
public class PlayerShooting : MonoBehaviour
{
    [Header("Current Weapon Settings")]
    [SerializeField] private WeaponData initialWeaponData; // Ссылка на базовый SO, который НЕ будет меняться
    [HideInInspector] public WeaponData _runtimeWeaponData; // Эта копия будет меняться во время игры
    [SerializeField] private Transform firePoint; // Точка, откуда вылетают пули
    [SerializeField] private Transform cameraTransform; // Трансформ камеры для определения направления
    [SerializeField] private WeaponData initialWeaponDataForRuntime; // Для создания runtime-копии
    public bool isCritical; // Флаг крит урона

    [Header("Targeting")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private float targetingRadius = 15f; // Радиус поиска врагов
    [SerializeField] private float lostTargetDistance = 17f; // Если враг ушел дальше этого, теряем цель
    //[SerializeField] private GameObject targetPoint;

    [Header("Obstacle Settings")]
    [SerializeField] private LayerMask obstacleLayer; // Слой для стен, деревьев и т.д.

    private bool _isShooting = false; // Флаг состояния стрельбы
    private Transform _currentTarget; // Цель, которую мы атакуем в данный момент

    // Скорость поворота к противнику
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    // Пул пуль
    private ObjectPool<Bullet> _bulletPool;
    private Coroutine _shootingCoroutine; // Ссылка на корутину, чтобы можно было остановить

    // Анимация
    private Animator _animator;

    // В Awake создаем пул объектов
    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        if (initialWeaponData == null)
        {
            Debug.LogError("PlayerShooting: initialWeaponData не назначено!");
            enabled = false; // Отключаем скрипт, если нет данных об оружии
            return;
        }

        // Создаем runtime-копию WeaponData из исходного SO
        _runtimeWeaponData = Instantiate(initialWeaponData);
        

        // Инициализируем пул
        _bulletPool = new ObjectPool<Bullet>(
            createFunc: () => CreatePooledBullet(), // Как создать новую пулю
            actionOnGet: (bullet) => { }, // Что делать, когда достаем пулю (уже в Initialize)
            actionOnRelease: (bullet) => bullet.gameObject.SetActive(false), // Что делать, когда возвращаем пулю
            actionOnDestroy: (bullet) => Destroy(bullet.gameObject), // Что делать, когда пул решает удалить пулю
            collectionCheck: false, // Отключить проверку, т.к. это может быть дорого
            defaultCapacity: 20, // Начальная вместимость
            maxSize: 200 // Максимальное количество пуль в пуле
        );
    }

    //Создание пули для пула
    private Bullet CreatePooledBullet()
    {
        if (_runtimeWeaponData == null || _runtimeWeaponData.bulletPrefab == null)
        {
            Debug.LogError("PlayerShooting: Bullet Prefab is not assigned or weapon data is missing!");
            return null; // Возвращаем null, если что-то не так
        }
        Bullet bullet = Instantiate(_runtimeWeaponData.bulletPrefab);
        bullet.gameObject.SetActive(false); // Изначально деактивируем
        return bullet;
    }

    void Start()
    {
        // Создаем FirePoint как дочерний объект игрока
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0, 0.5f, 0.5f); // Слегка впереди и над центром
            firePoint = fp.transform;
        }
    }

    private void Update()
    {
        if (_runtimeWeaponData == null) return; // Не стреляем, если оружия нет

        // 1. Ищем или проверяем цель
        HandleTargeting();

        // 2. Если цель валидна — поворачиваемся к ней
        if (IsTargetValid(_currentTarget))
        {
            RotateTowardsTarget(_currentTarget.position);

            if (!_isShooting)
            {
                StartShooting();
            }
        }
        else if(_isShooting)
        {
            StopShooting();
            _animator.SetBool("Attack", false);
        }
    }

    // Устанавливаем направление чтобы персонаж смотрел в сторону ближайшего врага
    private void RotateTowardsTarget(Vector3 targetPosition)
    {
        // Вычисляем направление к цели
        Vector3 direction = targetPosition - transform.position;

        // Обнуляем Y, чтобы куб не наклонялся вверх/вниз, если враг чуть выше или ниже
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            // Создаем нужный поворот
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Плавно переходим от текущего поворота к целевому
            // Quaternion.Slerp — сферическая линейная интерполяция
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleTargeting()
    {
        // Проверяем текущую цель на "валидность"
        if (!IsTargetValid(_currentTarget) || Vector3.Distance(transform.position, _currentTarget.position) > lostTargetDistance)
        {
            _currentTarget = null;
        }
       
        _currentTarget = FindNearestValidEnemy();
    }

    public void StartShooting()
    {
        if (_isShooting) return; // Уже стреляем
        if (_shootingCoroutine != null) StopCoroutine(_shootingCoroutine);
        _isShooting = true; // Устанавливаем флаг, что стрельба идет
        _shootingCoroutine = StartCoroutine(ShootCoroutine());
    }
    public void StopShooting()
    {
        _isShooting = false; // Сбрасываем флаг
        if (_shootingCoroutine != null)
        {
            StopCoroutine(_shootingCoroutine);
            _shootingCoroutine = null;
        }
    }

    private IEnumerator ShootCoroutine()
    {
        while (_isShooting) // Цикл зависит от флага _isShooting
        {
            Shoot();
            yield return new WaitForSeconds(Math.Clamp(_runtimeWeaponData.baseFireRate, 0.3f, 10)); // Ждем перед следующим выстрелом
        }
    }

    // Вспомогательный метод для проверки: живой ли враг и можно ли в него стрелять
    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead) return false;

        // --- ПРОВЕРКА ЛИНИИ ВИДИМОСТИ ---
        Vector3 start = transform.position + Vector3.up * 1f; // Из груди игрока
        Vector3 end = target.position + Vector3.up * 1f;   // В грудь врага
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        // Пускаем луч. Если он попадет в препятствие раньше, чем долетит до врага — цель невалидна.
        if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance, obstacleLayer))
        {
            return false; // На пути стена
        }

        return true;
    }

    private void Shoot()
    {

        // КРИТИЧЕСКАЯ ПРОВЕРКА: прямо в момент выстрела проверяем всё еще раз
        if (!IsTargetValid(_currentTarget))
        {
            _currentTarget = null;
            return;
        }

        // ПРОВЕРКА ДИСТАНЦИИ: чтобы не стрелять в тех, кто за радиусом
        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        if (distanceToTarget > lostTargetDistance)
        {
            _currentTarget = null;
            return;
        }

        if (_runtimeWeaponData == null) return;

        _animator.SetBool("Attack", true);

        Vector3 direction = (new Vector3(_currentTarget.position.x, 1.5f, _currentTarget.position.z) - firePoint.position).normalized;

        for (int i = 0; i < _runtimeWeaponData.projectilesPerShot; i++)
        {
            // Получаем пулю из пула
            Bullet bullet = _bulletPool.Get();

            // Устанавливаем позицию
            bullet.transform.position = firePoint.position;

            // Если есть разброс, применяем его
            Vector3 spreadDirection = direction;
            int count = Math.Clamp(_runtimeWeaponData.projectilesPerShot, 1, 30);
            if (Math.Clamp(_runtimeWeaponData.bulletSpreadAngle, 0, 360) > 0)
            {
                if (count == 1)
                {
                    // одна пуля — центр
                    spreadDirection = Quaternion.Euler(0f, 0f, 0f) * direction;
                }
                else
                {
                    float step = Math.Clamp(_runtimeWeaponData.bulletSpreadAngle, 0, 360) / (count - 1); // шаг между снарядами
                    float start = -Math.Clamp(_runtimeWeaponData.bulletSpreadAngle, 0, 360) * 0.5f;      // угол первого снаряда 
                    float angle = start + i * step;
                    spreadDirection = Quaternion.Euler(0f, angle, 0f) * direction;
                }
            }

            // Устанавливаем направление
            bullet.transform.rotation = Quaternion.LookRotation(spreadDirection); // Чтобы пуля смотрела в сторону полета

            // Новая логика крита
            isCritical = false;
            float critMul = 1f;
            if (playerStats != null)
            {
                isCritical = playerStats.RollCritical();
                critMul = isCritical ? playerStats.critMultiplier : 1f;
            }

            // Инициализируем пулю с параметрами из WeaponData
            bullet.Initialize(
            spreadDirection,
            _runtimeWeaponData.baseProjectileSpeed,
            _runtimeWeaponData.baseDamage * playerStats.damageMultiplier,
            _runtimeWeaponData.bulletLifetime,
            _runtimeWeaponData.explosiveBullets,
            _runtimeWeaponData.explosionRadius,
            _runtimeWeaponData.ricochetCount,
            isCritical,
            critMul
            );
        }
    }

    public Transform FindNearestValidEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRadius, LayerMask.GetMask("Enemy"));
        Transform closest = null;
        float minDst = Mathf.Infinity;

        foreach (var col in colliders)
        {
            if (IsTargetValid(col.transform))
            {
                float dst = Vector3.Distance(transform.position, col.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    closest = col.transform;
                }
            }
        }
        return closest;
    }

    public void RefreshBulletPool()
    {
        // 1. Если пул уже существует, очищаем его
        if (_bulletPool != null)
        {
            // В Unity ObjectPool есть метод Clear(), который уничтожает все объекты в пуле
            _bulletPool.Clear();
        }

        // 2. Пересоздаем пул. Теперь createFunc будет использовать НОВЫЙ префаб из _runtimeWeaponData
        _bulletPool = new ObjectPool<Bullet>(
            createFunc: () => CreatePooledBullet(), // Этот метод берет префаб из _runtimeWeaponData
            actionOnGet: (bullet) => bullet.gameObject.SetActive(true),
            actionOnRelease: (bullet) => bullet.gameObject.SetActive(false),
            actionOnDestroy: (bullet) => Destroy(bullet.gameObject),
            defaultCapacity: 20,
            maxSize: 100
        );

        Debug.Log("Пул пуль обновлен на новый префаб: " + _runtimeWeaponData.bulletPrefab.name);
    }

    // Для визуализации радиуса поиска врагов в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetingRadius);
    }

    // Добавим публичный getter, чтобы UpgradeData мог получить _runtimeWeaponData
    public WeaponData GetCurrentRuntimeWeaponData()
    {
        return _runtimeWeaponData;
    }
}
