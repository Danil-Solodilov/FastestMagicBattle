using UnityEngine;
using System.Collections;

public class LichIceSkill : MonoBehaviour
{
    private float _damage;
    private float _duration;
    private float _radius;

    private float _damageTimer;
    public float tickInterval = 1.5f;

    [SerializeField] private float beforeTakeDamage;
    public LayerMask PlayerLayer;

    public void Setup(float damage, float duration, float radius)
    {
        _damage = damage;
        _duration = duration;
        _radius = radius;

        transform.rotation = Quaternion.identity;

        _damageTimer = beforeTakeDamage;

        Destroy(gameObject, _duration);
    }

    private void Update()
    {
        // Таймер нанесения урона
        _damageTimer -= Time.deltaTime;
        if (_damageTimer <= 0)
        {
            DealDamageToPlayerInZone();
            _damageTimer = tickInterval; // Сбрасываем таймер урона
        }
    }


    private void DealDamageToPlayerInZone()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _radius, PlayerLayer);

        foreach (var hitCollider in hitColliders)
        {
            // Проверяем тег или наличие скрипта Enemy
            if (hitCollider.CompareTag("Player"))
            {
                PlayerStats player = hitCollider.GetComponent<PlayerStats>();
                if (player != null)
                {
                    player.TakeDamage(_damage);
                    Debug.Log("Игрок получил урон от скилла");
                }
            }
        }
    }
}
