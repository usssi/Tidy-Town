using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int baseMaxTrashCapacity = 3;
    public float basePickupRadius = 1.5f;
    public float baseStationInteractRadius = 1.0f; // Variable añadida

    public int currentTrash = 0;
    public int maxTrashCapacity;
    public int money = 0;
    public float moveSpeedMultiplier = 1f;
    public float trashPickupRadius;
    public float stationInteractRadius; // Quitamos inicializador aquí

    void Awake()
    {
        maxTrashCapacity = baseMaxTrashCapacity;
        trashPickupRadius = basePickupRadius;
        stationInteractRadius = baseStationInteractRadius; // Inicializa desde la base
        moveSpeedMultiplier = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            money += 999999999;
            // Considera guardar la referencia a AudioManager en Start si lo usas mucho
            FindObjectOfType<AudioManager>()?.Play("Sell Trash Result", 1f);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            money = 0;
            FindObjectOfType<AudioManager>()?.Play("noMoney", 1f);
        }
    }
}