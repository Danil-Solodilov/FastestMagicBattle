using UnityEngine;
using UnityEngine.Pool; // Для использования Object Pool
using System.Collections; // Для IEnumerator

[RequireComponent(typeof(PlayerMovement))] // Убедимся, что PlayerMovement есть
public class PlayerShooting : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint; // Точка, откуда вылетают пули
    [SerializeField] private float fireRate = 0.5f; // Время между выстрелами
    [SerializeField] private float bulletSpreadAngle = 0f; // Угол разброса пули (для мульти-пулевого оружия)

    [Header("Targeting")]
    [SerializeField] private float targetingRadius = 5f; // Радиус поиска врагов

    private ObjectPool<Bullet> _bulletPool;
    private Coroutine _shootingCoroutine; // Ссылка на корутину, чтобы можно было остановить

    // В Awake создаем пул объектов
    private void Awake()
    {
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

    private Bullet CreatePooledBullet()
    {
        Bullet bullet = Instantiate(bulletPrefab);
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

        StartShooting(); // Начинаем стрельбу сразу
    }

    public void StartShooting()
    {
        if (_shootingCoroutine != null)
        {
            StopCoroutine(_shootingCoroutine);
        }
        _shootingCoroutine = StartCoroutine(ShootCoroutine());
    }
    public void StopShooting()
    {
        if (_shootingCoroutine != null)
        {
            StopCoroutine(_shootingCoroutine);
            _shootingCoroutine = null;
        }
    }

    private IEnumerator ShootCoroutine()
    {
        while (true) // Бесконечный цикл стрельбы
        {
            Shoot();
            yield return new WaitForSeconds(fireRate); // Ждем перед следующим выстрелом
        }
    }

    private void Shoot()
    {
        // Находим ближайшего врага
        Transform target = FindNearestEnemy();
        Vector3 direction = transform.forward; // Если нет врага, стреляем вперед

        if (target != null)
        {
            direction = (target.position - firePoint.position).normalized;
        }

        // Получаем пулю из пула
        Bullet bullet = _bulletPool.Get();

        // Устанавливаем позицию и направление
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.LookRotation(direction); // Чтобы пуля смотрела в сторону полета

        // Инициализируем пулю
        bullet.Initialize(direction);

        // Чтобы пуля вернулась в пул, когда она деактивируется (из-за столкновения или времени жизни)
        bullet.GetComponent<MonoBehaviour>().StartCoroutine(ReleaseBulletAfterDelay(bullet, bullet.Lifetime + 0.1f)); // Добавим немного задержки
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

    private Transform FindNearestEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRadius, LayerMask.GetMask("Enemy")); // Используем LayerMask

        Transform nearestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in colliders)
        {
            // Убедитесь, что враги имеют тег "Enemy"
            if (col.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = col.transform;
                }
            }
        }
        return nearestEnemy;
    }

    // Для визуализации радиуса поиска врагов в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetingRadius);
    }
}
