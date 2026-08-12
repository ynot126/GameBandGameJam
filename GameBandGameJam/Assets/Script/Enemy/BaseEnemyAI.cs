#nullable enable
using UnityEngine;

public abstract class BaseEnemyAI : MonoBehaviour
{
    protected Transform PlayerTransform = null!;

    public virtual void Initialize(Transform playerTransform)
    {
        PlayerTransform = playerTransform;
    }

    public virtual void CancelPendingActions()
    {
    }

    public virtual void UpdateAIMovement()
    {
    }
}
