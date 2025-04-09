using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private PlayerStats playerStats;
    private Camera mainCamera;
    private Collider2D playerCollider;
    private Vector2 playerExtents;

    private AudioManager audioManager;
    [Header("Walk Sound Settings")]
    [Tooltip("Base time between steps at speed multiplier 1.0")]
    public float baseWalkSoundInterval = 0.5f;
    [Tooltip("Fastest possible time between steps")]
    public float minWalkSoundInterval = 0.15f;
    [Tooltip("Speed multiplier at which the walk sound reaches minimum interval")]
    public float maxSpeedMultiplierForMinInterval = 3.0f; // New variable
    [Tooltip("Minimum random pitch variation")]
    public float minWalkPitch = 0.9f;
    [Tooltip("Maximum random pitch variation")]
    public float maxWalkPitch = 1.1f;
    private float walkTimer = 0f;
    private bool wasMovingLastFrame = false;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerStats = GetComponent<PlayerStats>();
        playerCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        audioManager = AudioManager.instance;
        if (mainCamera == null) { enabled = false; return; }
        playerExtents = playerCollider.bounds.extents;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;
        HandleWalkSound();
    }

    void FixedUpdate()
    {
        if (playerStats == null) return;
        rb.velocity = moveInput * moveSpeed * playerStats.moveSpeedMultiplier;
    }

    void LateUpdate()
    {
        ClampPositionToCameraView();
    }

    void HandleWalkSound()
    {
        if (audioManager == null || playerStats == null) return;
        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            // --- Gradual Interval Calculation using Lerp ---
            // Normalize speed multiplier between 1.0 and the max defined speed
            float normalizedSpeed = Mathf.InverseLerp(1.0f, maxSpeedMultiplierForMinInterval, playerStats.moveSpeedMultiplier);
            // Interpolate between base interval (at speed 1.0) and min interval (at max speed)
            float currentWalkInterval = Mathf.Lerp(baseWalkSoundInterval, minWalkSoundInterval, normalizedSpeed);
            // Ensure it doesn't go below absolute minimum just in case
            currentWalkInterval = Mathf.Max(minWalkSoundInterval, currentWalkInterval);
            // --- End Lerp Calculation ---

            walkTimer += Time.deltaTime;

            if (walkTimer >= currentWalkInterval)
            {
                float randomPitch = Random.Range(minWalkPitch, maxWalkPitch);
                audioManager.Play("Walk", randomPitch);
                walkTimer -= currentWalkInterval;
            }
        }
        else
        {
            walkTimer = 0f;
        }
        wasMovingLastFrame = isMoving;
    }

    void ClampPositionToCameraView()
    {
        if (mainCamera == null || playerCollider == null) return;
        float camNearClipPlane = mainCamera.nearClipPlane;
        Vector3 viewBottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, camNearClipPlane));
        Vector3 viewTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, camNearClipPlane));
        playerExtents = playerCollider.bounds.extents;
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, viewBottomLeft.x + playerExtents.x, viewTopRight.x - playerExtents.x);
        currentPos.y = Mathf.Clamp(currentPos.y, viewBottomLeft.y + playerExtents.y, viewTopRight.y - playerExtents.y);
        transform.position = currentPos;
    }
}