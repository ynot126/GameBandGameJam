#nullable enable
using System;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public event Action<Collider>? OnTriggerEnterAction;
    public event Action<Collider>? OnTriggeExitAction;
    void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterAction?.Invoke(other);
    }

    void OnTriggerExit(Collider other)
    {
        OnTriggeExitAction?.Invoke(other);
    }
}