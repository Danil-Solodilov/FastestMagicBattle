using UnityEngine;
using UnityEngine.UI;
using System; // Для TimeSpan
using TMPro;

public class UISystem : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [Header("UI Sliders")]
    [SerializeField] private Slider healthSlider; // Слайдер здоровья
    [SerializeField] private Slider expSlider; // Слайдер прогресса опыта

    [Header("UI Texts")]
    [SerializeField] private TextMeshProUGUI timerText; //Текст таймера
    [SerializeField] private TextMeshProUGUI killsText; //Текст убийств
    [SerializeField] private TextMeshProUGUI lvlText; //Текст уровня

    private float _elapsedTime;
    private int _killCount;

    // Синглтон для легкого доступа
    public static UISystem Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Если UI должен быть в разных сценах
    }

    void Update()
    {
        // Обновляем слайдер здоровья
        healthSlider.value = playerStats.health;
        healthSlider.maxValue = playerStats.maxHealth;

        // Обновляем слайдер опыта
        expSlider.maxValue = playerStats.GetExperienceRequiredForLevel(playerStats.currentLevel + 1);
        expSlider.value = playerStats.currentExperience;

        //Обновляем текст текущего уровня
        lvlText.text = "Уровень: " + playerStats.currentLevel.ToString();

        // Обновляем таймер
        _elapsedTime += Time.deltaTime;
        timerText.text = FormatTime(_elapsedTime);
    }

    // Форматирование времени в MM:SS
    private string FormatTime(float timeInSeconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);
        return $"Вы продержались: {timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }

    // Публичный метод для обновления счетчика убийств
    public void UpdateKillCount()
    {
        _killCount++;
        killsText.text = $"Убийств: {_killCount}";
    }
}
