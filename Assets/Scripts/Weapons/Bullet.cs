using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float lifetime = 3f; // Время жизни пули

    // Флаг для критов
    [HideInInspector] public bool isCritical = false;

    private Vector3 _direction; // Направление полета пули
    private Vector3 _lastPosition;
    private bool _isInitialized = false;

    [Header("Special Effects")]
    public bool isExplosive; //Для взрывных снарядов
    public float explosionRadius = 3f;

    public int bouncesLeft = 0; //Для рикошетных снарядов
    public float ricochetRange = 5f;

    [Header("Layers Settings")]
    [SerializeField] private LayerMask collisionLayers; // Необходимые слои

    // Метод для инициализации пули
    public void Initialize(Vector3 direction, float bSpeed, float bDamage, float bLifetime, bool bExplosive, float bRadius, int bBounces,
        bool isCrit = false, float critMultiplier = 1f)
    {
        _direction = direction.normalized; // Нормализуем, чтобы скорость была одинаковой
        speed = Math.Clamp(bSpeed, 10f, 200f);
        lifetime = bLifetime;
        isExplosive = bExplosive;
        explosionRadius = bRadius;
        bouncesLeft = bBounces;

        isCritical = isCrit;
        damage = isCrit ? bDamage * critMultiplier : bDamage;

        _lastPosition = transform.position;
        _isInitialized = true;

        gameObject.SetActive(true); // Активируем пулю из пула
        CancelInvoke(); // Очищаем старые вызовы
        Invoke(nameof(DeactivateBullet), lifetime); // Отключаем пулю через время жизни
    }

    void Update()
    {
        if (!_isInitialized) return;

        // 1. Вычисляем направление и дистанцию, которую пуля прошла за этот кадр
        float moveDistance = speed * Time.deltaTime;
        Vector3 nextPosition = transform.position + _direction * moveDistance;

        // Проверка столкновений через Raycast (от старой позиции к новой)
        if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, moveDistance + 0.1f, collisionLayers))
        {
            HandleHit(hit.collider, hit.point);
            return; // Прекращаем движение, если попали
        }

        transform.position = nextPosition;
    }

    private void HandleHit(Collider other, Vector3 hitPoint)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, isCritical);

            if (isCritical)
            {
                Debug.Log($"КРИТИЧЕСКИЙ УДАР! Нанесено {damage} урона.");
            }
            else
            {
                Debug.Log($"Пуля попала во врага! Нанесла {damage} урона.");
            }

            // 1.Логика взрыва
            if (isExplosive)
            {
                VFXManager.Instance?.SpawnExplosion(hitPoint);
                Explode();
            }

            // 2. Логика рикошета
            if (bouncesLeft > 0)
            {
                VFXManager.Instance.SpawnImpact(transform.position);
                Ricochet(other.transform);
                return; // Не деактивируем, так как есть рикошет
            }  
        }

        VFXManager.Instance.SpawnImpact(transform.position);
        // Отключаем пулю после столкновения, если нет рикошета
        DeactivateBullet();
    }

    void OnEnable()
    {
        _lastPosition = transform.position;
    }

    // Метод для деактивации (возвращения в пул)
    private void DeactivateBullet()
    {
        _isInitialized = false;
        // Отменяем все Invoke, чтобы не сработало после возвращения в пул
        CancelInvoke();
        if (gameObject.activeInHierarchy)
        {
            // Если объект активен, просто выключаем его. 
            // Если пул был очищен, пуля просто останется выключенной и не будет мешать.
            gameObject.SetActive(false);// Деактивируем объект, чтобы он вернулся в пул
        }

        // сбрасываем все нужные переменные
        isExplosive = false;
        bouncesLeft = 0;
        explosionRadius = 0;
        isCritical = false;
    }

    // Метод для взрыва пули
    private void Explode()
    {
        // Ищем всех врагов в радиусе взрыва
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));

        foreach (var col in hitEnemies)
        {
            Enemy e = col.GetComponent<Enemy>();
            if (e != null)
            {
                // Наносим, например, 50% урона по области
                if (damage == 1)
                {
                    e.TakeDamage(damage);
                }
                else
                {
                    e.TakeDamage(damage * 0.5f);
                }
            }
        }

        // Тут можно спавнить эффект взрыва (Particles)
        Debug.Log("Взрыв!");
    }

    // Метод для рикошета пули
    private void Ricochet(Transform currentEnemy)
    {
        bouncesLeft--;

        // Ищем следующую ближайшую цель
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, ricochetRange, LayerMask.GetMask("Enemy"));

        Transform bestTarget = null;
        float minDistance = Mathf.Infinity;

        foreach (var col in potentialTargets)
        {
            if (col.transform == currentEnemy) continue; // Не отскакивать в того же самого

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestTarget = col.transform;
            }
        }

        if (bestTarget != null)
        {
            // Меняем направление пули на новую цель
            _direction = (bestTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(_direction);
        }
        else
        {
            // Если целей рядом нет, пуля просто исчезает
            DeactivateBullet();
        }
    }
}
