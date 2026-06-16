using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Ultimate Unlock")]

public class UltimateUnlock : UpgradeData
{
    public SkillData ultimateToUnlock;

    public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting, SkillManager skillManager)
    {
        skillManager.SetUltimate(ultimateToUnlock);
    }
}
