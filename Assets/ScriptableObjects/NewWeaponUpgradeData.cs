using UnityEngine;

// Пример наследника: Новый тип оружия
[CreateAssetMenu(fileName = "NewWeaponUpgradeData", menuName = "Game/Upgrades/New Weapon")]
public class NewWeaponUpgradeData : UpgradeData
{
  public WeaponData weaponToGive; // Какое оружие будет выдано

  public NewWeaponUpgradeData()
  {
      type = UpgradeType.NewWeapon; // Устанавливаем тип по умолчанию
  }

  public override void ApplyUpgrade(PlayerStats playerStats, PlayerShooting playerShooting)
  {
        if (weaponToGive != null)
        {
            Debug.Log($"Применяем апгрейд: {upgradeName} (новое оружие: {weaponToGive.weaponName})");
            // В реальной игре нужно будет добавить это оружие к списку оружия игрока
            // Пока просто сменим текущее оружие:
            playerShooting.ChangeWeapon(weaponToGive);
            // Здесь должна быть логика добавления нового оружия в PlayerShooting,
            // а не просто замена текущего. Но для MVP (Minimum Viable Product) сойдет.
        }
  }
    
}
