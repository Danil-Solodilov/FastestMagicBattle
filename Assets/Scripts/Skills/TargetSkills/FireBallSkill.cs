using UnityEngine;

public class FireBallSkill : MonoBehaviour
{
    [SerializeField] private float _speed = 20f;
    private float _damage;
    private float _burningDuration;

    [HideInInspector] public float maxRange; // Будет устанавливаться из SkillManager
    private Vector3 _startPosition;


    public void Initialize(float damage, float range, float duration)
    {
        _startPosition = transform.position;
        _damage = damage;
        maxRange = range;
        _burningDuration = duration;

        Destroy(gameObject, 5f); // Чтобы не летела вечно
    }


    void Update()
    {
        // Летим вперед (в ту сторону, куда смотрела стрелка при касте)
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);

        // Проверяем дистанцию от начальной точки
        float distanceTraveled = Vector3.Distance(_startPosition, transform.position);

        if (distanceTraveled >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Burning(_burningDuration); // Замораживаем
                enemy.TakeDamage(_damage); // Наносим урон
            }

            // После попадания уничтожаем снаряд (или создаем эффект взрыва)
            Destroy(gameObject);
        }
    }
}
