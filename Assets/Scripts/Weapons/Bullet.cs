using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float lifetime = 3f; // Время жизни пули

    private Vector3 _direction; // Направление полета пули

    public float Lifetime // Публичное свойство (getter) для получения значения lifetime в другом скрипте в обход типа "private"
    {
        get { return lifetime; }
    }

    // Метод для инициализации пули
    public void Initialize(Vector3 direction)
    {
        _direction = direction.normalized; // Нормализуем, чтобы скорость была одинаковой
        gameObject.SetActive(true); // Активируем пулю из пула
        Invoke(nameof(DeactivateBullet), lifetime); // Отключаем пулю через время жизни
    }

    void Update()
    {
        // Движение пули
        transform.position += _direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, столкнулись ли мы с врагом
        if (other.CompareTag("Enemy"))
        {
            // Здесь будет логика нанесения урона врагу
            other.GetComponent<Enemy>().TakeDamage(damage); 
            Debug.Log($"Пуля попала во врага! Нанесла {damage} урона.");

            // Отключаем пулю после столкновения
            DeactivateBullet();
        }
    }

    // Метод для деактивации (возвращения в пул)
    private void DeactivateBullet()
    {
        // Отменяем все Invoke, чтобы не сработало после возвращения в пул
        CancelInvoke();
        gameObject.SetActive(false); // Деактивируем объект, чтобы он вернулся в пул
    }
}
