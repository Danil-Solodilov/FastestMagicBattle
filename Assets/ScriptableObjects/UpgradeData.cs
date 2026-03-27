using UnityEngine;

public enum UpgradeType // Типы апгрейдов
{
    NewWeapon,
    WeaponUpgrade,
    PlayerStatUpgrade,
    PassiveAbility
}

// Это будет базовый класс для всех апгрейдов
// Мы не будем создавать экземпляры UpgradeData напрямую
// Будут наследники, например, NewWeaponUpgradeData, StatUpgradeData
public abstract class UpgradeData : ScriptableObject
{
    [Header("General Upgrade Info")]
    public string upgradeName = "New Upgrade";
    [TextArea] public string description = "A mysterious upgrade.";
    public Sprite icon; // Иконка для UI
    public UpgradeType type;

    // Виртуальный метод, который каждый наследник будет реализовывать по-своему
    public abstract void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting);
}
