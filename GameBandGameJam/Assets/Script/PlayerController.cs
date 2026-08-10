#nullable enable
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float movementSpeed;
    bool movementEnabled = true;
    bool invertHorizontal;
    bool invertVertical;
    Rigidbody body = null!;

    public void Initialize(int aMovementSpeed, Rigidbody aBody)
    {
        movementSpeed = aMovementSpeed;
        body = aBody;
        movementEnabled = true;
        invertHorizontal = false;
        invertVertical = false;
    }

    public void SetMovementEnabled(bool val)
    {
        movementEnabled = val;
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

    void Update()
    {
        if (!movementEnabled)
        {
            return;
        }

        if (!TryGetRawMoveAxes(out var horizontalInput, out var verticalInput))
        {
            return;
        }

        var movement = GetCameraPlanarDirection(horizontalInput, verticalInput);
        if (movement == Vector3.zero)
        {
            return;
        }
        
        body.MovePosition(transform.position + movement * (movementSpeed * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(movement, Vector3.up);
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
