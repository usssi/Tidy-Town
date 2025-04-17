using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    private float previousDistance;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Collider2D agentCollider;

    [Space]
    public Color winColor;
    public Color loseColor;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        agentCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        if (rb != null) rb.gravityScale = 0;
        if (mainCamera == null || agentCollider == null)
        {
            Debug.LogError("Agent needs a Collider2D and a Main Camera must exist!", gameObject);
            enabled = false;
        }
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = Vector3.zero;
        GameObject targetObj = GameObject.FindGameObjectWithTag("Trash");
        if (targetObj != null)
        {
            targetTransform = targetObj.transform;
        }
        else
        {
            targetTransform = null;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        if (targetTransform != null)
        {
            sensor.AddObservation(targetTransform.localPosition);
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
        AddReward(-0.003f);

        // En FixedUpdate, necesitarías guardar la distancia anterior
        float distanceNow = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
        float rewardForDistance = previousDistance - distanceNow; // Positivo si te acercas
        AddReward(rewardForDistance * 0.01f); // Factor pequeño
        previousDistance = distanceNow;

        CheckBoundaries();
    }

    private void CheckBoundaries()
    {
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
            SetReward(-1.0f);
            mainCamera.backgroundColor = loseColor;
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
        if (collision.CompareTag("Trash"))
        {
            SetReward(1f);
            mainCamera.backgroundColor = winColor;
            EndEpisode();
        }
    }
}