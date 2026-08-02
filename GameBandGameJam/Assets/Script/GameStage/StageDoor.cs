#nullable enable
using System;
using UnityEngine;

public class StageDoor : MonoBehaviour
{
    [SerializeField] CollisionDetector collisionDetector = null!;

    public event Action? OnEnterDoor;

    bool isPlayerInArea;

    public void Initialize()
    {
        collisionDetector.OnTriggerEnterAction += HandleTriggerEnter;
        collisionDetector.OnTriggeExitAction += HandleTriggerExit;
    }

    void Update()
    {
        if (!isPlayerInArea)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            OnEnterDoor?.Invoke();
        }
    }

    void HandleTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Player>() != null)
        {
            isPlayerInArea = true;
        }
    }

    void HandleTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<Player>() != null)
        {
            isPlayerInArea = false;
        }
    }
}
