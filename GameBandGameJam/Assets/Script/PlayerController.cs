#nullable enable
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float movementSpeed;
    bool movementEnabled = true;
    Rigidbody? body;

    public void Initialize(int aMovementSpeed)
    {
        movementSpeed = aMovementSpeed;
        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
        }

        movementEnabled = true;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    void Update()
    {
        if (!movementEnabled)
        {
            return;
        }

        var horizontalInput = Input.GetAxisRaw("Horizontal");
        var verticalInput = Input.GetAxisRaw("Vertical");
        if (Mathf.Approximately(horizontalInput, 0f) && Mathf.Approximately(verticalInput, 0f))
        {
            return;
        }

        var movement = GetCameraPlanarDirection(horizontalInput, verticalInput);
        if (movement == Vector3.zero)
        {
            return;
        }

        transform.position += movement * (movementSpeed * Time.deltaTime);
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
