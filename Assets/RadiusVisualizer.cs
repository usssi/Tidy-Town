using UnityEngine;

// Attach this script to the child GameObject with the circle/ring sprite
public class RadiusVisualizer : MonoBehaviour
{
    [Tooltip("Diameter of your circle/ring sprite in Unity units when its Scale is (1, 1, 1)")]
    public float baseSpriteDiameter = 1f; // IMPORTANT: Set this accurately in the Inspector!

    private PlayerStats playerStats;
    private SpriteRenderer spriteRenderer; // Optional: for hiding if needed

    void Start()
    {
        playerStats = GetComponentInParent<PlayerStats>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // Optional

        if (playerStats == null || baseSpriteDiameter <= 0f)
        {
            // Disable if setup is invalid
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            enabled = false;
        }
        // Optional: Hide visualizer initially if desired
        // if(spriteRenderer != null) spriteRenderer.enabled = false;
    }

    void Update()
    {
        if (playerStats == null) return;

        // Optional: Show visualizer only when near trash, etc.
        // bool shouldBeVisible = CheckIfShouldBeVisible();
        // if(spriteRenderer != null) spriteRenderer.enabled = shouldBeVisible;
        // if(!shouldBeVisible) return;


        // Calculate scale based on the TRASH pickup radius
        float targetDiameter = playerStats.trashPickupRadius * 2f;
        float requiredScale = targetDiameter / baseSpriteDiameter;

        // Apply scale (assuming uniform scaling is desired)
        transform.localScale = new Vector3(requiredScale, requiredScale, 1f);
    }

    // Example placeholder for potential visibility logic
    // bool CheckIfShouldBeVisible() {
    //    // Add logic here - e.g., check if any trash is nearby?
    //    return true; // Always visible for now
    // }
}