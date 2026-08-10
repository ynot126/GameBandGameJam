#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Owns aim and facing math used during attacks.
/// </summary>
public sealed class CombatFacing
{
    Transform owner = null!;
    PlayerController playerController = null!;
    float lockRotationSpeed = 720f;

    public void Initialize(Transform ownerTransform, PlayerController controller, float rotationSpeed)
    {
        owner = ownerTransform;
        playerController = controller;
        lockRotationSpeed = rotationSpeed;
    }

    public void FaceAimDirection()
    {
        if (TryGetHeldMoveInput(out var held))
        {
            FaceDirection(held);
            return;
        }

        if (TryGetFloorAim(out var aimPoint))
        {
            var dir = aimPoint - owner.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                FaceDirection(dir);
            }
        }
    }

    public void FaceDirection(Vector3 worldDirection)
    {
        var flat = Flatten(worldDirection);
        if (flat.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        owner.rotation = Quaternion.LookRotation(flat, Vector3.up);
    }

    public Vector3 ResolveMoveInputDirection()
    {
        if (TryGetHeldMoveInput(out var move) && move.sqrMagnitude > 0.001f)
        {
            return move;
        }

        return Flatten(owner.forward);
    }

    public async UniTask AlignToLockDuringStartupAsync(
        CombatAutoLock autoLock,
        float duration,
        CancellationToken cancellationToken)
    {
        if (duration <= 0f)
        {
            if (autoLock.TryGetLockDirection(out var instantDir))
            {
                FaceDirection(instantDir);
            }

            return;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!autoLock.TryGetLockDirection(out var lockDirection))
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(lockDirection, Vector3.up);
            owner.rotation = Quaternion.RotateTowards(
                owner.rotation,
                targetRotation,
                lockRotationSpeed * Time.deltaTime);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    public static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    bool TryGetHeldMoveInput(out Vector3 planarDirection)
    {
        return playerController.TryGetMoveInputDirection(out planarDirection);
    }

    static bool TryGetFloorAim(out Vector3 point)
    {
        point = default;
        var cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var floor = new Plane(Vector3.up, 0f);
        if (!floor.Raycast(ray, out var enter))
        {
            return false;
        }

        point = ray.GetPoint(enter);
        return true;
    }
}
