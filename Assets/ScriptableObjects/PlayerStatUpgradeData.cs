using UnityEngine;

// Улучшение характеристик игрока
[CreateAssetMenu(fileName = "PlayerStatUpgradeData", menuName = "Game/Upgrades/Player Stat")]
public class PlayerStatUpgradeData : UpgradeData
{
    public float healthIncrease = 0f;
    public float moveSpeedIncrease = 0f;
    public float damageIncrease = 0f;

    public PlayerStatUpgradeData()
    {
        type = UpgradeType.PlayerStatUpgrade;
    }

    public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting)
    {
        playerStats.maxHealth += healthIncrease;
        playerStats.health += healthIncrease; // Восстанавливаем здоровье при увеличении макс. здоровья
        playerStats.moveSpeedMultiplier += moveSpeedIncrease;
        playerStats.damageMultiplier += damageIncrease;
        // Здесь нужно будет обновить PlayerMovement, если moveSpeedMultiplier будет влиять
        // А также PlayerShooting, если damageMultiplier будет применяться к урону пуль
    }
}
