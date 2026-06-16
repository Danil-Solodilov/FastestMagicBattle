using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StunningZoneEffect : MonoBehaviour
{
    public float duration = 2f;       // Общая длительность существования зоны
    public float skillDamage = 50f;     // Урон за одно "тик"
    public float effectRadius = 5f;   // Радиус действия зоны (должен совпадать с тем, что в SkillData)
    public float stunDuration = 3f;   // Длительность оглушения врага

    private float _timer;
    private float _damageTimer = 1f;
    public LayerMask enemyLayer;      // Слой врагов (чтобы не сканировать всё подряд)

    void Start()
    {
        float correctSize = effectRadius / 4;
        transform.localScale = new Vector3(correctSize, 1, correctSize);
        _timer = duration;
        StartCoroutine(DelayBeforeDamage());
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
        //_damageTimer -= Time.deltaTime;
        //if (_damageTimer <= 0)
        //{
        //    DealDamageToEnemiesInZone();
        //}
    }

    private IEnumerator DelayBeforeDamage()
    {
        yield return new WaitForSeconds(_damageTimer);
        DealDamageToEnemiesInZone();
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
                    // Эффект стана врага
                    enemy.Stunning(stunDuration);
                    enemy.TakeDamage(skillDamage);
                }
            }
        }
    }
}
