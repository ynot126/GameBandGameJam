#nullable enable
using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    static readonly Plane FloorPlane = new(Vector3.up, 0f);

    float movementSpeed;

    public event Action<Vector3>? OnFloorClicked;

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

        if (Input.GetMouseButtonDown(0) && TryGetFloorClickPosition(out var clickPosition))
        {
            OnFloorClicked?.Invoke(clickPosition);
        }
    }

    bool TryGetFloorClickPosition(out Vector3 clickPosition)
    {
        clickPosition = default;

        var cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!FloorPlane.Raycast(ray, out var enter))
        {
            return false;
        }

        clickPosition = ray.GetPoint(enter);
        return true;
    }
}
