using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))] // Asegura que tengamos el trigger
public class SellingPointUI : MonoBehaviour
{
    public GameObject earningsTextObject; // Asigna el objeto hijo "EarningsText" aqu�

    private TextMeshPro textMeshComponent;
    private PlayerStats playerStatsCache = null; // Para guardar referencia al jugador cercano

    void Awake()
    {
        if (earningsTextObject != null)
        {
            textMeshComponent = earningsTextObject.GetComponent<TextMeshPro>();
            // Podr�as asegurar RichText aqu� si usaras colores
            // if(textMeshComponent != null) textMeshComponent.richText = true;
            earningsTextObject.SetActive(false); // Ocultar al inicio
        }
        else
        {
            enabled = false; // Desactivar si no hay objeto de texto asignado
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // Asume que tu jugador tiene el tag "Player"
        {
            playerStatsCache = other.GetComponent<PlayerStats>();
            if (earningsTextObject != null) earningsTextObject.SetActive(true);
            UpdateEarningsText(); // Actualizar al entrar
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (earningsTextObject != null) earningsTextObject.SetActive(false);
            playerStatsCache = null; // Limpiar referencia al salir
        }
    }

    // Actualizar mientras el jugador est� dentro por si recoge/suelta basura cerca
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Asegurarse de que tenemos la referencia (por si acaso entr� antes del Start)
            if (playerStatsCache == null) playerStatsCache = other.GetComponent<PlayerStats>();
            UpdateEarningsText();
        }
    }

    void UpdateEarningsText()
    {
        if (textMeshComponent == null || playerStatsCache == null) return;

        int currentTrash = playerStatsCache.currentTrash;

        if (currentTrash <= 0)
        {
            textMeshComponent.text = "$0"; // O "" si prefieres texto vac�o
        }
        else
        {
            // Usa la misma f�rmula triangular que en PlayerInteraction
            int potentialEarnings = currentTrash * (currentTrash + 1) / 2;
            textMeshComponent.text = "$" + potentialEarnings;
        }
        // Podr�as cambiar el color aqu� tambi�n si currentTrash > 0
        // textMeshComponent.color = (currentTrash > 0) ? Color.green : Color.gray;
    }
}