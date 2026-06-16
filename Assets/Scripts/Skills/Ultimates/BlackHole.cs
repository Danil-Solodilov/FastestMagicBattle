using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BlackHole : MonoBehaviour
{
    public float duration = 4f;
    public float pullRadius = 30f;
    public float pullForce = 2f; // Сила притяжения
    public float explosionDamage = 100f;
    public GameObject explosionVFX;

    [SerializeField] private Camera cam;
    private List<NavMeshAgent> pulledAgents = new List<NavMeshAgent>(); // Список всех, кого мы "захватили"

    public void Initialize(SkillData data)
    {
        duration = data.duration;
        pullRadius = data.effectRadius;
        explosionDamage = data.damage;

        // Запускаем уничтожение здесь, так как теперь мы знаем длительность
        Destroy(gameObject, duration);
    }



    void FixedUpdate()
    {
        // Ищем всех врагов вокруг
        Collider[] colliders = Physics.OverlapSphere(transform.position, pullRadius);

        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Enemy"))
            {
                NavMeshAgent agent = hit.GetComponent<NavMeshAgent>();

                // 1. Если нашли агента - отключаем его, чтобы он не сопротивлялся
                if (agent != null && agent.enabled)
                {
                    agent.enabled = false;
                    if (!pulledAgents.Contains(agent)) pulledAgents.Add(agent);
                }
                // 2. Двигаем через Transform (так как Rigidbody теперь Kinematic)
                Vector3 targetPos = transform.position;
                // Оставляем врага на той же высоте, что и черная дыра, или его текущей
                targetPos.y = hit.transform.position.y; 

                hit.transform.position = Vector3.MoveTowards(hit.transform.position, targetPos, pullForce * Time.fixedDeltaTime);
            }
        }
    }

    void OnDestroy()
    {
        // Финальный взрыв при исчезновении
        Explode();
    }

    void Explode()
    {
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        // Тряска камеры при взрыве
        cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraShake>() != null)
            cam.GetComponent<CameraShake>().StartShake(0.7f, 0.2f);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pullRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null) enemy.TakeDamage(explosionDamage);
            }
        }

        // 3. Возвращаем управление выжившим врагам
        foreach (var agent in pulledAgents)
        {
            // Если агент пустой (объект уничтожен) или враг уже умер/деактивирован
            if (agent == null || !agent.gameObject.activeInHierarchy) continue;

            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                // Warp нужен, чтобы агент "приземлился" на ближайшую точку NavMesh после того, как его сместили
                agent.Warp(agent.transform.position);
            }
        }
        pulledAgents.Clear();
    }
}
