using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("How quickly the camera adjusts size/position (smaller = faster)")]
    public float sizeSmoothTime = 0.3f;
    public float positionSmoothTime = 0.25f;

    private float targetOrthoSize;
    private float targetYPosition;

    private Camera cam;
    private float orthoSizeVelocity = 0.0f;
    private float yPositionVelocity = 0.0f;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            targetOrthoSize = cam.orthographicSize;
            targetYPosition = transform.position.y;
        }
        else
        {
            enabled = false;
        }
    }

    public void SetTargetCameraState(float newOrthoSize, float newYPosition)
    {
        targetOrthoSize = newOrthoSize;
        targetYPosition = newYPosition;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        if (!Mathf.Approximately(cam.orthographicSize, targetOrthoSize))
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetOrthoSize, ref orthoSizeVelocity, sizeSmoothTime);
        }

        Vector3 currentPos = transform.position;
        if (!Mathf.Approximately(currentPos.y, targetYPosition))
        {
            float newY = Mathf.SmoothDamp(currentPos.y, targetYPosition, ref yPositionVelocity, positionSmoothTime);
            transform.position = new Vector3(currentPos.x, newY, currentPos.z);
        }
    }
}