using UnityEngine;

public class HealingSkillEffect : MonoBehaviour
{
    public float duration = 5f; // Длительность действия хила
    public bool makeInvincible = true; // Делает игрока неуязвимым
    public float restoringHealth = 5f; // Восстановление хп за 1 тик
    public float tickInterval = 1f; // Интервал между тиками (в секундах)

    private PlayerStats _playerStats;
    private GameObject _player; // Ссылка на игрока, чтобы повесить хилл
    private float _timeSinceLastTick; // Таймер для отсчета интервалов
    [SerializeField] private GameObject _visualEffect; // Визуальный эффект хила (может быть дочерним)

    //[Header("Player UI Number")]
    //[SerializeField] private GameObject UINumberPos;

    void Start()
    {
        // Находим игрока (кто активировал скилл)
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player == null)
        {
            Debug.LogError("ShieldSkillEffect: Не найден игрок с тегом 'Player'!");
            Destroy(gameObject); // Уничтожаем эффект, если нет игрока
            return;
        }

        _playerStats = _player.GetComponent<PlayerStats>();
        if (_playerStats == null)
        {
            Debug.LogError("ShieldSkillEffect: У игрока нет компонента PlayerStats!");
            Destroy(gameObject);
            return;
        }

        // 1. Включаем неуязвимость сразу
        if (makeInvincible)
        {
            _playerStats.isInvincible = true;
        }

        _timeSinceLastTick = 0f; // Сбрасываем таймер в начале

        // Уничтожаем эффект через заданное время
        Destroy(gameObject, duration);
    }

    void Update()
    {
        if (_playerStats == null) return;

        _timeSinceLastTick += Time.deltaTime;
        if (_timeSinceLastTick >= tickInterval)
        {
            // Убеждаемся, что мы не хилим больше, чем нужно, если tickInterval пропустил кадры
            _timeSinceLastTick -= tickInterval;

            // 2. Хилим каждый кадр
            _playerStats.Heal(restoringHealth);
        }
    }

    void OnDestroy()
    {
        // Снимаем эффект хила, когда объект уничтожается
        RemoveShieldEffect();
    }

    // Снимаем эффект хила, когда объект уничтожается
    private void RemoveShieldEffect()
    {
        if (_playerStats == null) return; // Игрок мог быть уничтожен раньше

        if (makeInvincible)
        {
            _playerStats.isInvincible = false;
            Debug.Log("Игрок больше не неуязвим.");
        }

        _visualEffect.SetActive(false);
    }
}
