using UnityEngine;
using TMPro;
using System.Globalization;

public class UpgradeStationUI : MonoBehaviour
{
    public UpgradeType stationUpgradeType;
    public GameObject infoTextObject;

    private TextMeshPro textMeshComponent;
    private UpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private string affordableColor = "green";
    private string unaffordableColor = "red";
    private CultureInfo numberFormatCulture = new CultureInfo("en-US");

    void Start()
    {
        upgradeManager = FindObjectOfType<UpgradeManager>();
        playerStats = FindObjectOfType<PlayerStats>();

        if (infoTextObject != null)
        {
            textMeshComponent = infoTextObject.GetComponent<TextMeshPro>();
            if (textMeshComponent != null)
            {
                textMeshComponent.richText = true;
                infoTextObject.SetActive(true);
            }
            else
            {
                infoTextObject.SetActive(false);
            }
        }
        if (upgradeManager == null || playerStats == null || textMeshComponent == null)
        {
            enabled = false;
            if (infoTextObject != null) infoTextObject.SetActive(false);
            return;
        }
    }

    void Update()
    {
        UpdateTextContent();
    }

    private void UpdateTextContent()
    {
        int currentLevel = upgradeManager.GetCurrentLevel(stationUpgradeType);
        int cost = upgradeManager.GetUpgradeCost(stationUpgradeType);
        bool canAfford = playerStats.money >= cost;

        string line1 = $"Lvl {currentLevel}";
        string effectString = "";
        string formattedCost = cost.ToString("N0", numberFormatCulture);
        string costString = $"${formattedCost}";

        switch (stationUpgradeType)
        {
            case UpgradeType.Speed:
                float nextSpeed = upgradeManager.CalculateSpeedMultiplierForLevel(currentLevel + 1);
                effectString = $"{nextSpeed:F1}x";
                break;
            case UpgradeType.Capacity:
                int nextCapacity = upgradeManager.CalculateCapacityForLevel(playerStats, currentLevel + 1);
                effectString = nextCapacity.ToString("N0", numberFormatCulture);
                break;
            case UpgradeType.Radius:
                float nextRadius = upgradeManager.CalculateRadiusForLevel(playerStats, currentLevel + 1);
                effectString = $"{nextRadius:F1}";
                break;
        }

        string colorTag = canAfford ? affordableColor : unaffordableColor;
        string line2 = $"<color={colorTag}>{costString} | {effectString}</color>";

        textMeshComponent.text = line1 + "\n" + line2;
    }
}