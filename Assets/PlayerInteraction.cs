using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E; // No longer used directly but kept for reference maybe
    public float requiredHoldTime = 1.0f;
    public Image holdProgressImage;

    public string trashTag = "Trash";
    public string sellingPointTag = "RecyclingBin";
    public string speedStationTag = "SpeedUpgradeStation";
    public string capacityStationTag = "CapacityUpgradeStation";
    public string radiusStationTag = "RadiusUpgradeStation";

    public bool pickupAllInRadius = true;

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
        if (holdProgressImage != null)
        {
            holdProgressImage.fillAmount = 0;
            holdProgressImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        previousInteractableObject = currentInteractableObject;
        FindInteractable();
        CheckInteractionPossibility();
        // HandleStationUIActivation(); // Assuming UI self-updates

        bool interactionKeyDown = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
        bool interactionKeyPressed = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space);
        bool interactionKeyUp = Input.GetKeyUp(KeyCode.E) || Input.GetKeyUp(KeyCode.Space);

        bool handledByHold = HandleHoldInteraction(interactionKeyPressed);

        if (!isHoldingInteract && interactionKeyDown)
        {
            AttemptPickupTrashInRadius();
        }

        if (interactionKeyUp) ResetHoldState();
        if (!isNearInteractable && isHoldingInteract) ResetHoldState();
        if (!currentInteractionIsPossible && isHoldingInteract) ResetHoldState();
    }

    void AttemptPickupTrashInRadius()
    {
        if (playerStats == null) return;
        if (playerStats.currentTrash >= playerStats.maxTrashCapacity)
        {
            PlayInventoryFullSound();
            return;
        }
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, playerStats.trashPickupRadius);
        int pickedUpCount = 0;
        foreach (Collider2D col in colliders)
        {
            if (playerStats.currentTrash >= playerStats.maxTrashCapacity)
            {
                if (col.CompareTag(trashTag)) AttemptPickupTrash(col.gameObject);
                break;
            }
            if (col.CompareTag(trashTag))
            {
                if (AttemptPickupTrash(col.gameObject)) pickedUpCount++;
            }
        }
    }

    void HandleStationUIActivation()
    {
        // Commented out as UI likely self-updates now
        /*
        if (currentInteractableObject != previousInteractableObject) {
           if (previousInteractableObject != null) {
               UpgradeStationUI prevStationUI = previousInteractableObject.GetComponent<UpgradeStationUI>();
               // if (prevStationUI != null) prevStationUI.HideText();
           }
           currentStationUIScript = null;
           if (currentInteractableObject != null) {
                currentStationUIScript = currentInteractableObject.GetComponent<UpgradeStationUI>();
                // if (currentStationUIScript != null) currentStationUIScript.ShowAndUpdateText(playerStats);
           }
       }
       else if (currentStationUIScript != null && isNearInteractable && currentInteractableObject == previousInteractableObject) {
            // currentStationUIScript.ShowAndUpdateText(playerStats);
       }
       */
    }

    bool HandleHoldInteraction(bool isKeyPressed)
    {
        UpgradeType stationTypeForSoundCheck = UpgradeType.Speed;
        bool isPotentiallyUpgrading = IsUpgradeStation(currentInteractableObject, out stationTypeForSoundCheck);

        if (isNearInteractable && currentInteractableObject != null)
        {
            bool isHoldableAction = IsStationOrSellingPoint(currentInteractableObject);
            if (isHoldableAction && isKeyPressed)
            {
                if (!currentInteractionIsPossible)
                {
                    PlayNoMoneySound();
                    return false;
                }
                if (!isHoldingInteract)
                {
                    isHoldingInteract = true;
                    playedFailSoundThisAction = false;
                    activeHoldSoundName = null;
                    if (holdProgressImage != null) holdProgressImage.gameObject.SetActive(true);
                    if (audioManager != null)
                    {
                        string soundToPlay = null;
                        if (isPotentiallyUpgrading)
                        {
                            if (stationTypeForSoundCheck == UpgradeType.Speed) soundToPlay = "Upgrade Hold 1";
                            else if (stationTypeForSoundCheck == UpgradeType.Capacity) soundToPlay = "Upgrade Hold 2";
                            else if (stationTypeForSoundCheck == UpgradeType.Radius) soundToPlay = "Upgrade Hold 3";
                        }
                        else if (currentInteractableObject.CompareTag(sellingPointTag))
                        {
                            if (playerStats != null && playerStats.currentTrash > 0)
                            {
                                soundToPlay = "Sell Trash Hold";
                            }
                        }
                        if (soundToPlay != null)
                        {
                            activeHoldSoundName = soundToPlay;
                            audioManager.Play(activeHoldSoundName, 1f);
                        }
                    }
                }
                currentHoldTime += Time.deltaTime;
                if (holdProgressImage != null) holdProgressImage.fillAmount = Mathf.Clamp01(currentHoldTime / requiredHoldTime);
                if (currentHoldTime >= requiredHoldTime)
                {
                    bool actionAttempted = false;
                    bool wasSuccess = false;
                    UpgradeType typePurchased = stationTypeForSoundCheck;
                    if (isPotentiallyUpgrading)
                    {
                        actionAttempted = true;
                        wasSuccess = upgradeManager.PurchaseUpgrade(typePurchased, playerStats);
                    }
                    else if (currentInteractableObject.CompareTag(sellingPointTag))
                    {
                        actionAttempted = true;
                        if (playerStats != null && playerStats.currentTrash > 0)
                        {
                            AttemptSellTrash();
                            wasSuccess = true;
                        }
                        else
                        {
                            wasSuccess = false;
                        }
                    }
                    if (actionAttempted && audioManager != null)
                    {
                        if (isPotentiallyUpgrading)
                        {
                            if (wasSuccess)
                            {
                                if (typePurchased == UpgradeType.Speed) audioManager.Play("Upgrade Result 1", 1f);
                                else if (typePurchased == UpgradeType.Capacity) audioManager.Play("Upgrade Result 2", 1f);
                                else if (typePurchased == UpgradeType.Radius) audioManager.Play("Upgrade Result 3", 1f);
                            }
                        }
                        else if (currentInteractableObject.CompareTag(sellingPointTag))
                        {
                            if (wasSuccess)
                            {
                                audioManager.Play("Sell Trash Result", 1f);
                            }
                        }
                    }
                    ResetHoldState();
                    CheckInteractionPossibility();
                    return true;
                }
                return true;
            }
            else { if (isHoldingInteract) ResetHoldState(); }
        }
        else { if (isHoldingInteract) ResetHoldState(); }
        return false;
    }

    void CheckInteractionPossibility()
    {
        bool previousPossibility = currentInteractionIsPossible;
        currentInteractionIsPossible = false;
        if (isNearInteractable && currentInteractableObject != null && playerStats != null && upgradeManager != null)
        {
            if (IsUpgradeStation(currentInteractableObject, out currentStationType))
            {
                currentInteractionIsPossible = upgradeManager.CanAffordUpgrade(currentStationType, playerStats.money);
            }
            else if (currentInteractableObject.CompareTag(sellingPointTag))
            {
                currentInteractionIsPossible = playerStats.currentTrash > 0;
            }
            else if (currentInteractableObject.CompareTag(trashTag))
            {
                currentInteractionIsPossible = playerStats.currentTrash < playerStats.maxTrashCapacity;
            }
        }
        if (!currentInteractionIsPossible && isHoldingInteract) ResetHoldState();
    }

    void ResetHoldState()
    {
        if (isHoldingInteract && audioManager != null && activeHoldSoundName != null)
        {
            audioManager.Stop(activeHoldSoundName);
        }
        currentHoldTime = 0f;
        isHoldingInteract = false;
        activeHoldSoundName = null;
        playedFailSoundThisAction = false;
        if (holdProgressImage != null)
        {
            holdProgressImage.fillAmount = 0;
            holdProgressImage.gameObject.SetActive(false);
        }
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
            Invoke(nameof(ResetFailSoundFlag), 0.15f);
        }
    }

    void PlayNoMoneySound()
    {
        if (audioManager != null && !playedFailSoundThisAction)
        {
            audioManager.Play("noMoney", 1f);
            playedFailSoundThisAction = true;
            Invoke(nameof(ResetFailSoundFlag), 0.2f);
        }
    }

    void FindInteractable()
    {
        currentInteractableObject = null;
        isNearInteractable = false;
        if (playerStats == null) return;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, playerStats.trashPickupRadius);
        GameObject closestValidObject = null;
        float minDistanceSqr = float.MaxValue;
        foreach (Collider2D col in colliders)
        {
            bool isValid = false;
            float distanceSqr = (col.transform.position - transform.position).sqrMagnitude;
            if (col.CompareTag(trashTag)) isValid = true;
            else if (IsStationOrSellingPoint(col.gameObject))
            {
                if (distanceSqr <= playerStats.stationInteractRadius * playerStats.stationInteractRadius) isValid = true;
            }
            if (isValid && distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestValidObject = col.gameObject;
            }
        }
        currentInteractableObject = closestValidObject;
        isNearInteractable = currentInteractableObject != null;
        if (!isNearInteractable)
        {
            playedFailSoundThisAction = false;
        }
    }

    bool IsStationOrSellingPoint(GameObject obj)
    {
        if (obj == null) return false;
        if (obj.CompareTag(speedStationTag) || obj.CompareTag(capacityStationTag) || obj.CompareTag(radiusStationTag) || obj.CompareTag(sellingPointTag))
        { return true; }
        return false;
    }

    bool IsUpgradeStation(GameObject obj, out UpgradeType type)
    {
        type = UpgradeType.Speed;
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
            int priceReceived = itemsSold * (itemsSold + 1) / 2;
            playerStats.money += priceReceived;
            playerStats.currentTrash = 0;
        }
    }

    bool AttemptPickupTrash(GameObject trashObject)
    {
        if (trashObject == null) return false;
        if (playerStats != null && playerStats.currentTrash < playerStats.maxTrashCapacity)
        {
            bool isLastTrash = false;
            if (upgradeManager != null && upgradeManager.trashSpawnerReference != null)
            {
                if (upgradeManager.trashSpawnerReference.transform.childCount == 1)
                {
                    if (trashObject.transform.parent == upgradeManager.trashSpawnerReference.transform)
                    {
                        isLastTrash = true;
                    }
                }
            }

            playerStats.currentTrash++;
            Destroy(trashObject);

            if (audioManager != null)
            {
                if (isLastTrash)
                {
                    audioManager.Play("clean", 1f);
                }
                else
                {
                    audioManager.Play("Trash Pickup", 1f);
                }
            }
            playedFailSoundThisAction = false;
            return true;
        }
        else
        {
            PlayInventoryFullSound();
            return false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, playerStats.trashPickupRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, playerStats.stationInteractRadius);
        }
        else
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            Gizmos.DrawWireSphere(transform.position, 1.0f);
        }
    }
}