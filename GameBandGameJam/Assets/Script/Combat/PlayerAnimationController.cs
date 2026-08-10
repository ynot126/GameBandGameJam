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

    public void PlayAttack(ComboType comboType)
    {
        if (!animator)
        {
            return;
        }

        if (!PlayerAnimationClips.TryGetClip(comboType, out var stateName, out _))
        {
            return;
        }

        PlayState(stateName);
    }

    public void ResetAttack()
    {
        PlayIdle();
    }

    void PlayIdle()
    {
        PlayState(PlayerAnimationClips.Idle);
    }

    public float GetClipDuration(ComboType comboType)
    {
        if (!PlayerAnimationClips.TryGetClip(comboType, out _, out var duration))
        {
            return 0f;
        }

        return duration;
    }

    public void SetPlaybackSpeed(float speed)
    {
        if (!animator)
        {
            return;
        }

        animator.speed = Mathf.Max(0.01f, speed);
    }

    void PlayState(string stateName)
    {
        if (!animator)
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
