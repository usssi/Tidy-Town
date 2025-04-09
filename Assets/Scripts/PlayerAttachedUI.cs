using UnityEngine;
using TMPro;
using System.Globalization;

public class PlayerAttachedUI : MonoBehaviour
{
    public TextMeshPro textMeshComponent;
    public Color defaultColor = Color.white;
    public Color fullColor = Color.red;

    private PlayerStats playerStats;
    private CultureInfo numberFormatCulture = new CultureInfo("en-US");

    void Start()
    {
        playerStats = GetComponentInParent<PlayerStats>();

        if (textMeshComponent == null)
        {
            textMeshComponent = GetComponentInChildren<TextMeshPro>();
        }

        if (playerStats == null || textMeshComponent == null)
        {
            enabled = false;
            if (textMeshComponent != null) textMeshComponent.gameObject.SetActive(false);
        }
        else
        {
            textMeshComponent.color = defaultColor;
        }
    }

    void Update()
    {
        if (playerStats != null && textMeshComponent != null)
        {
            string formattedCurrent = playerStats.currentTrash.ToString("N0", numberFormatCulture);
            string formattedMax = playerStats.maxTrashCapacity.ToString("N0", numberFormatCulture);
            textMeshComponent.text = formattedCurrent + "/" + formattedMax;

            if (playerStats.currentTrash >= playerStats.maxTrashCapacity)
            {
                textMeshComponent.color = fullColor;
            }
            else
            {
                textMeshComponent.color = defaultColor;
            }
        }
    }
}