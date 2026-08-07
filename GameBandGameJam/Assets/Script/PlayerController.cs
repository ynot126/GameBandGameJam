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

        var movement = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        if (movement == Vector3.zero)
        {
            return;
        }

        transform.position += movement * (movementSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(movement, Vector3.up);
    }
}
