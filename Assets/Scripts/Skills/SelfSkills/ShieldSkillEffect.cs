using UnityEngine;

public class ShieldSkillEffect : MonoBehaviour
{
    public float duration = 5f; // Длительность действия щита
    public bool makeInvincible = true; // Делает игрока неуязвимым
    public float damageReduction = 0.5f; // Если не неуязвим, то уменьшает урон на 50%

    private PlayerStats _playerStats;
    private GameObject _player; // Ссылка на игрока, чтобы повесить щит
    [SerializeField] private GameObject _visualEffect; // Визуальный эффект щита (может быть дочерним)

    void Start()
    {
        // Находим игрока (кто активировал скилл)
        _player = GameObject.FindGameObjectWithTag("Player"); // Убедись, что у игрока есть тег "Player"
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

        // Активируем эффект щита
        ApplyShieldEffect();

        // Уничтожаем эффект через заданное время
        Destroy(gameObject, duration);
    }

    // Активируем эффект щита
    private void ApplyShieldEffect()
    {
        if (makeInvincible)
        {
            _playerStats.isInvincible = true;
            Debug.Log("Игрок стал неуязвимым!");
        }
        else
        {
            _playerStats.currentDamageReduction += damageReduction; // Добавляем к текущему снижению урона
            Debug.Log($"Урон игрока снижен на {damageReduction * 100}%!");
        }

        // Активировать визуальный эффект щита вокруг игрока
        _visualEffect.SetActive(true);
    }

    void OnDestroy()
    {
        // Снимаем эффект щита, когда объект уничтожается
        RemoveShieldEffect();
    }

    // Снимаем эффект щита, когда объект уничтожается
    private void RemoveShieldEffect()
    {
        if (_playerStats == null) return; // Игрок мог быть уничтожен раньше

        if (makeInvincible)
        {
            _playerStats.isInvincible = false;
            Debug.Log("Игрок больше не неуязвим.");
        }
        else
        {
            _playerStats.currentDamageReduction -= damageReduction; // Убираем снижение урона
            Debug.Log("Снижение урона от щита снято.");
        }

        _visualEffect.SetActive(false);
    }
}
