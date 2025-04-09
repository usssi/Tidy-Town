using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrashSpawner : MonoBehaviour
{
    public List<GameObject> trashPrefabs;
    public float spawnInterval = 3f;
    public int maxTrashCount = 20;
    public float baseNoSpawnMarginHeight = 6.0f; // Renombrado y valor base cambiado
    public float marginIncreasePerRadiusLevel = 0.2f; // Nuevo: Incremento por nivel
    public float spawnAmountMultiplier = 1.0f;

    [Tooltip("DEBUG: Shows the number of items calculated for the next spawn attempt.")]
    public int debugCalculatedSpawnAmount = 0;

    private Coroutine spawnCoroutine;
    private Camera mainCamera;
    private AudioManager audioManager;
    private UpgradeManager upgradeManager;

    void Start()
    {
        mainCamera = Camera.main;
        audioManager = AudioManager.instance;
        upgradeManager = FindObjectOfType<UpgradeManager>();

        if (mainCamera == null || audioManager == null || upgradeManager == null || trashPrefabs == null || trashPrefabs.Count == 0)
        {
            Debug.LogError("TrashSpawner missing required components or trashPrefabs list is empty/not assigned!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearAllChildren();
        }
    }

    void ClearAllChildren()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void UpdateSpawnTiming()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        StartSpawning();
    }

    private void StartSpawning()
    {
        if (spawnInterval > 0 && mainCamera != null && enabled && gameObject.activeInHierarchy)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnTrashIfPossible();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private int CalculateSpawnContribution(int level)
    {
        if (level < 0) level = 0;
        int baseAmount = 1 + (level / 10);
        float extraChance = (level % 10) * 0.1f;
        int amount = baseAmount;
        if (Random.value < extraChance)
        {
            amount++;
        }
        return amount;
    }

    void SpawnTrashIfPossible()
    {
        if (mainCamera == null || upgradeManager == null || transform.childCount >= maxTrashCount || trashPrefabs == null || trashPrefabs.Count == 0)
        {
            return;
        }

        int currentRadiusLevel = upgradeManager.GetCurrentLevel(UpgradeType.Radius);
        int currentSpeedLevel = upgradeManager.GetCurrentLevel(UpgradeType.Speed);

        int amountFromRadius = CalculateSpawnContribution(currentRadiusLevel);
        int amountFromSpeed = CalculateSpawnContribution(currentSpeedLevel);

        int combinedAmountBeforeMultiplier = Mathf.Max(1, amountFromRadius + amountFromSpeed - 1);

        float finalAmountFloat = combinedAmountBeforeMultiplier * spawnAmountMultiplier;
        int amountToSpawnThisTime = Mathf.Max(1, Mathf.RoundToInt(finalAmountFloat));

        debugCalculatedSpawnAmount = amountToSpawnThisTime;

        int spawnedCount = 0;
        for (int i = 0; i < amountToSpawnThisTime; i++)
        {
            if (transform.childCount >= maxTrashCount) break;

            float camNearClipPlane = mainCamera.nearClipPlane;
            Vector3 viewBottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, camNearClipPlane));
            Vector3 viewTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, camNearClipPlane));

            float spawnMinY = viewBottomLeft.y;
            float spawnMaxY = viewTopRight.y;
            float spawnMinX = viewBottomLeft.x;
            float spawnMaxX = viewTopRight.x;

            // Calcular el margen efectivo basado en el nivel de radio
            float effectiveNoSpawnMargin = baseNoSpawnMarginHeight + marginIncreasePerRadiusLevel * currentRadiusLevel;

            // Usar el margen efectivo calculado
            float marginBottomEdge = spawnMaxY - effectiveNoSpawnMargin;

            if (marginBottomEdge <= spawnMinY) continue;

            float effectiveSpawnMaxY = marginBottomEdge;
            float randomX = Random.Range(spawnMinX, spawnMaxX);
            float randomY = Random.Range(spawnMinY, effectiveSpawnMaxY);

            Vector2 spawnPosition = new Vector2(randomX, randomY);

            int randomIndex = Random.Range(0, trashPrefabs.Count);
            GameObject prefabToSpawn = trashPrefabs[randomIndex];

            float randomZAngle = Random.Range(0f, 360f);
            Quaternion randomRotation = Quaternion.Euler(0f, 0f, randomZAngle);

            Instantiate(prefabToSpawn, spawnPosition, randomRotation, transform);
            spawnedCount++;
        }

        if (spawnedCount > 0 && audioManager != null)
        {
            audioManager.Play("popTrash", 1f);
        }
    }

    void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    void OnEnable()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (audioManager == null) audioManager = AudioManager.instance;
        if (upgradeManager == null) upgradeManager = FindObjectOfType<UpgradeManager>();

        if (mainCamera != null && upgradeManager != null && trashPrefabs != null && trashPrefabs.Count > 0 && gameObject.activeInHierarchy && spawnCoroutine == null)
        {
            StartSpawning();
        }
    }
}