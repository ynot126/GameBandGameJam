#nullable enable
using UnityEngine;

public sealed class ChaseTeleport
{
    Transform player = null!;
    Collider[] playerColliders = null!;
    float offsetDistance = 1.5f;

    public void Initialize(Transform playerTransform, Collider[] colliders, float chaseOffset)
    {
        player = playerTransform;
        playerColliders = colliders;
        offsetDistance = chaseOffset;
    }

    public void TeleportBehind(Transform target)
    {
        SetCollidersEnabled(false);

        var forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        var destination = target.position - forward * offsetDistance;
        destination.y = player.position.y;
        player.position = destination;

        var lookDirection = target.position - player.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            player.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        SetCollidersEnabled(true);
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
