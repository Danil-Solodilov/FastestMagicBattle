using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("General Weapon Info")]
    public string weaponName = "Basic Weapon";
    [TextArea] public string description = "A simple weapon.";
    public Sprite icon; // Иконка для UI

    [Header("Bullet Settings")]
    public Bullet bulletPrefab; // Какой префаб пули использует это оружие
    public float baseDamage = 1f; // Базовый урон
    public float baseFireRate = 0.5f; // Базовая скорострельность (чем меньше, тем быстрее)
    public float baseProjectileSpeed = 70f; // Базовая скорость снаряда
    public float bulletSpreadAngle = 0f; // Разброс пуль (0 для точной стрельбы)
    public int projectilesPerShot = 1; // Количество пуль за один выстрел (для дробовика, например)
    public float bulletLifetime = 3f; // Время жизни пули

    [Header("Bullet Behavior")]
    public bool explosiveBullets; // Для взрывающихся снарядов
    public float explosionRadius = 3f;

    public int ricochetCount = 0; // Для рикошетящих снарядов
    public float ricochetRange = 5f;

    // Дополнительные параметры для будущего
    // public int pierceCount = 0; // Сколько врагов может пробить пуля
    // public float knockbackForce = 0f; // Сила отбрасывания врагов
}
