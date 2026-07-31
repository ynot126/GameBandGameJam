#nullable enable
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    [Header("Tracking")]
    [SerializeField] Vector3 positionOffset = new(0f, 8f, -10f);
    [SerializeField, Range(0f, 1f)] float positionLerpPercentage = 0.15f;
    [SerializeField, Min(0f)] float snapDistance = 25f;

    [Header("Rotation")]
    [SerializeField] Vector3 rotationEulerAngles = new(30f, 0f, 0f);
    [SerializeField, Range(0f, 1f)] float rotationLerpPercentage = 0.15f;

    Transform? trackedPlayerTransform;
    bool trackingPlayer;

    void Update()
    {
        if (!trackingPlayer || !trackedPlayerTransform)
        {
            return;
        }

        UpdatePlayerTracking(trackedPlayerTransform.transform.position);
    }

    public void StartTrackingPlayer(Transform playerTransform)
    {
        trackedPlayerTransform = playerTransform;
        trackingPlayer = true;
    }

    void UpdatePlayerTracking(Vector3 playerPosition)
    {
        var targetPosition = playerPosition + positionOffset;

        if (snapDistance > 0f &&
            Vector3.Distance(transform.position, targetPosition) >= snapDistance)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                positionLerpPercentage);
        }

        var targetRotation = Quaternion.Euler(rotationEulerAngles);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationLerpPercentage);
    }
}
