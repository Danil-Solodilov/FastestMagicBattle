using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

public class Enemy : MonoBehaviour
{
    private float _health = 3f; //Здоровье врага
    private float _moveSpeed = 2f; // Скорость движения врага к игроку
    private float _experienceReward = 10f; // Сколько опыта дает враг
    private float _damage = 10f; //Урон врага
    private float _attackInterval = 1f; //Скорость атаки врага
    private float _nextAttackTime;
    private bool _isDead; //Проверка на смерть врага
    public bool IsDead => _isDead; // Публичный геттер для проверки извне

    //Создание пула
    private IObjectPool<Enemy> _myPool;
    private float _currentHealth; // Используем для здоровья в рантайме

    //Визуализация
    [Header("VFX")]
    [SerializeField] private Color flashColor = Color.white; // Цвет вспышки
    [SerializeField] private float flashDuration = 0.1f;    // Длительность вспышки
    private Renderer _renderer;
    private Color _originalColor;
    private Coroutine _flashCoroutine;

    //Необходимые ссылки
    private Transform _playerTransform; // Ссылка на игрока
    private PlayerStats _playerStats; // Ссылка на PlayerStats

    void Awake()
    {
        // Ищем Renderer в самом объекте или его детях
        _renderer = GetComponentInChildren<Renderer>();

        // Запоминаем исходный цвет, чтобы к нему вернуться
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
        // Находим игрока при старте
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            _playerStats = _playerTransform.GetComponent<PlayerStats>();
        }
    }

    public void Setup(EnemyData data, IObjectPool<Enemy> pool)
    {
        _myPool = pool;
        _currentHealth = data.health; // Сбрасываем здоровье при выходе из пула!
        _health = data.health;
        _moveSpeed = data.speed;
        _experienceReward = data.experienceReward;
        _damage = data.damage;
        _attackInterval = data.attackInterval;

        _isDead = false; // Сброс флага при получении из пула
    }

    void Update()
    {
        if (_playerTransform == null) return;

        // Враг движется к игроку
        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        transform.position += directionToPlayer * _moveSpeed * Time.deltaTime;

        // Враг поворачивается лицом к игроку
        transform.LookAt(_playerTransform);
    }

    // Этот метод будет вызываться пулей
    public void TakeDamage(float amount)
    {
        if (_isDead) return; // Если враг уже мертв, игнорируем урон
        _currentHealth -= amount;

        // Используем глобальный менеджер цифр урона
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.SpawnDamageNumber(transform.position + Vector3.up, amount);
        }
     
        // Запускаем вспышку
        StopFlashAndStartNew();

        //Отбрасывание врага
        Vector3 pushDir = (transform.position - _playerTransform.position).normalized;
        transform.position += pushDir * 0.1f;

        Debug.Log($"Враг получил {amount} урона. Здоровье: {_health}");
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead)
        {
            return; // Дополнительная проверка, чтобы быть на 100% уверенным
        }

        _isDead = true;      // Устанавливаем флаг, что враг мертв
        _renderer.material.color = _originalColor;
        Debug.Log("Враг уничтожен!");

        // Здесь будут эффекты смерти, начисление опыта и т.д.
        if (_playerStats != null)
        {
            _playerStats.AddExperience(_experienceReward); // Добавляем опыт игроку
        }

        if (UISystem.Instance != null)
        {
            UISystem.Instance.UpdateKillCount();
        }

        // Вместо уничтожения — возвращаем в пул
        if (_myPool != null) _myPool.Release(this);
        else gameObject.SetActive(false); // На всякий случай
    }

    //Атаковать при приближении к игроку
    private void OnTriggerStay(Collider other)
    {
        // Если коснулись игрока
        if (other.CompareTag("Player"))
        {
            if (Time.time >= _nextAttackTime)
            {
                if (_playerStats != null)
                {
                    _playerStats.TakeDamage(_damage);
                    _nextAttackTime = Time.time + _attackInterval;
                }
            }
        }
    }
    //Вспышка при получении урона
    private void StopFlashAndStartNew()
    {
        // Если враг уже "мигает" (например, от предыдущей пули), 
        // останавливаем старую корутину и начинаем заново
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Меняем цвет на вспышку
        _renderer.material.color = flashColor;

        // Ждем короткое время
        yield return new WaitForSeconds(flashDuration);

        // Возвращаем исходный цвет
        _renderer.material.color = _originalColor;

        _flashCoroutine = null;
    }
}
