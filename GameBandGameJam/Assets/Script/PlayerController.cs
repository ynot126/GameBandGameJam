#nullable enable
using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float movementSpeed;
    public void Initialize(int aMovementSpeed)
    {
        movementSpeed = aMovementSpeed;
    }

    void Update()
    {
        var horizontalInput = Input.GetAxisRaw("Horizontal");
        var verticalInput = Input.GetAxisRaw("Vertical");

        var movement = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        if (movement != Vector3.zero)
        {
            transform.position += movement * (movementSpeed * Time.deltaTime);
        }
    }
}
