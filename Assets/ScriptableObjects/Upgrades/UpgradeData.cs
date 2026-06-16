using UnityEngine;

public enum UpgradeType // Типы апгрейдов
{
    MainWeaponUpgrade,
    PlayerStatUpgrade,
    BulletsUpgrade,
    SkillUnlock,    // Получение нового скилла
    UltimateUnlock, // Выбор ульты (на 10 уровне)
    UltimateUpgrade // Улучшение выбранной ульты
}

public enum UpgradeRarity { Common, Rare, Epic } // Редкость апгрейдов

// Это будет базовый класс для всех апгрейдов
// Будут наследники, например, NewWeaponUpgradeData, StatUpgradeData
public abstract class UpgradeData : ScriptableObject
{
    [Header("General Upgrade Info")]
    public string upgradeName = "New Upgrade";
    [TextArea] public string description = "A mysterious upgrade.";
    public Sprite icon; // Иконка для UI
    public UpgradeType type;

    [Header("Rarity Settings")]
    public UpgradeRarity rarity;

    [Header("Grouping")]
    public string groupID;

    // Виртуальный метод, который каждый наследник будет реализовывать по-своему
    public abstract void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting, SkillManager skillManager);
}
