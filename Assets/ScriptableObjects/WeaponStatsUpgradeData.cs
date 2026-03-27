using UnityEngine;


// Пример наследника: апгрейд статов оружия
[CreateAssetMenu(fileName = "WeaponStatsUpgradeData", menuName = "Game/Upgrades/Weapon Stats")]
public class WeaponStatsUpgradeData : UpgradeData
{
    public float damageIncrease = 0f;
    public float fireRateIncrease = 0f;
    public float projectileSpeedIncrease = 0f;
    public int projectilePerShotIncrease = 0;

    public WeaponStatsUpgradeData()
    {
        type = UpgradeType.WeaponUpgrade;
    }

    public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting)
    {
        WeaponData currentWeapon = playerShooting.GetCurrentRuntimeWeaponData();

        if (currentWeapon != null)
        {
            currentWeapon.baseDamage += damageIncrease; //Увеличение базового урона
            currentWeapon.baseFireRate -= fireRateIncrease; //Увеличение скорострельности
            currentWeapon.baseProjectileSpeed += projectileSpeedIncrease; //Увеличение скорости снаряда
            currentWeapon.projectilesPerShot += projectilePerShotIncrease; //Увеличение кол-ва пуль за 1 выстрел

            playerShooting.StopShooting(); // Останавливаем стрельбу
            playerShooting.StartShooting(); // Запускаем заново с обновленными параметрами
        }
    }
}
