using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Для работы с UI элементами

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject upgradePanel; // Панель, которая появляется при уровне
    [SerializeField] private List<UpgradeOptionUI> upgradeOptions; // Список UI-элементов для выбора апгрейдов

    [Header("Available Upgrades")]
    // Список всех возможных апгрейдов в игре
    [SerializeField] private List<UpgradeData> allAvailableUpgrades;

    [Header("Dependencies")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerShooting playerShooting;

    private void Awake()
    {
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
        // Подписываемся на событие получения уровня
        PlayerStats.OnLevelUp += ShowUpgradePanel;
        upgradePanel.SetActive(false); // Убедимся, что панель изначально скрыта
    }

    private void OnDestroy()
    {
        // Отписываемся от события, чтобы избежать ошибок
        PlayerStats.OnLevelUp -= ShowUpgradePanel;
    }

    private void ShowUpgradePanel(int newLevel)
    {
        // Ставим игру на паузу
        Time.timeScale = 0f;
        upgradePanel.SetActive(true);

        // Выбираем случайные апгрейды (пока просто, потом доработаем)
        List<UpgradeData> chosenUpgrades = ChooseRandomUpgrades(upgradeOptions.Count);

        // Обновляем UI каждой кнопки апгрейда
        for (int i = 0; i < upgradeOptions.Count; i++)
        {
            if (i < chosenUpgrades.Count)
            {
                upgradeOptions[i].SetUpgradeData(chosenUpgrades[i], this);
                upgradeOptions[i].gameObject.SetActive(true);
            }
            else
            {
                upgradeOptions[i].gameObject.SetActive(false); // Скрываем лишние кнопки
            }
        }
    }

    private List<UpgradeData> ChooseRandomUpgrades(int count)
    {
        // TODO: Более сложная логика выбора (учитывать, что уже есть у игрока, редкость и т.д.)
        if (allAvailableUpgrades.Count == 0)
        {
            Debug.LogError("Нет доступных апгрейдов в списке 'allAvailableUpgrades'!");
            return new List<UpgradeData>();
        }

        List<UpgradeData> chosen = new List<UpgradeData>();
        List<UpgradeData> tempPool = new List<UpgradeData>(allAvailableUpgrades); // Временный пул для выбора

        for (int i = 0; i < count; i++)
        {
            if (tempPool.Count == 0) break;

            int randomIndex = Random.Range(0, tempPool.Count);
            chosen.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex); // Удаляем, чтобы не выбрать дважды одно и то же
        }
        return chosen;
    }

    public void SelectUpgrade(UpgradeData upgrade)
    {
        upgrade.ApplyUpgrade(playerStats, playerShooting);

        // Скрываем панель и снимаем игру с паузы
        upgradePanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
