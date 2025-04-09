using UnityEngine;
using TMPro; // O UnityEngine.UI si usas texto Legacy

public class UIManager : MonoBehaviour
{
    // Asigna estos en el Inspector
    public PlayerStats playerStats;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI capacityText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI radiusText;
    public TextMeshPro currentTrashValueText; // <-- ¡NUEVO! Asigna tu nuevo texto aquí

    void Update()
    {
        if (playerStats != null)
        {
            if (moneyText != null) moneyText.text = "$" + playerStats.money;
            if (capacityText != null) capacityText.text = "" + playerStats.maxTrashCapacity;
            if (speedText != null) speedText.text = playerStats.moveSpeedMultiplier.ToString("F1") + "x";
            if (radiusText != null) radiusText.text = "" + playerStats.trashPickupRadius.ToString("F1");

            // --- NUEVA LÓGICA PARA VALOR DE BASURA ACTUAL ---
            if (currentTrashValueText != null)
            {
                int currentTrash = playerStats.currentTrash;
                if (currentTrash <= 0)
                {
                    currentTrashValueText.text = "$0";
                }
                else
                {
                    // Calcula valor con la fórmula triangular
                    int potentialValue = currentTrash * (currentTrash + 1) / 2;
                    currentTrashValueText.text = "+$" + potentialValue;
                }
            }
            // ------------------------------------------------
        }
    }
}