#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class ChaseTeleport
{
    Rigidbody body = null!;
    Collider[] playerColliders = Array.Empty<Collider>();
    float offsetDistance = 1.5f;
    float chaseDuration = 0.35f;
    float groundedY;

    public void Initialize(
        Rigidbody playerBody,
        Collider[] colliders,
        float chaseOffset,
        float duration = 0.35f,
        float height = 1.75f)
    {
        body = playerBody;
        playerColliders = colliders;
        offsetDistance = chaseOffset;
        chaseDuration = duration;
        groundedY = playerBody.position.y;
        // height kept for call-site compatibility; chase stays planar so hitboxes remain valid.
        _ = height;
    }

    public void SnapToGround()
    {
        if (body == null)
        {
            return;
        }

        var position = body.position;
        if (Mathf.Approximately(position.y, groundedY))
        {
            return;
        }

        position.y = groundedY;
        body.MovePosition(position);
    }

    public async UniTask ChaseBehindAsync(Transform target, CancellationToken cancellationToken)
    {
        var forward = body.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        var destination = target.position - forward * offsetDistance;
        destination.y = groundedY;

        var lookDirection = target.position - destination;
        lookDirection.y = 0f;
        var targetRotation = lookDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : body.rotation;

        SetCollidersEnabled(false);
        try
        {
            SnapToGround();
            await KinematicMover.MoveToAsync(
                body,
                destination,
                chaseDuration,
                cancellationToken);

            body.MoveRotation(targetRotation);
        }
        finally
        {
            SnapToGround();
            SetCollidersEnabled(true);
        }
    }

    void SetCollidersEnabled(bool enabled)
    {
        for (var i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null)
            {
                playerColliders[i].enabled = enabled;
            }
        }
    }
}
