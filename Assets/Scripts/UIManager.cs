using UnityEngine;
using TMPro;
using System.Globalization;

public class UIManager : MonoBehaviour
{
    public PlayerStats playerStats;
    public PlayerInteraction playerInteraction;

    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI capacityText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI radiusText;
    public TextMeshPro currentTrashValueText;

    private CultureInfo numberFormatCulture = new CultureInfo("en");

    void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null) Debug.LogError("UIManager could not find PlayerStats!");
        }

        if (playerInteraction == null && playerStats != null)
        {
            playerInteraction = playerStats.GetComponent<PlayerInteraction>();
            if (playerInteraction == null) Debug.LogError("UIManager could not find PlayerInteraction on PlayerStats GameObject!");
        }

        if (currentTrashValueText == null) Debug.LogError("Current Trash Value Text not assigned in UIManager!");
    }


    void Update()
    {
        if (playerStats != null && playerInteraction != null)
        {
            if (moneyText != null) moneyText.text = "$" + playerStats.money.ToString("N0", numberFormatCulture);

            if (capacityText != null)
            {
                string formattedCurrent = playerStats.currentTrash.ToString("N0", numberFormatCulture);
                string formattedMax = playerStats.maxTrashCapacity.ToString("N0", numberFormatCulture);
                capacityText.text = formattedCurrent + "/" + formattedMax;
            }

            if (speedText != null) speedText.text = playerStats.moveSpeedMultiplier.ToString("F1") + "x";
            if (radiusText != null) radiusText.text = playerStats.trashPickupRadius.ToString("F1");

            if (currentTrashValueText != null)
            {
                int currentTrash = playerStats.currentTrash;

                if (currentTrash <= 0)
                {
                    currentTrashValueText.text = "$0";
                }
                else
                {
                    long baseQuadraticValue = (long)currentTrash * (currentTrash + 1) / 2;
                    float calculatedValue = baseQuadraticValue * playerInteraction.sellPriceQuadraticMultiplier;
                    int potentialValue = Mathf.Max(0, Mathf.RoundToInt(calculatedValue));

                    currentTrashValueText.text = "+$" + potentialValue.ToString("N0", numberFormatCulture);
                }
            }
        }
    }
}