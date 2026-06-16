using UnityEngine;

public enum ProjectileType { Explosive, Ricochet }

[CreateAssetMenu(fileName = "NewBulletModifier", menuName = "Game/Upgrades/Bullet Modifier")]
public class BulletModifierUpgradeData : UpgradeData
{
    public ProjectileType projectileType;

    [Header("Explosion Settings")]
    public bool enableExplosions;
    public float explosionRadiusSet;

    [Header("Ricochet Settings")]
    public int ricochetCountSet;
    public float extraRicochetRange;

    public Bullet NewBulletPrefab; // Новый префаб пули

    // Метод, который вызывается при выборе апгрейда
    public override void ApplyUpgrade(PlayerStats stats, PlayerShooting shooting, SkillManager skillManager)
    {
        // 1. Получаем текущее работающее оружие игрока
        WeaponData currentWeapon = shooting.GetCurrentRuntimeWeaponData();

        if (currentWeapon == null)
        {
            Debug.LogError("Не удалось найти данные оружия для модификации!");
            return;
        }

        // Перед тем как применить новое, зануляем старое
        currentWeapon.explosiveBullets = false;
        currentWeapon.explosionRadius = 0;
        currentWeapon.ricochetCount = 0;
        currentWeapon.ricochetRange = 0;

        // 2. Модифицируем параметры взрыва
        // 3. Если в апгрейде указан новый префаб — меняем его в данных оружия
        if (NewBulletPrefab != null)
        {
            currentWeapon.bulletPrefab = NewBulletPrefab;
        }
        if (projectileType == ProjectileType.Explosive)
        {
            currentWeapon.explosiveBullets = true;
            currentWeapon.explosionRadius = explosionRadiusSet;

            // Если радиус был 0, установим хотя бы базовый
            if (currentWeapon.explosionRadius <= 0) currentWeapon.explosionRadius = 3f;

            // На всякий случай сбрасываем рикошет
            currentWeapon.ricochetCount = 0;
        }
        else if (projectileType == ProjectileType.Ricochet) // 3. Модифицируем параметры рикошета
        {
            currentWeapon.explosiveBullets = false; // Выключаем взрывы

            currentWeapon.ricochetCount = ricochetCountSet;
            currentWeapon.ricochetRange = extraRicochetRange;

            // Если дальность рикошета была 0, установим базовую
            if (currentWeapon.ricochetCount > 0 && currentWeapon.ricochetRange <= 0)
            {
                currentWeapon.ricochetRange = 5f;
                //currentWeapon.ricochetCount = 1;
            }
        }

        // Обновляем пул, так как параметры пули (префаб или логика) изменились
        shooting.RefreshBulletPool();

        Debug.Log($"Модификатор применен! Рикошеты: {currentWeapon.ricochetCount}, Взрывы: {currentWeapon.explosiveBullets}");
    }
}
