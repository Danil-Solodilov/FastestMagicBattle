using UnityEngine;


// Пример наследника: апгрейд статов оружия
[CreateAssetMenu(fileName = "WeaponStatsUpgradeData", menuName = "Game/Upgrades/Weapon Stats")]
public class WeaponStatsUpgradeData : UpgradeData
{
    public float damageIncrease = 0f;
    public float fireRateIncrease = 0f;
    public float projectileSpeedIncrease = 0f;
    public float bulletSpreadAngleIncrease = 0f;
    public int bulletPerShot = 0;

    public WeaponStatsUpgradeData()
    {
        type = UpgradeType.MainWeaponUpgrade;
    }

    public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting, SkillManager skillManager)
    {
        WeaponData currentWeapon = playerShooting.GetCurrentRuntimeWeaponData();

        if (currentWeapon != null)
        {
            currentWeapon.baseDamage += damageIncrease; //Увеличение урона текущего оружия
            currentWeapon.baseFireRate /= 1f + fireRateIncrease / 100; //Увеличение скорострельности
            currentWeapon.baseProjectileSpeed *= 1f + projectileSpeedIncrease / 100; //Увеличение скорости снаряда
            currentWeapon.bulletSpreadAngle += bulletSpreadAngleIncrease;
            currentWeapon.projectilesPerShot += bulletPerShot;

            playerShooting.StopShooting(); // Останавливаем стрельбу
            playerShooting.StartShooting(); // Запускаем заново с обновленными параметрами
        }
    }
}
