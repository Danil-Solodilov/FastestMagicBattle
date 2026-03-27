using UnityEngine;
using UnityEngine.UI;
using TMPro; // Для TextMeshPro

public class UpgradeOptionUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button selectButton;

    private UpgradeData _currentUpgradeData;
    private UpgradeManager _upgradeManager;

    public void SetUpgradeData(UpgradeData data, UpgradeManager manager)
    {
        _currentUpgradeData = data;
        _upgradeManager = manager;

        if (_currentUpgradeData.icon != null) iconImage.sprite = _currentUpgradeData.icon;
        nameText.text = _currentUpgradeData.upgradeName;
        descriptionText.text = _currentUpgradeData.description;

        selectButton.onClick.RemoveAllListeners(); // Очищаем старые слушатели
        selectButton.onClick.AddListener(OnSelectUpgrade); // Добавляем новый
    }

    private void OnSelectUpgrade()
    {
        if (_upgradeManager != null && _currentUpgradeData != null)
        {
            _upgradeManager.SelectUpgrade(_currentUpgradeData);
        }
    }
}
