#nullable enable
using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float movementSpeed;
    bool movementEnabled = true;
    bool invertHorizontal;
    bool invertVertical;
    bool isLocomoting;
    Rigidbody body = null!;

    public event Action? OnLocomotionStarted;
    public event Action? OnLocomotionStopped;

    public void Initialize(float aMovementSpeed, Rigidbody aBody)
    {
        movementSpeed = aMovementSpeed;
        body = aBody;
        movementEnabled = true;
        invertHorizontal = false;
        invertVertical = false;
        isLocomoting = false;
    }

    public void SetMovementSpeed(float speed)
    {
        movementSpeed = Mathf.Max(0f, speed);
    }

    public void SetMovementEnabled(bool val)
    {
        movementEnabled = val;
        if (!val)
        {
            // Leave attack animations alone; do not emit stop / play idle here.
            isLocomoting = false;
            return;
        }

        SyncLocomotionState(TryResolveMoveDirection(out _));
    }

    public void InvertMovementAxes()
    {
        invertHorizontal = !invertHorizontal;
        invertVertical = !invertVertical;
    }

    public bool TryGetMoveInputDirection(out Vector3 planarDirection)
    {
        planarDirection = default;
        if (!TryGetRawMoveAxes(out var horizontalInput, out var verticalInput))
        {
            return false;
        }

        planarDirection = GetCameraPlanarDirection(horizontalInput, verticalInput);
        return planarDirection.sqrMagnitude > 0.001f;
    }

    bool TryGetRawMoveAxes(out float horizontalInput, out float verticalInput)
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        if (invertHorizontal)
        {
            horizontalInput = -horizontalInput;
        }

        if (invertVertical)
        {
            verticalInput = -verticalInput;
        }

        return !Mathf.Approximately(horizontalInput, 0f) || !Mathf.Approximately(verticalInput, 0f);
    }

    bool TryResolveMoveDirection(out Vector3 movement)
    {
        movement = Vector3.zero;
        if (!TryGetRawMoveAxes(out var horizontalInput, out var verticalInput))
        {
            return false;
        }

        movement = GetCameraPlanarDirection(horizontalInput, verticalInput);
        return movement != Vector3.zero;
    }

    void Update()
    {
        if (!movementEnabled)
        {
            return;
        }

        var hasMove = TryResolveMoveDirection(out var movement);
        SyncLocomotionState(hasMove);

        if (!hasMove)
        {
            return;
        }

        body.MovePosition(transform.position + movement * (movementSpeed * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(movement, Vector3.up);
    }

    void SyncLocomotionState(bool hasMove)
    {
        if (hasMove)
        {
            if (isLocomoting)
            {
                return;
            }

            isLocomoting = true;
            OnLocomotionStarted?.Invoke();
            return;
        }

        if (!isLocomoting)
        {
            return;
        }

        isLocomoting = false;
        OnLocomotionStopped?.Invoke();
    }

    static Vector3 GetCameraPlanarDirection(float horizontal, float vertical)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            return Flatten(new Vector3(horizontal, 0f, vertical));
        }

        var forward = Flatten(cam.transform.forward);
        var right = Flatten(cam.transform.right);
        return Flatten(forward * vertical + right * horizontal);
    }

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }
}
