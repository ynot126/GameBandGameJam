#nullable enable
using UnityEngine;

public class PlayerAnimatorDriver : MonoBehaviour
{
    static readonly int AttackIdHash = Animator.StringToHash("Attack_ID");
    static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    [SerializeField] Animator? animator;

    public void Initialize(Animator? targetAnimator)
    {
        animator = targetAnimator;
        ResetAttack();
    }

    public void PlayAttack(AttackId attackId)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetInteger(AttackIdHash, (int)attackId);
    }

    public void ResetAttack()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetInteger(AttackIdHash, (int)AttackId.None);
    }

    public void SetMoving(bool isMoving)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, isMoving);
    }
}
