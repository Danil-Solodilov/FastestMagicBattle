using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float lifetime = 3f; // Время жизни пули

    private Vector3 _direction; // Направление полета пули

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
