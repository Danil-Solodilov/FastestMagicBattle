using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _damage;

    [Header("Lifetime & Spin")]
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float spinSpeedDegrees = 360f; // градусы/сек
    private Vector3 _spinAxis;
    private bool _usesRigidbody = false;
    private Rigidbody _rb;

    [Header("Obstacle Settings")]
    [SerializeField] private LayerMask obstacleLayer; // —лой дл€ стен, деревьев и т.д.

    public void Setup(Vector3 direction, float speed, float damage)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;
        transform.rotation = Quaternion.LookRotation(_direction); // ѕоворачиваем пулю

        // spin init
        _spinAxis = Random.onUnitSphere;
        _usesRigidbody = TryGetComponent(out _rb);
        if (_usesRigidbody)
        {
            // angularVelocity в радианах/сек
            _rb.angularVelocity = _spinAxis * (spinSpeedDegrees * Mathf.Deg2Rad);
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        // если нет Rigidbody Ч вращаем вручную
        if (!_usesRigidbody)
        {
            transform.Rotate(_spinAxis, spinSpeedDegrees * Time.deltaTime, Space.Self);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats player = other.GetComponent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(_damage);
            }
            Destroy(gameObject); // ”ничтожаем объект при попадании
            return;
        }
        else if (((1 << other.gameObject.layer) & obstacleLayer.value) != 0 || other.CompareTag("Obstacle") || other.CompareTag("Ground"))
        {
            // «десь можно спавнить эффект искр или пыли
            Destroy(gameObject); // ”ничтожаем объект при попадании
        }
    }
}
