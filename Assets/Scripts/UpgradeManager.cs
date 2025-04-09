using UnityEngine;
using TMPro;
using System.Globalization;

public enum UpgradeType
{
    Speed,
    Capacity,
    Radius
}

public class UpgradeManager : MonoBehaviour
{
    [Header("References")]
    public TrashSpawner trashSpawnerReference;
    private CameraController cameraController;
    private PlayerStats playerStatsInstance;

    [Space(10)]
    [Header("Spawner Scaling (Linked to Upgrades)")]
    public int baseMaxTrashSpawnCount = 10;
    public float baseSpawnInterval = 3.0f;
    public float spawnIntervalDecayFactor = 0.835f;
    public float spawnIntervalTargetMin = 0.01f;
    public float minSpawnInterval = 0.1f;
    public int baseItemsToSpawn = 1;

    [Space(10)]
    [Header("Camera Scaling (Linked to Radius Upgrade)")]
    public float baseOrthographicSize = 7f;
    public float orthoSizeIncreasePerRadiusLevel = 0.5f;

    [Space(10)]
    [Header("Self Transform Scaling (Linked to Radius Upgrade)")]
    public float selfScaleIncreasePerRadiusLevel = 0.3f;
    public float selfPositionYIncreasePerRadiusLevel = 0.3f;

    [Space(10)]
    [Header("Speed Upgrade Config")]
    public int speedBaseCost = 12;
    public float speedCostTriangularFactor = 15f;
    public float speedBaseMultiplierIncrease = 0.25f;

    [Space(10)]
    [Header("Capacity Upgrade Config")]
    public int capacityBaseCost = 18;
    public float capacityCostTriangularFactor = 25f;

    [Space(10)]
    [Header("Radius Upgrade Config (Player)")] // Encabezado actualizado
    public int radiusBaseCost = 24;
    public float radiusCostTriangularFactor = 35f;
    [Tooltip("How much the Player's TRASH pickup radius increases per Radius Level.")]
    public float radiusBaseIncrease = 0.5f; // Ahora solo para pickup
    [Tooltip("How much the Player's STATION interaction radius increases per Radius Level.")]
    public float stationInteractRadiusIncreasePerLevel = 0.1f; // Nueva variable específica

    [HideInInspector] public int speedLevel = 0;
    [HideInInspector] public int capacityLevel = 0;
    [HideInInspector] public int radiusLevel = 0;

    private Camera mainCamera;
    private float initialCameraY;
    private Vector3 initialPosition;
    private Vector3 initialScale;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            initialCameraY = mainCamera.transform.position.y;
            cameraController = mainCamera.GetComponent<CameraController>();
        }
        initialPosition = transform.position;
        initialScale = transform.localScale;
        playerStatsInstance = FindObjectOfType<PlayerStats>();
        if (playerStatsInstance == null)
        {
            Debug.LogError("UpgradeManager could not find PlayerStats instance in the scene!");
        }
        InitializeSystems();
    }

    void InitializeSystems()
    {
        if (trashSpawnerReference != null)
        {
            trashSpawnerReference.maxTrashCount = CalculateMaxTrashCountForLevel(capacityLevel);
            UpdateSpawnInterval();
            trashSpawnerReference.UpdateSpawnTiming();
        }
        UpdateCameraState();
        UpdateSelfTransformState();
        UpdatePlayerRadii(playerStatsInstance);
    }

    void UpdateCameraState()
    {
        if (cameraController == null) return;
        float targetOrthoSize = baseOrthographicSize + orthoSizeIncreasePerRadiusLevel * radiusLevel;
        float deltaOrthoSize = targetOrthoSize - baseOrthographicSize;
        float targetCameraY = initialCameraY - deltaOrthoSize;
        cameraController.SetTargetCameraState(targetOrthoSize, targetCameraY);
    }

    void UpdateSelfTransformState()
    {
        Vector3 scaleIncreaseVector = new Vector3(selfScaleIncreasePerRadiusLevel, selfScaleIncreasePerRadiusLevel, selfScaleIncreasePerRadiusLevel);
        Vector3 targetScale = initialScale + scaleIncreaseVector * radiusLevel;

        Vector3 targetPosition = new Vector3(initialPosition.x, initialPosition.y + selfPositionYIncreasePerRadiusLevel * radiusLevel, initialPosition.z);

        transform.localScale = targetScale;
        transform.position = targetPosition;
    }

    void UpdateSpawnInterval()
    {
        if (trashSpawnerReference == null) return;
        int level = speedLevel;
        float span = baseSpawnInterval - spawnIntervalTargetMin;
        float decayPower = Mathf.Pow(spawnIntervalDecayFactor, level);
        float calculatedInterval = spawnIntervalTargetMin + span * decayPower;
        trashSpawnerReference.spawnInterval = Mathf.Max(minSpawnInterval, calculatedInterval);
    }

    void UpdatePlayerRadii(PlayerStats stats)
    {
        if (stats == null) return;
        stats.trashPickupRadius = stats.basePickupRadius + radiusBaseIncrease * radiusLevel;
        stats.stationInteractRadius = stats.baseStationInteractRadius + stationInteractRadiusIncreasePerLevel * radiusLevel; // Usa la nueva variable
    }

    public int GetCurrentLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Speed: return speedLevel;
            case UpgradeType.Capacity: return capacityLevel;
            case UpgradeType.Radius: return radiusLevel;
            default: return -1;
        }
    }

    public int GetUpgradeCost(UpgradeType type)
    {
        int currentLevel = GetCurrentLevel(type);
        float triangularFactor = 0;
        int baseCost = 0;
        switch (type)
        {
            case UpgradeType.Speed: baseCost = speedBaseCost; triangularFactor = speedCostTriangularFactor; break;
            case UpgradeType.Capacity: baseCost = capacityBaseCost; triangularFactor = capacityCostTriangularFactor; break;
            case UpgradeType.Radius: baseCost = radiusBaseCost; triangularFactor = radiusCostTriangularFactor; break;
            default: return int.MaxValue;
        }
        long triangularN = (long)currentLevel * (currentLevel + 1) / 2;
        int calculatedCost = baseCost + (int)(triangularFactor * triangularN);
        return Mathf.Max(baseCost, calculatedCost);
    }

    public bool CanAffordUpgrade(UpgradeType type, int currentMoney)
    {
        return currentMoney >= GetUpgradeCost(type);
    }

    public bool PurchaseUpgrade(UpgradeType type, PlayerStats playerStats)
    {
        if (playerStats == null || !CanAffordUpgrade(type, playerStats.money)) return false;
        int cost = GetUpgradeCost(type);
        playerStats.money -= cost;

        bool needsIntervalUpdate = false;
        bool needsCameraUpdate = false;
        bool needsSelfTransformUpdate = false;
        bool needsPlayerRadiiUpdate = false;

        switch (type)
        {
            case UpgradeType.Speed:
                speedLevel++;
                playerStats.moveSpeedMultiplier = 1.0f + speedBaseMultiplierIncrease * speedLevel;
                needsIntervalUpdate = true;
                break;
            case UpgradeType.Capacity:
                capacityLevel++;
                int N_cap = capacityLevel;
                playerStats.maxTrashCapacity = playerStats.baseMaxTrashCapacity + N_cap * (N_cap + 1) / 2 + 2 * N_cap;
                if (trashSpawnerReference != null)
                    trashSpawnerReference.maxTrashCount = CalculateMaxTrashCountForLevel(capacityLevel);
                break;
            case UpgradeType.Radius:
                radiusLevel++;
                needsPlayerRadiiUpdate = true;
                needsCameraUpdate = true;
                needsSelfTransformUpdate = true;
                break;
        }

        if (needsIntervalUpdate)
        {
            UpdateSpawnInterval();
            if (trashSpawnerReference != null)
                trashSpawnerReference.UpdateSpawnTiming();
        }
        if (needsCameraUpdate)
        {
            UpdateCameraState();
        }
        if (needsSelfTransformUpdate)
        {
            UpdateSelfTransformState();
        }
        if (needsPlayerRadiiUpdate)
        {
            UpdatePlayerRadii(playerStats);
        }

        return true;
    }

    public float CalculateSpeedMultiplierForLevel(int level) { return 1.0f + speedBaseMultiplierIncrease * level; }
    public int CalculateCapacityForLevel(PlayerStats stats, int level) { if (stats == null) return 0; int N_calc = level; return stats.baseMaxTrashCapacity + N_calc * (N_calc + 1) / 2 + 2 * N_calc; }
    public float CalculateRadiusForLevel(PlayerStats stats, int level) { if (stats == null) return 0f; return stats.basePickupRadius + radiusBaseIncrease * level; }
    int CalculateMaxTrashCountForLevel(int level) { long N = level; long triangularN = N * (N + 1) / 2; long newMaxCount = baseMaxTrashSpawnCount + triangularN + 4 * N; return (int)Mathf.Clamp(newMaxCount, 0, int.MaxValue); }
}