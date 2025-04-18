using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Collider2D agentCollider;
    private float previousDistance = float.MaxValue;
    private AudioManager audioManager;
    private UpgradeManager upgradeManager;

    [Header("Reward Settings")]
    public float goalReward = 10f;
    public float boundaryPenalty = -1.0f;
    // Removed fixed stepPenalty
    [Tooltip("Initial step penalty magnitude at step 0 (positive value)")]
    public float baseStepPenalty = 0.001f; // Base penalty magnitude
    [Tooltip("How much penalty increases each step (e.g., 0.00001)")]
    public float penaltyIncreaseFactor = 0.00001f; // Increase per step
    public float distanceRewardScaleFactor = 0.1f;
    public float distanceRewardEpsilon = 0.1f;
    public float timeoutPenalty = -1.0f;

    [Header("Debug Colors")]
    public Color winColor = Color.green;
    public Color loseColor = Color.red;
    public string trashTag = "Trash";

    [Header("Debugging UI")]
    public TextMeshProUGUI rewardDebugText;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        agentCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        audioManager = FindFirstObjectByType<AudioManager>();
        upgradeManager = FindFirstObjectByType<UpgradeManager>();

        if (rb == null || agentCollider == null || mainCamera == null || audioManager == null || upgradeManager == null)
        {
            enabled = false;
            return;
        }
        rb.gravityScale = 0;
        UpdateRewardUI();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = Vector3.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        targetTransform = null;
        previousDistance = float.MaxValue;

        GameObject[] trashObjects = GameObject.FindGameObjectsWithTag(trashTag);
        GameObject closestTrash = null;
        if (trashObjects.Length > 0)
        {
            closestTrash = trashObjects.OrderBy(t => Vector3.Distance(transform.localPosition, t.transform.localPosition)).FirstOrDefault();
        }
        if (closestTrash != null)
        {
            targetTransform = closestTrash.transform;
            previousDistance = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
        }
        UpdateRewardUI();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (rb != null)
        {
            sensor.AddObservation(rb.linearVelocity.x);
            sensor.AddObservation(rb.linearVelocity.y);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
        if (targetTransform != null)
        {
            sensor.AddObservation(targetTransform.localPosition - transform.localPosition);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        float moveSpeed = 3f;
        if (rb != null)
        {
            Vector2 calculatedVelocity = new Vector2(moveX, moveY).normalized * moveSpeed;
            rb.linearVelocity = calculatedVelocity;
        }
    }

    void FixedUpdate()
    {
        // Calculate and apply increasing step penalty
        if (MaxStep > 0)
        { // Only apply if MaxStep is set
            float currentPenaltyMagnitude = baseStepPenalty + (penaltyIncreaseFactor * StepCount);
            AddReward(-currentPenaltyMagnitude); // Apply as negative reward
        }

        // Apply reward based on inverse distance to target
        if (targetTransform != null)
        {
            float distanceNow = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
            float distanceReward = distanceRewardScaleFactor * (1.0f / (distanceNow + distanceRewardEpsilon));
            AddReward(distanceReward);
            // Removed previousDistance check for reward shaping
        }
        else
        {
            GameObject[] trashObjects = GameObject.FindGameObjectsWithTag(trashTag);
            GameObject closestTrash = null;
            if (trashObjects.Length > 0)
            {
                closestTrash = trashObjects.OrderBy(t => Vector3.Distance(transform.localPosition, t.transform.localPosition)).FirstOrDefault();
            }
            if (closestTrash != null)
            {
                targetTransform = closestTrash.transform;
                previousDistance = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
            }
        }

        CheckForTimeout();
        CheckBoundaries();
        UpdateRewardUI();
    }

    void UpdateRewardUI()
    {
        if (rewardDebugText != null)
        {
            rewardDebugText.text = $"Reward: {GetCumulativeReward():F3}";
        }
    }

    void CheckForTimeout()
    {
        if (MaxStep > 0 && StepCount >= MaxStep - 1)
        {
            SetReward(timeoutPenalty);
            audioManager.Play("noMoney", 1);
            UpdateRewardUI();
            if (mainCamera != null) mainCamera.backgroundColor = loseColor;
            EndEpisode();
        }
    }

    private void CheckBoundaries()
    {
        if (!this.gameObject.activeInHierarchy || !this.enabled) return;
        if (mainCamera == null || agentCollider == null) return;
        Vector2 agentExtents = agentCollider.bounds.extents;
        Vector3 currentPos = transform.position;
        float camNearClipPlane = mainCamera.nearClipPlane;
        Vector3 viewBottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, camNearClipPlane));
        Vector3 viewTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, camNearClipPlane));
        bool isOutOfBounds =
            currentPos.x < viewBottomLeft.x + agentExtents.x ||
            currentPos.x > viewTopRight.x - agentExtents.x ||
            currentPos.y < viewBottomLeft.y + agentExtents.y ||
            currentPos.y > viewTopRight.y - agentExtents.y;

        if (isOutOfBounds)
        {
            SetReward(boundaryPenalty);
            audioManager.Play("noMoney", 1);
            UpdateRewardUI();
            if (mainCamera != null) mainCamera.backgroundColor = loseColor;
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (targetTransform != null && collision.transform == targetTransform)
        {
            Destroy(targetTransform.gameObject);
            SetReward(goalReward);
            audioManager.Play("clean", 1);
            UpdateRewardUI();
            if (mainCamera != null) mainCamera.backgroundColor = winColor;

            EndEpisode();
        }
    }
}