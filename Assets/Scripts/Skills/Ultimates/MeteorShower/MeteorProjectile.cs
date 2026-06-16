using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 40f;
    public float explosionRadius = 1f;
    public GameObject explosionVFX; // Префаб взрыва (частицы)

    [SerializeField] private Camera cam;

    [HideInInspector] public Vector3 targetPoint;

    public void Initialize(float dmg, float radius)
    {
        damage = dmg;
        explosionRadius = radius/5;
    }

    void Update()
    {
        // Двигаем метеор к целевой точке на земле
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        // Если долетели до цели
        if (Vector3.Distance(transform.position, targetPoint) < 0.1f)
        {
            Explode();
        }
    }

    void Explode()
    {
        // Тряска камеры при взрыве
        cam = Camera.main;
        cam.GetComponent<CameraShake>().StartShake(0.5f, 0.1f);
        
        // 1. Создаем визуальный эффект взрыва
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, targetPoint, Quaternion.identity);
        }

        // 2. Ищем всех врагов в радиусе взрыва
        Collider[] hitColliders = Physics.OverlapSphere(targetPoint, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        // 3. Удаляем метеор
        Destroy(gameObject);
    }
}