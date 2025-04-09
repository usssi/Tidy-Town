using UnityEngine;
using TMPro; // Asegúrate de tener esto

public class PlayerAttachedUI : MonoBehaviour
{
    public TextMeshPro textMeshComponent; // Asigna el objeto TextMeshPro hijo aquí
    public Color defaultColor = Color.white; // Color normal del texto
    public Color fullColor = Color.red; // Color cuando está lleno

    private PlayerStats playerStats;

    void Start()
    {
        // Busca PlayerStats en el objeto padre
        playerStats = GetComponentInParent<PlayerStats>();
        // Busca el componente de texto si no está asignado
        if (textMeshComponent == null)
        {
            textMeshComponent = GetComponentInChildren<TextMeshPro>();
        }

        // Comprobación inicial de seguridad y color
        if (playerStats == null || textMeshComponent == null)
        {
            enabled = false; // Desactiva el script si falta algo
            if (textMeshComponent != null) textMeshComponent.gameObject.SetActive(false);
        }
        else
        {
            textMeshComponent.color = defaultColor; // Establece color inicial
        }
    }

    void Update()
    {
        if (playerStats != null && textMeshComponent != null)
        {
            // Actualiza el contenido del texto
            textMeshComponent.text = playerStats.currentTrash + "/" + playerStats.maxTrashCapacity;

            // Comprueba si el inventario está lleno y cambia el color
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