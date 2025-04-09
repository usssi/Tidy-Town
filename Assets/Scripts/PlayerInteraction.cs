using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    // Existing variables...
    public KeyCode interactKey = KeyCode.E; // No longer used directly but kept for reference maybe
    public float baseHoldTime = 1.0f; // Renamed for clarity
    public Image holdProgressImage;

    public string trashTag = "Trash";
    public string sellingPointTag = "RecyclingBin";
    public string speedStationTag = "SpeedUpgradeStation";
    public string capacityStationTag = "CapacityUpgradeStation";
    public string radiusStationTag = "RadiusUpgradeStation";

    public bool pickupAllInRadius = true; // Kept, but logic focuses on stations/selling

    [Tooltip("Multiplier applied to the quadratic sell price formula (n*(n+1)/2). Default is 1.")]
    public float sellPriceQuadraticMultiplier = 1.0f;

    // --- New Variables for Acceleration ---
    [Range(0.1f, 1.0f)]
    public float holdTimeAccelerationFactor = 0.8f; // Multiplier for hold time reduction (0.8 = 20% faster each time)
    public float minHoldTime = 0.1f; // Minimum possible hold time
    private float currentEffectiveHoldTime; // The actual hold time needed for the NEXT interaction in a sequence
    private bool wasLastActionSuccessfulUpgrade = false; // Flag to track consecutive upgrades
    // ------------------------------------

    private PlayerStats playerStats;
    private UpgradeManager upgradeManager;
    private AudioManager audioManager;

    private float currentHoldTime = 0f;
    private bool isNearInteractable = false;
    private GameObject currentInteractableObject = null;
    private GameObject previousInteractableObject = null;
    private UpgradeType currentStationType = UpgradeType.Speed;
    private bool isHoldingInteract = false;
    private bool currentInteractionIsPossible = false;
    private UpgradeStationUI currentStationUIScript = null;
    private bool playedFailSoundThisAction = false;
    private string activeHoldSoundName = null;


    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        upgradeManager = FindObjectOfType<UpgradeManager>();
        audioManager = AudioManager.instance;

        // Initialize effective hold time to the base value
        currentEffectiveHoldTime = baseHoldTime;

        if (holdProgressImage != null)
        {
            holdProgressImage.fillAmount = 0;
            holdProgressImage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Hold Progress Image not assigned in PlayerInteraction.");
        }

        if (playerStats == null) Debug.LogError("PlayerStats component not found on player!");
        if (upgradeManager == null) Debug.LogError("UpgradeManager not found in scene!");
        if (audioManager == null) Debug.LogWarning("AudioManager instance not found.");
    }

    void Update()
    {
        previousInteractableObject = currentInteractableObject;
        FindInteractable(); // Find closest interactable first
        CheckInteractionPossibility(); // Check if current interaction is valid (money, items etc.)

        // --- Interaction Input ---
        // Allow both E and Space for interaction
        bool interactionKeyDown = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
        bool interactionKeyPressed = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space);
        bool interactionKeyUp = Input.GetKeyUp(KeyCode.E) || Input.GetKeyUp(KeyCode.Space);
        // -----------------------


        // Handle hold interaction returns true if it consumed the input (i.e., holding was active)
        bool handledByHold = HandleHoldInteraction(interactionKeyPressed);

        // If not holding (or hold finished) and pressed key, attempt instant trash pickup
        if (!isHoldingInteract && interactionKeyDown)
        {
            AttemptPickupTrashInRadius();
        }

        // --- Reset Conditions for Hold ---
        if (interactionKeyUp)
        {
            // If key is released, always reset the hold state and acceleration
            ResetHoldState(true); // Force reset acceleration
        }
        // If player moves away OR the current interactable changes OR interaction becomes impossible WHILE holding
        if (isHoldingInteract && (!isNearInteractable || currentInteractableObject != previousInteractableObject || !currentInteractionIsPossible))
        {
            ResetHoldState(true); // Force reset acceleration
        }
        // ---------------------------------
    }


    void AttemptPickupTrashInRadius()
    {
        if (playerStats == null) return;
        // Prioritize stations/selling points if holding near them - don't pick up trash simultaneously
        if (isHoldingInteract || (isNearInteractable && IsStationOrSellingPoint(currentInteractableObject)))
        {
            return;
        }

        if (playerStats.currentTrash >= playerStats.maxTrashCapacity)
        {
            PlayInventoryFullSound();
            return;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, playerStats.trashPickupRadius);
        int pickedUpCount = 0;
        List<GameObject> trashToPick = new List<GameObject>();

        // Collect all trash within radius first
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag(trashTag))
            {
                trashToPick.Add(col.gameObject);
            }
        }

        // Sort by distance (optional, but often feels better)
        trashToPick = trashToPick.OrderBy(go => (go.transform.position - transform.position).sqrMagnitude).ToList();

        // Attempt to pick up sorted trash until full
        foreach (GameObject trash in trashToPick)
        {
            if (playerStats.currentTrash >= playerStats.maxTrashCapacity)
            {
                PlayInventoryFullSound(); // Play sound once when hitting capacity
                break; // Stop picking up
            }
            if (AttemptPickupTrash(trash)) // AttemptPickupTrash now handles sounds internally
            {
                pickedUpCount++;
            }
        }

        // Play pickup sound only once if multiple items picked up? Or rely on AttemptPickupTrash sound?
        // Current implementation plays sound for each item in AttemptPickupTrash.
    }

    // Renamed parameter for clarity
    bool HandleHoldInteraction(bool isInteractionKeyPressed)
    {
        UpgradeType stationTypeForAction = UpgradeType.Speed; // Default
        bool isPotentiallyUpgrading = IsUpgradeStation(currentInteractableObject, out stationTypeForAction);

        // Only proceed if near a valid interactable object (station or selling point)
        if (isNearInteractable && currentInteractableObject != null && IsStationOrSellingPoint(currentInteractableObject))
        {
            // --- Start Hold ---
            if (isInteractionKeyPressed && !isHoldingInteract)
            {
                // Check if the interaction is possible *before* starting the hold
                CheckInteractionPossibility(); // Ensure possibility state is fresh
                if (!currentInteractionIsPossible)
                {
                    PlayInteractionFailSound(); // Play generic fail (no money/no trash)
                    return false; // Don't start hold if impossible
                }

                // Start the hold process
                isHoldingInteract = true;
                currentHoldTime = 0f;
                // Reset effective time only if the previous hold wasn't a successful upgrade continuation
                if (!wasLastActionSuccessfulUpgrade)
                {
                    currentEffectiveHoldTime = baseHoldTime; // Start with base speed
                }
                wasLastActionSuccessfulUpgrade = false; // Reset flag for the new hold sequence
                playedFailSoundThisAction = false;
                if (holdProgressImage != null) holdProgressImage.gameObject.SetActive(true);

                // --- Play appropriate HOLD sound ---
                activeHoldSoundName = GetHoldSoundName(isPotentiallyUpgrading, stationTypeForAction);
                if (audioManager != null && activeHoldSoundName != null)
                {
                    audioManager.Play(activeHoldSoundName, 1f);
                }
                // -----------------------------------
            }

            // --- Process Hold ---
            if (isHoldingInteract && isInteractionKeyPressed)
            {
                // Check if interaction is STILL possible (e.g., haven't run out of money mid-hold)
                if (!currentInteractionIsPossible)
                {
                    ResetHoldState(true); // Reset including acceleration
                    PlayInteractionFailSound(); // Play fail sound again
                    return false; // Stop processing hold
                }

                currentHoldTime += Time.deltaTime;
                // Update progress bar based on the *current effective* hold time
                if (holdProgressImage != null)
                {
                    holdProgressImage.fillAmount = Mathf.Clamp01(currentHoldTime / currentEffectiveHoldTime);
                }

                // --- Hold Complete: Attempt Action ---
                if (currentHoldTime >= currentEffectiveHoldTime)
                {
                    bool actionAttempted = false;
                    bool wasSuccess = false;
                    UpgradeType typePurchased = stationTypeForAction; // Store type used for this action

                    // --- Attempt Upgrade ---
                    if (isPotentiallyUpgrading)
                    {
                        actionAttempted = true;
                        // Re-check affordability right before purchase, just in case
                        if (upgradeManager.CanAffordUpgrade(typePurchased, playerStats.money))
                        {
                            wasSuccess = upgradeManager.PurchaseUpgrade(typePurchased, playerStats);
                        }
                        else
                        {
                            wasSuccess = false; // Can't afford anymore
                            PlayNoMoneySound(); // Specific sound for failing due to cost
                        }
                    }
                    // --- Attempt Sell ---
                    else if (currentInteractableObject.CompareTag(sellingPointTag))
                    {
                        actionAttempted = true;
                        // Re-check if trash exists right before selling
                        if (playerStats != null && playerStats.currentTrash > 0)
                        {
                            AttemptSellTrash();
                            wasSuccess = true;
                        }
                        else
                        {
                            wasSuccess = false; // No trash to sell
                            PlayInteractionFailSound(); // Generic fail sound for no trash
                        }
                    }

                    // --- Handle Action Result ---
                    if (actionAttempted)
                    {
                        // Play Result Sound
                        PlayActionResultSound(wasSuccess, isPotentiallyUpgrading, typePurchased);

                        // If the action was a SUCCESSFUL UPGRADE: Accelerate next hold
                        if (wasSuccess && isPotentiallyUpgrading)
                        {
                            // Reduce effective hold time for the *next* purchase in this sequence
                            currentEffectiveHoldTime = Mathf.Max(minHoldTime, currentEffectiveHoldTime * holdTimeAccelerationFactor);
                            currentHoldTime = 0f; // Reset timer for the next cycle immediately
                            wasLastActionSuccessfulUpgrade = true; // Set flag for next iteration

                            // Stop and quickly restart hold sound for continuous feel
                            if (audioManager != null && activeHoldSoundName != null)
                            {
                                audioManager.Stop(activeHoldSoundName);
                                // Check if STILL possible to interact before restarting sound
                                CheckInteractionPossibility();
                                if (currentInteractionIsPossible)
                                {
                                    audioManager.Play(activeHoldSoundName, 1f);
                                }
                                else
                                {
                                    // If next interaction isn't possible, reset fully
                                    ResetHoldState(true);
                                    return true; // Return true as we handled the input cycle
                                }
                            }

                            // Re-check possibility for the *next* upgrade immediately
                            CheckInteractionPossibility();
                            // Keep holding state active, DO NOT ResetHoldState here
                            return true; // Consumed input, holding continues
                        }
                        else
                        {
                            // If action failed OR was a sell action, reset the hold state fully
                            ResetHoldState(true); // Reset including acceleration
                            // If selling was successful, still reset acceleration.
                            // If purchase failed, reset acceleration.
                            return true; // Consumed input, but hold ends/resets
                        }
                    }
                    else
                    {
                        // Should not happen if logic is correct, but as a fallback:
                        ResetHoldState(true);
                        return true;
                    }
                }
                // --- Still Holding, Not Complete Yet ---
                return true; // Input consumed by active hold
            }
        }

        // If code reaches here, hold wasn't active or conditions not met
        // If the key is *not* pressed, but we *were* holding, reset.
        if (isHoldingInteract && !isInteractionKeyPressed)
        {
            ResetHoldState(true); // Key released, reset acceleration
        }
        return false; // Input not consumed by hold logic this frame
    }

    // Gets the name of the sound to play while holding interaction key
    string GetHoldSoundName(bool isUpgrade, UpgradeType type)
    {
        if (isUpgrade)
        {
            if (type == UpgradeType.Speed) return "Upgrade Hold 1";
            if (type == UpgradeType.Capacity) return "Upgrade Hold 2";
            if (type == UpgradeType.Radius) return "Upgrade Hold 3";
        }
        else if (currentInteractableObject != null && currentInteractableObject.CompareTag(sellingPointTag))
        {
            // Only play sell hold sound if player actually has trash
            if (playerStats != null && playerStats.currentTrash > 0)
            {
                return "Sell Trash Hold";
            }
        }
        return null; // No sound if conditions aren't met (e.g., selling with no trash)
    }

    // Plays sound based on action success/failure
    void PlayActionResultSound(bool success, bool wasUpgrade, UpgradeType type)
    {
        if (audioManager == null) return;

        if (wasUpgrade)
        {
            if (success)
            {
                if (type == UpgradeType.Speed) audioManager.Play("Upgrade Result 1", 1f);
                else if (type == UpgradeType.Capacity) audioManager.Play("Upgrade Result 2", 1f);
                else if (type == UpgradeType.Radius) audioManager.Play("Upgrade Result 3", 1f);
            }
            else
            {
                // Upgrade failed (likely no money) - handled by PlayNoMoneySound already?
                // PlayNoMoneySound(); // Redundant if called elsewhere? Let's rely on the check before purchase.
            }
        }
        else if (currentInteractableObject != null && currentInteractableObject.CompareTag(sellingPointTag))
        {
            if (success)
            {
                audioManager.Play("Sell Trash Result", 1f);
            }
            else
            {
                // Selling failed (no trash) - PlayInteractionFailSound handles this?
                // PlayInteractionFailSound(); // Might be redundant
            }
        }
    }


    void CheckInteractionPossibility()
    {
        bool previousPossibility = currentInteractionIsPossible;
        currentInteractionIsPossible = false; // Assume false initially

        if (isNearInteractable && currentInteractableObject != null && playerStats != null && upgradeManager != null)
        {
            if (IsUpgradeStation(currentInteractableObject, out currentStationType))
            {
                // Check if player can afford the *next* level of the current station type
                currentInteractionIsPossible = upgradeManager.CanAffordUpgrade(currentStationType, playerStats.money);
            }
            else if (currentInteractableObject.CompareTag(sellingPointTag))
            {
                // Check if player has any trash to sell
                currentInteractionIsPossible = playerStats.currentTrash > 0;
            }
            // Note: Trash pickup is instant, not checked here for hold possibility
            // else if (currentInteractableObject.CompareTag(trashTag)) { ... }
        }

        // If interaction becomes impossible while holding, reset the hold state
        // This is now handled in the Update loop's reset conditions
        // if (!currentInteractionIsPossible && isHoldingInteract) ResetHoldState(true);
    }

    // Modified ResetHoldState to optionally reset acceleration
    void ResetHoldState(bool resetAcceleration)
    {
        // Stop the looping hold sound if it's playing
        if (isHoldingInteract && audioManager != null && activeHoldSoundName != null)
        {
            audioManager.Stop(activeHoldSoundName);
        }

        // Reset hold tracking variables
        currentHoldTime = 0f;
        isHoldingInteract = false;
        activeHoldSoundName = null;
        wasLastActionSuccessfulUpgrade = false; // Crucial: clear this flag on any hold break

        // Reset visual progress bar
        if (holdProgressImage != null)
        {
            holdProgressImage.fillAmount = 0;
            holdProgressImage.gameObject.SetActive(false);
        }

        // Conditionally reset the effective hold time back to base
        if (resetAcceleration)
        {
            currentEffectiveHoldTime = baseHoldTime;
        }
        // We don't reset playedFailSoundThisAction here, it's managed by Invoke
    }

    // Simplified fail sound logic
    void PlayInteractionFailSound()
    {
        // Determine specific cause if possible, otherwise play generic fail
        if (isNearInteractable && currentInteractableObject != null)
        {
            if (IsUpgradeStation(currentInteractableObject, out _))
            {
                if (!upgradeManager.CanAffordUpgrade(currentStationType, playerStats.money))
                {
                    PlayNoMoneySound();
                    return;
                }
            }
            else if (currentInteractableObject.CompareTag(sellingPointTag))
            {
                if (playerStats.currentTrash <= 0)
                {
                    // Play a generic "cannot interact" sound if available, or reuse noMoney/inventoryFull?
                    // Let's reuse noMoney for simplicity, or add a new sound "Cant Interact"
                    PlayNoMoneySound(); // Placeholder - maybe add a dedicated sound
                    return;
                }
            }
        }
        // If no specific sound played, potentially play a generic fail? Or rely on the specific ones.
        // For now, rely on specific sounds (NoMoney, InventoryFull).
    }


    void ResetFailSoundFlag()
    {
        playedFailSoundThisAction = false;
    }

    void PlayInventoryFullSound()
    {
        if (audioManager != null && !playedFailSoundThisAction)
        {
            audioManager.Play("Inventory Full", 1f);
            playedFailSoundThisAction = true;
            Invoke(nameof(ResetFailSoundFlag), 0.25f); // Slightly longer delay maybe
        }
    }

    void PlayNoMoneySound()
    {
        if (audioManager != null && !playedFailSoundThisAction)
        {
            audioManager.Play("noMoney", 1f);
            playedFailSoundThisAction = true;
            Invoke(nameof(ResetFailSoundFlag), 0.25f); // Slightly longer delay maybe
        }
    }

    // --- Find Closest Interactable Logic ---
    void FindInteractable()
    {
        currentInteractableObject = null;
        isNearInteractable = false;
        if (playerStats == null) return;

        float actualPickupRadius = playerStats.trashPickupRadius;
        float actualStationRadius = playerStats.stationInteractRadius;

        // Find colliders in the larger radius needed (max of pickup or station radius)
        float searchRadius = Mathf.Max(actualPickupRadius, actualStationRadius);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        GameObject closestValidObject = null;
        float minDistanceSqr = float.MaxValue;
        bool foundStationOrBin = false;

        // Prioritize Stations and Selling Points
        foreach (Collider2D col in colliders)
        {
            float distanceSqr = (col.transform.position - transform.position).sqrMagnitude;
            if (IsStationOrSellingPoint(col.gameObject))
            {
                if (distanceSqr <= actualStationRadius * actualStationRadius && distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closestValidObject = col.gameObject;
                    foundStationOrBin = true;
                }
            }
        }

        // If no station/bin found nearby, check for trash within trash pickup radius
        if (!foundStationOrBin)
        {
            minDistanceSqr = float.MaxValue; // Reset min distance for trash check
            foreach (Collider2D col in colliders)
            {
                if (col.CompareTag(trashTag))
                {
                    float distanceSqr = (col.transform.position - transform.position).sqrMagnitude;
                    if (distanceSqr <= actualPickupRadius * actualPickupRadius && distanceSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distanceSqr;
                        closestValidObject = col.gameObject;
                    }
                }
            }
        }


        currentInteractableObject = closestValidObject;
        isNearInteractable = currentInteractableObject != null;

        // Reset fail sound flag if player is no longer near anything interactable
        if (!isNearInteractable)
        {
            playedFailSoundThisAction = false;
            // Also ensure hold state is reset if player moves away from interactable
            // (This is handled in Update now)
        }
    }
    // ----------------------------------------


    bool IsStationOrSellingPoint(GameObject obj)
    {
        if (obj == null) return false;
        return obj.CompareTag(speedStationTag) ||
               obj.CompareTag(capacityStationTag) ||
               obj.CompareTag(radiusStationTag) ||
               obj.CompareTag(sellingPointTag);
    }

    bool IsUpgradeStation(GameObject obj, out UpgradeType type)
    {
        type = UpgradeType.Speed; // Default
        if (obj == null) return false;

        if (obj.CompareTag(speedStationTag)) { type = UpgradeType.Speed; return true; }
        if (obj.CompareTag(capacityStationTag)) { type = UpgradeType.Capacity; return true; }
        if (obj.CompareTag(radiusStationTag)) { type = UpgradeType.Radius; return true; }

        return false;
    }

    void AttemptSellTrash()
    {
        if (playerStats != null && playerStats.currentTrash > 0)
        {
            int itemsSold = playerStats.currentTrash;
            long baseQuadraticValue = (long)itemsSold * (itemsSold + 1) / 2;
            float calculatedPrice = baseQuadraticValue * sellPriceQuadraticMultiplier;
            int priceReceived = Mathf.Max(0, Mathf.RoundToInt(calculatedPrice));

            playerStats.money += priceReceived;
            playerStats.currentTrash = 0;

            CheckInteractionPossibility();
        }
    }

    bool AttemptPickupTrash(GameObject trashObject)
    {
        if (trashObject == null || !trashObject.CompareTag(trashTag)) return false; // Check tag for safety
        if (playerStats != null && playerStats.currentTrash < playerStats.maxTrashCapacity)
        {
            // Check if this is the last trash item *before* destroying it
            bool isLastTrash = false;
            if (upgradeManager != null && upgradeManager.trashSpawnerReference != null && trashObject.transform.parent == upgradeManager.trashSpawnerReference.transform)
            {
                // Check child count *of the spawner*
                if (upgradeManager.trashSpawnerReference.transform.childCount == 1)
                {
                    // Check if the object we are about to destroy is indeed that last child
                    // This check might be slightly complex if objects can be parented differently
                    // A simpler check might be just childCount == 1, assuming only trash is parented there.
                    isLastTrash = true;
                }
            }

            playerStats.currentTrash++;
            Destroy(trashObject); // Destroy the object

            // --- Play Sound ---
            if (audioManager != null)
            {
                if (isLastTrash)
                {
                    audioManager.Play("clean", 1f); // Play special sound for last trash
                }
                else
                {
                    audioManager.Play("Trash Pickup", 1f); // Normal pickup sound
                }
            }
            // ------------------

            playedFailSoundThisAction = false; // Successful pickup resets fail flag
            CheckInteractionPossibility(); // Update possibility state (might become full)
            return true; // Pickup successful
        }
        else
        {
            // Don't play inventory full sound here, let AttemptPickupTrashInRadius handle it once
            // PlayInventoryFullSound();
            return false; // Pickup failed (likely inventory full)
        }
    }


    void OnDrawGizmosSelected()
    {
        // Use actual stats values if available, otherwise defaults
        float pickupRadius = (playerStats != null) ? playerStats.trashPickupRadius : 1.5f;
        float stationRadius = (playerStats != null) ? playerStats.stationInteractRadius : 1.0f;

        // Draw Trash Pickup Radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Draw Station Interaction Radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stationRadius);
    }
}