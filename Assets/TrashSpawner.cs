using UnityEngine;
using System.Collections;

public class TrashSpawner : MonoBehaviour
{
    public GameObject trashPrefab;
    public float spawnInterval = 3f;
    public int maxTrashCount = 20;
    // Removed itemsToSpawnPerInterval variable
    public float noSpawnMarginHeight = 1.5f;

    private Coroutine spawnCoroutine;
    private Camera mainCamera;
    private AudioManager audioManager;
    private UpgradeManager upgradeManager; // <-- NUEVO: Referencia a UpgradeManager

    void Start()
    {
        mainCamera = Camera.main;
        audioManager = AudioManager.instance;
        upgradeManager = FindObjectOfType<UpgradeManager>(); // <-- NUEVO: Encontrar UpgradeManager

        if (mainCamera == null || audioManager == null || upgradeManager == null) // <-- NUEVO: Comprobar UpgradeManager
        {
            enabled = false;
            return;
        }
        // StartSpawning still likely called by UpgradeManager after its init
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

    void SpawnTrashIfPossible()
    {
        if (mainCamera == null || upgradeManager == null || transform.childCount >= maxTrashCount) return;

        // --- Calcular cantidad a spawnear AHORA ---
        int currentRadiusLevel = upgradeManager.GetCurrentLevel(UpgradeType.Radius);
        int baseAmount = 1 + (currentRadiusLevel / 10); // 1 para 0-9, 2 para 10-19, etc.
        float extraChance = (currentRadiusLevel % 10) * 0.1f; // 0% a 90%

        int amountToSpawnThisTime = baseAmount;
        if (Random.value < extraChance)
        {
            amountToSpawnThisTime++;
        }
        // -----------------------------------------

        int spawnedCount = 0;
        for (int i = 0; i < amountToSpawnThisTime; i++) // Usa la cantidad calculada
        {
            if (transform.childCount >= maxTrashCount) break;

            float camNearClipPlane = mainCamera.nearClipPlane;
            Vector3 viewBottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, camNearClipPlane));
            Vector3 viewTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, camNearClipPlane));

            float spawnMinY = viewBottomLeft.y;
            float spawnMaxY = viewTopRight.y;
            float spawnMinX = viewBottomLeft.x;
            float spawnMaxX = viewTopRight.x;

            float marginBottomEdge = spawnMaxY - noSpawnMarginHeight;

            if (marginBottomEdge <= spawnMinY) continue;

            float effectiveSpawnMaxY = marginBottomEdge;
            float randomX = Random.Range(spawnMinX, spawnMaxX);
            float randomY = Random.Range(spawnMinY, effectiveSpawnMaxY);

            Vector2 spawnPosition = new Vector2(randomX, randomY);
            Instantiate(trashPrefab, spawnPosition, Quaternion.identity, transform);
            spawnedCount++;
        }

        if (spawnedCount > 0 && audioManager != null)
        {
            audioManager.Play("popTrash", 1f);
        }
    }

    void OnDisable()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
    }
    void OnEnable()
    {
        if (mainCamera != null && upgradeManager != null && gameObject.activeInHierarchy && spawnCoroutine == null)
        {
            StartSpawning(); // Restart if needed and manager exists
        }
    }
}