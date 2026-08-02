#nullable enable
using System;
using UnityEngine;

public class StageDoor : MonoBehaviour
{
    [SerializeField] CollisionDetector collisionDetector = null!;
    [SerializeField] NextStageVisual nextStageVisualPrefab = null!;
    public event Action? OnEnterDoor;

    bool isPlayerInArea;
    NextStageVisual? activeNextStageVisual;

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
        if (other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        isPlayerInArea = true;
        ShowNextStageVisual();
    }

    void HandleTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        isPlayerInArea = false;
        HideNextStageVisual();
    }

    void ShowNextStageVisual()
    {
        if (activeNextStageVisual != null)
        {
            return;
        }

        activeNextStageVisual = Instantiate(nextStageVisualPrefab);
        activeNextStageVisual.Initialize(transform.position);
    }

    void HideNextStageVisual()
    {
        if (activeNextStageVisual == null)
        {
            return;
        }

        Destroy(activeNextStageVisual.gameObject);
        activeNextStageVisual = null;
    }
}
