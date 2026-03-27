using System.Collections; // Для IEnumerator
using UnityEngine;
using UnityEngine.Pool; // Для использования Object Pool
using System.Linq; // Для Linq методов

[RequireComponent(typeof(PlayerMovement))] // Убедимся, что PlayerMovement есть
public class PlayerShooting : MonoBehaviour
{
    [Header("Current Weapon Settings")]
    [SerializeField] private WeaponData initialWeaponData; // Ссылка на базовый SO, который НЕ будет меняться
    private WeaponData _runtimeWeaponData; // Эта копия будет меняться во время игры
    [SerializeField] private Transform firePoint; // Точка, откуда вылетают пули
    [SerializeField] private Transform cameraTransform; // Трансформ камеры для определения направления

    [Header("Targeting")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private float targetingRadius = 15f; // Радиус поиска врагов
    [SerializeField] private float lostTargetDistance = 17f; // Если враг ушел дальше этого, теряем цель

    private bool _isShooting = false; // Флаг состояния стрельбы
    private Transform _currentTarget; // Цель, которую мы атакуем в данный момент

    //Скорость поворота к противнику
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    //Пул пуль
    private ObjectPool<Bullet> _bulletPool;
    private Coroutine _shootingCoroutine; // Ссылка на корутину, чтобы можно было остановить

    // В Awake создаем пул объектов
    private void Awake()
    {
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
            maxSize: 100 // Максимальное количество пуль в пуле
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

    private void FixedUpdate()
    {
        // 1. Ищем или проверяем цель
        HandleTargeting();

        // 2. Если цель валидна — поворачиваемся к ней
        if (IsTargetValid(_currentTarget))
        {
            RotateTowardsTarget(_currentTarget.position);

            if (!_isShooting) StartShooting();
        }
        else
        {
            if (_isShooting) StopShooting();
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

        // Если цели нет — ищем новую
        if (_currentTarget == null)
        {
            _currentTarget = FindNearestValidEnemy();
        }
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
            yield return new WaitForSeconds(_runtimeWeaponData.baseFireRate); // Ждем перед следующим выстрелом
        }
    }

    // Вспомогательный метод для проверки: живой ли враг и можно ли в него стрелять
    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead) return false;

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

        // ПРОВЕРКА ДИСТАНЦИИ: чтобы не стрелять в тех, кто за радиусом (решает проблему "одного выстрела")
        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
        if (distanceToTarget > lostTargetDistance)
        {
            _currentTarget = null;
            return;
        }

        if (_runtimeWeaponData == null) return;

        Vector3 direction = (_currentTarget.position - firePoint.position).normalized;

        for (int i = 0; i < _runtimeWeaponData.projectilesPerShot; i++)
        {
            // Получаем пулю из пула
            Bullet bullet = _bulletPool.Get();

            // Устанавливаем позицию
            bullet.transform.position = firePoint.position;

            // Если есть разброс, применяем его
            Vector3 spreadDirection = direction;
            if (_runtimeWeaponData.bulletSpreadAngle > 0)
            {
                spreadDirection = Quaternion.Euler(0, Random.Range(-_runtimeWeaponData.bulletSpreadAngle / 2f, _runtimeWeaponData.bulletSpreadAngle / 2f), 0) * direction;
            }

            // Устанавливаем направление
            bullet.transform.rotation = Quaternion.LookRotation(spreadDirection); // Чтобы пуля смотрела в сторону полета

            // Инициализируем пулю с параметрами из WeaponData
            bullet.Initialize(spreadDirection); // Направление уже включает разброс
            bullet.speed = _runtimeWeaponData.baseProjectileSpeed; // Теперь пуля имеет публичное поле speed
            bullet.damage = _runtimeWeaponData.baseDamage * playerStats.damageMultiplier; // И урон, который зависит от Player Stats
            bullet.lifetime = _runtimeWeaponData.bulletLifetime; // И публичное свойство Lifetime
            // Чтобы пуля вернулась в пул, когда она деактивируется (из-за столкновения или времени жизни)
            bullet.GetComponent<MonoBehaviour>().StartCoroutine(ReleaseBulletAfterDelay(bullet, bullet.lifetime)); // Добавим немного задержки
        }
    }

    // Корутина для возврата пули в пул
    private IEnumerator ReleaseBulletAfterDelay(Bullet bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet.gameObject.activeSelf) // Если пуля до сих пор активна (не попала никуда)
        {
            _bulletPool.Release(bullet);
        }
    }

    // Новый публичный метод для смены оружия (для NewWeaponUpgradeData)
    public void ChangeWeapon(WeaponData newWeapon)
    {
        StopShooting(); // Останавливаем старую стрельбу
        // Создаем новую runtime-копию для нового оружия
        _runtimeWeaponData = Instantiate(newWeapon);
        // TODO: Возможно, потребуется пересоздать пул или очистить старый, 
        // если новое оружие использует совсем другие пули.
        // Для MVP пока просто заменим _runtimeWeaponData.
        StartShooting(); // Запускаем новую стрельбу с новыми параметрами
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
