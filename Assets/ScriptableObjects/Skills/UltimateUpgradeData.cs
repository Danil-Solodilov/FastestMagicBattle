using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Ultimate Upgrade")]

public class UltimateUpgradeData : UpgradeData
{
    public SkillData relatedUltimate;

    public float coldownDecrease = 1f;
    public float damageIncrease = 1f;
    public float radiusIncrease = 1f;
    public float durationIncrease = 1f;

    public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting, SkillManager skillManager)
    {
        skillManager.UpgradeUltimate(coldownDecrease, damageIncrease, radiusIncrease, durationIncrease);
    }
}
