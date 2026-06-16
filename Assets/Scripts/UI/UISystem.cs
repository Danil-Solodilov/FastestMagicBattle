using UnityEngine;
using UnityEngine.UI;
using System; // Для TimeSpan
using TMPro;
using UnityEngine.SceneManagement; // Для перезагрузки игры

public class UISystem : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private PlayerMovement movement;
    private Transform _cam;

    [Header("UI Sliders")]
    [SerializeField] private Slider healthSlider; // Слайдер здоровья
    [SerializeField] private Slider expSlider; // Слайдер прогресса опыта

    [Header("UI Texts")]
    [SerializeField] private TextMeshProUGUI timerText; //Текст таймера
    [SerializeField] private TextMeshProUGUI killsText; //Текст убийств
    [SerializeField] private TextMeshProUGUI lvlText; //Текст уровня

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalStatsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("DownPanel UI")]
    [SerializeField] private Button pauseButton;

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
        _cam = Camera.main.transform;
        // Назначаем действие кнопке программно
        restartButton.onClick.AddListener(RestartGame);
        gameOverPanel.SetActive(false);
    }

    void LateUpdate()
    {
        // Обновляем слайдер здоровья
        healthSlider.value = playerStats.health;
        healthSlider.maxValue = playerStats.maxHealth;
        healthSlider.transform.rotation = _cam.transform.rotation;

        // Обновляем слайдер опыта
        expSlider.maxValue = playerStats.GetExperienceRequiredForLevel(playerStats.currentLevel + 1);
        expSlider.value = playerStats.currentExperience;

        //Обновляем текст текущего уровня
        lvlText.text = playerStats.currentLevel.ToString();

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

    // Метод для перезагрузки игры
    public void RestartGame()
    {
        // ОБЯЗАТЕЛЬНО возвращаем время в норму перед перезагрузкой!
        Time.timeScale = 1f;

        _elapsedTime = 0;
        _killCount = 0;
        // Перезагружаем текущую активную сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Метод, который вызывается из PlayerStats
    public void ShowGameOver()
    {
        // Останавливаем время в игре
        Time.timeScale = 0f;

        gameOverPanel.SetActive(true);

        // Формируем финальную строку статистики
        string timeStr = FormatTime(_elapsedTime);
        finalStatsText.text = $"{timeStr} минут\nВы убили: {_killCount} врагов";
    }

    //Выход из игры
    public void QuitGame()
    {
        Application.Quit();
    }
}
