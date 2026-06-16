using UnityEngine;
using System;

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
    public float moveSpeed = 15f; // Скорость движения
    public float moveSpeedMultiplier = 1f; // Множитель скорости движения
    public float damageMultiplier = 1f; // Множитель урона
    public float health = 100f; // Текущее здоровье
    public float maxHealth = 100f; // Максимальное здоровье
    [Range(0f, 100f)] public float critChance = 0f; // шанс крита в процентах
    [Min(1f)] public float critMultiplier = 1.5f; // множитель урона при крите

    // Дополнительные статы (для скиллов)
    [Header("Player Skills Bonuses")]
    public bool isInvincible = false; // Неуязвимость
    public float currentDamageReduction = 0f; // Снижение входящего урона (0f = нет снижения, 0.5f = 50% снижение)

    // Переменные для эффектов от скиллов врагов
    public bool isFrozen = false;
    public bool isBurning = false;
    public bool isStunning = false;

    [Header("Player UI")]
    [SerializeField] private GameObject UINumberPos;

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
        health = maxHealth;
    }

    //Добавление опыта
    public void AddExperience(float amount)
    {
        if (currentLevel >= maxLevel) return; // Не добавляем опыт, если достигнут макс. уровень

        currentExperience += amount;
        Debug.Log($"Получено {amount} опыта. Текущий опыт: {currentExperience}");

        // Проверяем уровень в цикле, пока опыта хватает на новый уровень
        while (currentLevel < maxLevel && currentExperience >= GetExperienceRequiredForLevel(currentLevel + 1))
        {
            // Вычитаем стоимость уровня из текущего опыта, чтобы сохранить остаток
            currentExperience -= GetExperienceRequiredForLevel(currentLevel + 1);
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

    // Восстановление здоровья
    public void Heal(float amount)
    {
        health += amount;
        if (health > maxHealth)
        {
            health = maxHealth;
        }

        // Цифры хила
        if (HealNumberManager.Instance != null)
        {
            HealNumberManager.Instance.SpawnHealNumber(UINumberPos.transform.position + Vector3.up, amount);
        }
    }

    // Шанс крита
    public bool RollCritical()
    {
        return UnityEngine.Random.value < (Math.Clamp(critChance, 0f, 50f) / 100f);
    }

    //Смерть игрока
    public void TakeDamage(float amount)
    {
        if (health <= 0) return; // Чтобы не умирать дважды
        // Если игрок неуязвим
        if (isInvincible)
        {
            Debug.Log("Игрок неуязвим!");
            return;
        }

        float finalDamage = amount * (1f - currentDamageReduction); // Применяем снижение
        if (finalDamage < 0) finalDamage = 0; // Урон не может быть отрицательным

        health -= finalDamage;

        // Используем глобальный менеджер цифр урона
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.SpawnDamageNumber(UINumberPos.transform.position + Vector3.up, finalDamage, RollCritical(), isBurning, isFrozen, isStunning);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Игрок погиб!");

        // 1. Отключаем скрипты управления и стрельбы
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerShooting>().enabled = false;

        // 2. Сообщаем UI менеджеру, что игра окончена
        if (UISystem.Instance != null)
        {
            UISystem.Instance.ShowGameOver();
        }
    }
}
