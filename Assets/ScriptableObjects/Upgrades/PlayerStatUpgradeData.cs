using UnityEngine;

// Улучшение характеристик игрока
[CreateAssetMenu(fileName = "PlayerStatUpgradeData", menuName = "Game/Upgrades/Player Stat")]
public class PlayerStatUpgradeData : UpgradeData
{
    public float healthIncrease = 0f;
    public float moveSpeedIncrease = 0f;
    public float damageIncrease = 0f;
    public float criticalChance = 0f;
    public float critMultiplierUpgrade = 0f;

    public PlayerStatUpgradeData()
    {
        type = UpgradeType.PlayerStatUpgrade;
    }

    public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting, SkillManager skillManager)
    {
        playerStats.maxHealth += healthIncrease;
        playerStats.health += healthIncrease; // Восстанавливаем здоровье при увеличении макс. здоровья
        playerStats.moveSpeed *= 1f + (moveSpeedIncrease / 100f);
        playerStats.damageMultiplier += (damageIncrease / 100f);
        playerStats.critChance += criticalChance;
        playerStats.critMultiplier += (critMultiplierUpgrade / 100f);
    }
}
