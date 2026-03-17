using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float health = 3f;
    [SerializeField] private float moveSpeed = 2f; // Скорость движения врага к игроку

    private Transform _playerTransform; // Ссылка на игрока

    void Awake()
    {
        // Находим игрока при старте
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    void Update()
    {
        if (_playerTransform == null) return;

        // Враг движется к игроку
        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        transform.position += directionToPlayer * moveSpeed * Time.deltaTime;

        // Враг поворачивается лицом к игроку
        transform.LookAt(_playerTransform);
    }

    // Этот метод будет вызываться пулей
    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"Враг получил {amount} урона. Здоровье: {health}");
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Враг уничтожен!");
        // Здесь будут эффекты смерти, начисление опыта и т.д.
        gameObject.SetActive(false); // В дальнейшем будем возвращать в пул
    }
}
