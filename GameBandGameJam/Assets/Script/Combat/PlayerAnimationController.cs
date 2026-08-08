#nullable enable
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    const float CrossfadeSeconds = 0.05f;
    const int BaseLayer = 0;

    [SerializeField] Animator? animator;

    public void Initialize(Animator? targetAnimator)
    {
        animator = targetAnimator;
        PlayIdle();
    }

    public void PlayAttack(AttackId attackId)
    {
        if (animator == null)
        {
            return;
        }

        if (!PlayerAnimationClips.TryGetClip(attackId, out var stateName, out _))
        {
            return;
        }

        PlayState(stateName);
    }

    public void ResetAttack()
    {
        PlayIdle();
    }

    public void PlayIdle()
    {
        PlayState(PlayerAnimationClips.Idle);
    }

    public float GetClipDuration(AttackId attackId)
    {
        if (!PlayerAnimationClips.TryGetClip(attackId, out _, out var duration))
        {
            return 0f;
        }

        return duration;
    }

    void PlayState(string stateName)
    {
        if (animator == null)
        {
            return;
        }

        var stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(BaseLayer, stateHash))
        {
            Debug.LogWarning(
                $"Animator on '{animator.gameObject.name}' has no state '{stateName}' on layer {BaseLayer}.");
            return;
        }

        animator.CrossFadeInFixedTime(stateHash, CrossfadeSeconds, BaseLayer, 0f);
    }
}
