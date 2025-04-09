// UpgradeStationUI.cs MODIFICADO
using UnityEngine;
using TMPro;

public class UpgradeStationUI : MonoBehaviour
{
    public UpgradeType stationUpgradeType;
    public GameObject infoTextObject; // Asigna el objeto hijo con TextMeshPro

    private TextMeshPro textMeshComponent;
    private UpgradeManager upgradeManager;
    private PlayerStats playerStats; // Referencia para buscar
    private string affordableColor = "green";
    private string unaffordableColor = "red";

    void Start()
    {
        // Busca las referencias una vez
        upgradeManager = FindObjectOfType<UpgradeManager>();
        playerStats = FindObjectOfType<PlayerStats>(); // Encuentra al jugador

        if (infoTextObject != null)
        {
            textMeshComponent = infoTextObject.GetComponent<TextMeshPro>();
            if (textMeshComponent != null)
            {
                textMeshComponent.richText = true;
                infoTextObject.SetActive(true); // Activar al inicio
            }
            else
            {
                infoTextObject.SetActive(false); // Desactivar si no hay texto
            }
        }
        if (upgradeManager == null || playerStats == null || textMeshComponent == null)
        {
            enabled = false; // Desactivar script si falta algo
            if (infoTextObject != null) infoTextObject.SetActive(false);
            return;
        }
    }

    void Update()
    {
        // Actualizar constantemente el texto
        UpdateTextContent();
    }

    private void UpdateTextContent()
    {
        // Las comprobaciones null ahora están en Start
        int currentLevel = upgradeManager.GetCurrentLevel(stationUpgradeType);
        int cost = upgradeManager.GetUpgradeCost(stationUpgradeType);
        bool canAfford = playerStats.money >= cost;

        string line1 = $"Lvl {currentLevel}";
        string effectString = "";
        string costString = $"${cost}";

        switch (stationUpgradeType)
        {
            case UpgradeType.Speed:
                float nextSpeed = upgradeManager.CalculateSpeedMultiplierForLevel(currentLevel + 1);
                effectString = $"{nextSpeed:F1}x";
                break;
            case UpgradeType.Capacity:
                int nextCapacity = upgradeManager.CalculateCapacityForLevel(playerStats, currentLevel + 1);
                effectString = $"{nextCapacity}";
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

    // HideText ya no es necesaria si siempre está visible
    // public void HideText() { ... }
}