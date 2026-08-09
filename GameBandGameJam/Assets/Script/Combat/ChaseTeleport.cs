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
    float arcHeight = 1.75f;

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
        arcHeight = height;
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
        destination.y = body.position.y;

        var lookDirection = target.position - destination;
        lookDirection.y = 0f;
        var targetRotation = lookDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : body.rotation;

        SetCollidersEnabled(false);
        try
        {
            await KinematicMover.MoveAlongArcAsync(
                body,
                destination,
                chaseDuration,
                arcHeight,
                cancellationToken);

            body.MoveRotation(targetRotation);
        }
        finally
        {
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
