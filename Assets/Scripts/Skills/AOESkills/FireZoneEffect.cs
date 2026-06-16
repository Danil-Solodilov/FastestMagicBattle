using UnityEngine;
using System.Collections.Generic; // Нужен для List

public class FireZoneEffect : MonoBehaviour
{
    public float duration = 5f;       // Общая длительность существования зоны
    public float tickDamage = 8f;     // Урон за одно "тик"
    public float tickInterval = 0.3f; // Как часто наносится урон (раз в 0.3 сек)
    public float effectRadius = 5f;   // Радиус действия зоны (должен совпадать с тем, что в SkillData)
    public float burnDuration = 1f;   // Длительность горения врага

    private float _timer;
    private float _damageTimer;
    public LayerMask enemyLayer;      // Слой врагов (чтобы не сканировать всё подряд)

    void Start()
    {
        float correctSize = effectRadius / 5;
        transform.localScale = new Vector3(correctSize, 1, correctSize);
        _timer = duration;
        _damageTimer = tickInterval; // Начинаем сразу наносить урон

        // Сразу наносим первый удар при появлении
        DealDamageToEnemiesInZone();
    }

    void Update()
    {
        // Таймер жизни самой зоны
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            Destroy(gameObject); // Уничтожаем зону
            return;
        }

        // Таймер нанесения урона
        _damageTimer -= Time.deltaTime;
        if (_damageTimer <= 0)
        {
            DealDamageToEnemiesInZone();
            _damageTimer = tickInterval; // Сбрасываем таймер урона
        }
    }

    private void DealDamageToEnemiesInZone()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, effectRadius, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            // Проверяем тег или наличие скрипта Enemy
            if (hitCollider.CompareTag("Enemy"))
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // Эффект поджога врага
                    enemy.Burning(burnDuration);
                    enemy.TakeDamage(tickDamage);
                }
            }
        }
    }

    // Отрисовка в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}
