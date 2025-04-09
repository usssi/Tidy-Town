using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int baseMaxTrashCapacity = 3;
    public float basePickupRadius = 1.5f;
    public float stationInteractRadius = 1.0f;

    public int currentTrash = 0;
    public int maxTrashCapacity;
    public int money = 0;
    public float moveSpeedMultiplier = 1f;
    public float trashPickupRadius;

    void Awake()
    {
        maxTrashCapacity = baseMaxTrashCapacity;
        trashPickupRadius = basePickupRadius;
        moveSpeedMultiplier = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            money += 100;
        }
    }
}