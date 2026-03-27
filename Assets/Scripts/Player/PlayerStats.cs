using UnityEngine;
using System;
using UnityEngine.UI; // Для работы с UI элементами

public class PlayerStats : MonoBehaviour
{
    [Header("Experience & Leveling")]
    public float currentExperience = 0f;
    public int currentLevel = 0;
    [SerializeField] private AnimationCurve experienceCurve; // Кривая для определения опыта до следующего уровня
    [SerializeField] private int maxLevel = 100; // Максимальный уровень

    // Событие, которое оповещает другие скрипты о получении уровня
    public static event Action<int> OnLevelUp;

    // Дополнительные статы (для апгрейдов)
    [Header("Player Attributes")]
    public float moveSpeedMultiplier = 1f; // Множитель скорости движения
    public float damageMultiplier = 1f; // Множитель урона
    public float health = 100f; // Текущее здоровье
    public float maxHealth = 100f; // Максимальное здоровье



    void Start()
    {
        // Убедимся, что кривая опыта настроена.
        // Пример: на 0 уровне нужно 100 опыта, на 10 уровне нужно 1000 опыта
        // В редакторе создай ключи (0, 100), (10, 1000), (20, 2500) и т.д.
        if (experienceCurve.keys.Length == 0)
        {
            Debug.LogWarning("Experience Curve is empty. Setting default values.");
            experienceCurve = AnimationCurve.EaseInOut(0, 100, 100, 100000); // Пример
        }
    }

    //Добавление опыта
    public void AddExperience(float amount)
    {
        if (currentLevel >= maxLevel) return; // Не добавляем опыт, если достигнут макс. уровень

        currentExperience += amount;
        Debug.Log($"Получено {amount} опыта. Текущий опыт: {currentExperience}");

        // Проверяем, достаточно ли опыта для следующего уровня
        float expToNextLevel = GetExperienceRequiredForLevel(currentLevel + 1);
        if (currentExperience >= expToNextLevel)
        {
            LevelUp();
        }
    }

    public float expToNextLevel()
    {
        float maxEXP = GetExperienceRequiredForLevel(currentLevel + 1);
        return maxEXP;
    }

    //Повышение уровня
    private void LevelUp()
    {
        if (currentLevel >= maxLevel) return;

        currentLevel++;
        currentExperience = 0; // Сбрасываем опыт или переносим излишек

        Debug.Log($"Персонаж достиг уровня {currentLevel}!");

        // Вызываем событие, чтобы UI или система апгрейдов отреагировали
        OnLevelUp?.Invoke(currentLevel);
    }

    //Значение на кривой опыта
    public float GetExperienceRequiredForLevel(int level)
    {
        // Используем кривую опыта.
        // Пример: experienceCurve.Evaluate(currentLevel) вернет опыт, необходимый для этого уровня.
        // Чтобы получить опыт для СЛЕДУЮЩЕГО уровня, используем level - 1 в качестве индекса
        // (если curve.keys - это уровни, а значения - это опыт)
        // Для простоты:
        if (level - 1 < experienceCurve.keys.Length)
        {
            return experienceCurve.Evaluate(level - 1); // Опыт для перехода на уровень (level)
        }
        else // Если кривая не определена до этого уровня
        {
            // Увеличиваем требование линейно или по другой формуле
            return experienceCurve.Evaluate(experienceCurve.keys.Length - 1) * 1.5f * (level - experienceCurve.keys.Length);
        }
    }

    // Для дебага:
    public float GetCurrentExpPercentage()
    {
        float expToNextLevel = GetExperienceRequiredForLevel(currentLevel + 1);
        float expForCurrentLevel = GetExperienceRequiredForLevel(currentLevel); // Опыт, который нужно набрать на текущем уровне
        return (currentExperience - expForCurrentLevel) / (expToNextLevel - expForCurrentLevel);
    }

    //Смерть игрока
    public void TakeDamage(float amount)
    {
        health -= amount;
        // Используем глобальный менеджер цифр урона
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.SpawnDamageNumber(transform.position + Vector3.up, amount);
        }
        Debug.Log($"Игрок получил урон! Здоровье: {health}/{maxHealth}");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Игрок погиб!");
        // Пока просто перезагрузим сцену или поставим паузу
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
