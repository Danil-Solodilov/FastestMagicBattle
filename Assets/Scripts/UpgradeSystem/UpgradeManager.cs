using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Sprites;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject upgradePanel; // Панель апгрейдов, которая появляется при уровне
    [SerializeField] private List<UpgradeOptionUI> upgradeOptions; // Список UI-элементов для выбора апгрейдов

    [Header("DownPanel UI")]
    [SerializeField] private TextMeshProUGUI damageText; //Текст урона
    [SerializeField] private TextMeshProUGUI attackSpeedText; //Текст скорости атаки
    [SerializeField] private TextMeshProUGUI critText; // Текст шанса крита

    [Header("Available Upgrades")]
    // Список ВСЕХ возможных апгрейдов в игре. Сюда добавляются все SO апгрейдов.
    [SerializeField] private List<UpgradeData> allUpgrades;

    [Header("Dependencies")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private SkillManager skillManager;

    [Header("Balance Settings")]
    [Range(0, 1)][SerializeField] private float projectileUpgradeChance = 0.1f; // 10% шанс на смену снарядов

    [Header("Rarity Weights")] // Шанс выпадения апгрейдов
    [SerializeField] private int commonWeight = 70;    // 70% шанс
    [SerializeField] private int rareWeight = 20;      // 20% шанс
    [SerializeField] private int epicWeight = 10;       // 10% шанс

    private void Awake()
    {
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        if (skillManager == null) skillManager = GetComponent<SkillManager>();

        if (playerStats == null || skillManager == null)
        {
            Debug.LogError("UpgradeManager: PlayerStats or SkillManager not found! Please assign them.");
            enabled = false; // Отключаем скрипт, если нет основных зависимостей
            return;
        }

        // Подписываемся на событие получения уровня
        PlayerStats.OnLevelUp += HandleLevelUp;
        if (upgradePanel != null) upgradePanel.SetActive(false); // Убедимся, что панель изначально скрыта

        // Обновление UI панели статов
        attackSpeedText.text = $"{70}";
        damageText.text = $"{10}";
        critText.text = $"{playerStats.critChance}";
    }

    private void OnDestroy()
    {
        // Отписываемся от события, чтобы избежать ошибок
        PlayerStats.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int level)
    {
        Time.timeScale = 0f;
        List<UpgradeData> choices = new List<UpgradeData>();

        // --- ЛОГИКА РИТМА ИГРЫ ---

        // 1. Ультимейты (10, 20, 30...)
        if (level % 10 == 0)
        {
            if (level == 10 && skillManager.ultimateSkill == null)
                choices = GetUpgradesByType(UpgradeType.UltimateUnlock, 3);
            else
                choices = GetUpgradesByType(UpgradeType.UltimateUpgrade, 3);
        }
        // 2. Скиллы (5, 15, 25, 35...)
        else if (level % 10 == 5)
        {
            choices = GetUpgradesByType(UpgradeType.SkillUnlock, 3);
        }
        // 3. Статы и Снаряды (Все остальные уровни)
        else
        {
            choices = GetRandomStatsAndProjectiles(3);
        }

        // --- ПРОВЕРКА НА ПУСТУЮ ПАНЕЛЬ (FALLBACK) ---
        // Если после всех фильтров у нас меньше 3-х апгрейдов, забиваем пустые места статами
        if (choices.Count < 3)
        {
            FillMissingWithStats(choices, 3);
        }

        ShowUpgradeChoicesUI(choices);
    }

    // Метод заполнения пустот обычными статами
    private void FillMissingWithStats(List<UpgradeData> currentList, int totalNeeded)
    {
        // 1. Берем только настоящие статы, исключая модификаторы пуль и скиллы
        var statsPool = allUpgrades.Where(u =>
            (u.type == UpgradeType.PlayerStatUpgrade || u.type == UpgradeType.MainWeaponUpgrade) &&
            !(u is BulletModifierUpgradeData) &&
            !(u is SkillUnlockUpgrade)
        ).ToList();

        // Применяем фильтр к апгрейдам статов
        statsPool = FilterUnavailableUpgrades(statsPool);

        // 2. УДАЛЯЕМ из пула те группы, которые УЖЕ есть в currentList
        // (Чтобы если на 5 уровне выпал редкий скилл с группой "Fire", статы с этой группой не подмешались)
        foreach (var alreadyChosen in currentList)
        {
            if (!string.IsNullOrEmpty(alreadyChosen.groupID))
            {
                statsPool.RemoveAll(u => u.groupID == alreadyChosen.groupID);
            }
            else
            {
                statsPool.Remove(alreadyChosen);
            }
        }

        // 3. Заполняем пустые места
        while (currentList.Count < totalNeeded && statsPool.Count > 0)
        {
            // Используем взвешенный рандом для соблюдения редкости
            UpgradeData sChoice = GetWeightedRandomUpgrade(statsPool);
            if (sChoice != null)
            {
                currentList.Add(sChoice);

                // Удаляем всю группу выбранного стата, чтобы не было дублей вариаций
                if (!string.IsNullOrEmpty(sChoice.groupID))
                {
                    statsPool.RemoveAll(u => u.groupID == sChoice.groupID);
                }
                else
                {
                    statsPool.Remove(sChoice);
                }
            }
            else break;
        }
    }

    // Метод для получения УНИКАЛЬНЫХ апгрейдов по типу (для уровней 5, 10 и т.д.)
    private List<UpgradeData> GetUpgradesByType(UpgradeType type, int count)
    {
        List<UpgradeData> pool = allUpgrades.Where(u => u.type == type).ToList();

        // Применяем фильтр к уникальным аппгрейдам
        pool = FilterUnavailableUpgrades(pool);

        if (type == UpgradeType.SkillUnlock)
        {
            pool = pool.Where(u => {
                var skillUpgrade = u as SkillUnlockUpgrade;
                if (skillUpgrade == null) return false;

                // Проверяем, есть ли уже такой скилл у игрока (сравниваем по имени)
                bool alreadyHas = skillManager.activeSkills.Any(s =>
                    s != null && s.skillName == skillUpgrade.skillToUnlock.skillName);

                return !alreadyHas; // Оставляем только те, которых НЕТ
            }).ToList();
        }

        // Доп. фильтр для ульты (только для той, что у игрока)
        if (type == UpgradeType.UltimateUpgrade && skillManager.ultimateSkill != null)
        {
            pool = pool.Where(u => {
                var upg = u as UltimateUpgradeData;
                return upg != null && upg.relatedUltimate.skillName == skillManager.ultimateSkill.skillName;
            }).ToList();
        }

        // Перемешиваем и берем столько, сколько есть
        List<UpgradeData> chosen = pool.OrderBy(x => UnityEngine.Random.value).Take(count).ToList();

        // Если после фильтрации скиллов осталось меньше 3 (например, игрок уже собрал все скиллы в игре)
        // мы дозаполняем список обычными статами
        if (chosen.Count < count)
        {
            FillMissingWithStats(chosen, count);
        }

        return chosen;
    }

    // Случайная смесь статов и редких "типов снарядов"
    private List<UpgradeData> GetRandomStatsAndProjectiles(int count)
    {
        List<UpgradeData> chosen = new List<UpgradeData>();

        // 1. Подготовка пулов (используем Distinct на случай, если в инспекторе случайно задублировали ссылки)
        List<UpgradeData> statsPool = allUpgrades
            .Where(u => u.type == UpgradeType.PlayerStatUpgrade || u.type == UpgradeType.MainWeaponUpgrade)
            .Distinct().ToList();

        // Применяем фильтры к аппгрейдам статов и снарядов
        statsPool = FilterUnavailableUpgrades(statsPool);

        // Пул типов снарядов
        List<UpgradeData> projectilePool = allUpgrades
        .Where(u => u.type == UpgradeType.BulletsUpgrade)
        .Distinct().ToList();

        // Применяем фильтры к аппгрейдам статов и снарядов
        projectilePool = FilterUnavailableUpgrades(projectilePool);

        // Шанс на выпадение типа снарядов
        if (UnityEngine.Random.value < projectileUpgradeChance && projectilePool.Count > 0)
        {
            WeaponData current = playerShooting.GetCurrentRuntimeWeaponData();

            // Фильтруем пул: убираем то, что уже активно у игрока
            var validProjectiles = projectilePool.Where(p => {
                var proj = p as BulletModifierUpgradeData;
                if (proj == null) return false;
                if (proj.projectileType == ProjectileType.Explosive && current.explosiveBullets) return false;
                if (proj.projectileType == ProjectileType.Ricochet && current.ricochetCount > 0) return false;
                return true;
            }).ToList();

            if (validProjectiles.Count > 0)
            {
                // Берем один случайный спец-апгрейд
                UpgradeData pChoice = GetWeightedRandomUpgrade(validProjectiles);
                if (pChoice != null)
                {
                    chosen.Add(pChoice);
                    // Если у снарядов есть группы, удаляем их
                    if (!string.IsNullOrEmpty(pChoice.groupID))
                        projectilePool.RemoveAll(u => u.groupID == pChoice.groupID);
                }
            }
        }

        int safetyThrottle = 0; // Защита от бесконечного цикла
        while (chosen.Count < count && statsPool.Count > 0 && safetyThrottle < 100)
        {
            safetyThrottle++;
            UpgradeData sChoice = GetWeightedRandomUpgrade(statsPool);

            // Проверяем, нет ли такого апгрейда уже в списке выбранных (chosen)
            if (sChoice != null)
            {
                chosen.Add(sChoice);

                if (!string.IsNullOrEmpty(sChoice.groupID))// Удаляем из локального списка, чтобы не выбрать снова
                {
                    // Удаляем все апгрейды, у которых такой же groupID
                    statsPool.RemoveAll(u => u.groupID == sChoice.groupID);
                }
                else
                {
                    // Если группы нет, удаляем только сам объект
                    statsPool.Remove(sChoice);
                }
            }
            else break;
        }

        return chosen;
    }

    // Фильтры для отбора некоторых аппгрейдов
    private List<UpgradeData> FilterUnavailableUpgrades(List<UpgradeData> pool)
    {
        if (pool == null || playerStats == null) return pool;

        // Если шанс крита = 0 — убираем апгрейды, требующие наличия шанса крита
        if (playerStats.critChance <= 0f)
        {
            var filtered = pool.Where(u => u != null &&
            (string.IsNullOrEmpty(u.groupID) ||
             !u.groupID.Trim().Equals("CritDamage", StringComparison.OrdinalIgnoreCase))
            ).ToList();
            return filtered;
        }
        return pool;
    }

    public void SelectUpgrade(UpgradeData data)
    {
        // Если это скилл - используем логику замены
        if (data.type == UpgradeType.SkillUnlock)
        {
            var skillData = (data as SkillUnlockUpgrade).skillToUnlock;
            skillManager.AddOrReplaceSkill(skillData);
        }
        else
        {
            data.ApplyUpgrade(playerStats, playerShooting, skillManager);

            //Обновление UI текста
            attackSpeedText.text = $"{Mathf.RoundToInt(playerShooting._runtimeWeaponData.baseFireRate * 100)}";
            damageText.text = $"{Mathf.RoundToInt(playerShooting._runtimeWeaponData.baseDamage * playerStats.damageMultiplier)}";
            critText.text = $"{playerStats.critChance}";
        }

        HideUpgradePanel();
    }

    private void ShowUpgradeChoicesUI(List<UpgradeData> choices)
    {
        upgradePanel.SetActive(true);
        skillManager.CancelAiming();
        for (int i = 0; i < upgradeOptions.Count; i++)
        {
            if (i < choices.Count)
            {
                upgradeOptions[i].SetUpgradeData(choices[i], this);
                upgradeOptions[i].gameObject.SetActive(true);
            }
            else upgradeOptions[i].gameObject.SetActive(false);
        }
    }

    // Метод для закрытия панели (вызывается кнопками "Закрыть" или после выбора)
    public void HideUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);

        Time.timeScale = 1f; // Возвращаем время в игру
    }

    // Вспомогательный метод для получения веса конкретного апгрейда
    private int GetRarityWeight(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => commonWeight,
            UpgradeRarity.Rare => rareWeight,
            UpgradeRarity.Epic => epicWeight,
            _ => 10
        };
    }

    private UpgradeData GetWeightedRandomUpgrade(List<UpgradeData> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        // 1. Считаем суммарный вес всех доступных апгрейдов в пуле
        int totalWeight = 0;
        foreach (var upgrade in pool)
        {
            totalWeight += GetRarityWeight(upgrade.rarity);
        }

        // 2. Выбираем случайное число от 0 до totalWeight
        int randomValue = UnityEngine.Random.Range(0, totalWeight);

        // 3. Проходим по пулу и вычитаем веса, пока не дойдем до нуля
        int currentWeight = 0;
        foreach (var upgrade in pool)
        {
            currentWeight += GetRarityWeight(upgrade.rarity);
            if (randomValue < currentWeight)
            {
                return upgrade;
            }
        }

        return pool[0];
    }
}
