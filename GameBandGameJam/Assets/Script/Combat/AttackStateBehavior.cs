#nullable enable
using UnityEngine;

/// <summary>
/// Optional Mecanim fallback: opens the cancel window at a normalized clip time.
/// Prefer Animation Event <c>OpenCancelWindow</c> on the clip for a specific frame.
/// </summary>
public class AttackStateBehavior : StateMachineBehaviour
{
    [SerializeField, Range(0f, 1f)]
    float cancelWindowStartNormalized = 0.7f;

    PlayerCombat? playerCombat;
    bool cancelWindowOpened;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerCombat = animator.GetComponentInParent<PlayerCombat>();
        cancelWindowOpened = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (cancelWindowOpened || playerCombat == null)
        {
            return;
        }

        if (stateInfo.normalizedTime < cancelWindowStartNormalized)
        {
            return;
        }

        cancelWindowOpened = true;
        playerCombat.OpenCancelWindow();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        cancelWindowOpened = false;
        playerCombat = null;
    }
}
