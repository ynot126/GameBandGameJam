#nullable enable
using System;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public event Action<Collider>? OnTriggerEnterAction;
    void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterAction?.Invoke(other);
    }
}