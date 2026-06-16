using UnityEngine;


[CreateAssetMenu(menuName = "Game/Upgrades/Skill Unlock")]
public class SkillUnlockUpgrade : UpgradeData
{
    public SkillData skillToUnlock;

    public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting, SkillManager skillManager)
    {
        skillManager.AddSkill(skillToUnlock);
    }
}
