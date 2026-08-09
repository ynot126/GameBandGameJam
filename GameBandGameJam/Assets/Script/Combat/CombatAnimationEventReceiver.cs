#nullable enable
using UnityEngine;

/// <summary>
/// Place on the same GameObject as the Animator. Animation Events on attack clips
/// call these methods by name (frame-accurate cancel / hitbox windows / camera effects).
/// </summary>
public class CombatAnimationEventReceiver : MonoBehaviour
{
    [SerializeField] PlayerCombat? playerCombat;
    [SerializeField] CombatHitbox? hitbox;

    public void Initialize(PlayerCombat combat, CombatHitbox combatHitbox)
    {
        playerCombat = combat;
        hitbox = combatHitbox;
    }

    // Animation Event — scrub to the cancel frame and add this function.
    public void OpenCancelWindow()
    {
        playerCombat?.OpenCancelWindow();
    }

    // Animation Event — active hit frames without camera shake on impact.
    public void EnableHitbox()
    {
        hitbox?.EnableHitbox();
        playerCombat?.PlayImpactFrameParticle();
    }

    // Animation Event — active hit frames; shake only if a hit is confirmed during this window.
    public void EnableHitboxWithShake()
    {
        hitbox?.EnableHitbox();
        playerCombat?.ArmCameraShakeOnHit();
        playerCombat?.PlayImpactFrameParticle();
    }

    // Animation Event — end active hit frames.
    public void DisableHitbox()
    {
        hitbox?.DisableHitbox();
        playerCombat?.ClearCameraShakeOnHit();
    }

    // Animation Event — float parameter is zoom depth in world units.
    public void BeginCameraZoom(float zoomDepth)
    {
        GameCameraController.Instance.BeginZoom(zoomDepth);
    }

    // Animation Event — restore framing after BeginCameraZoom.
    public void EndCameraZoom()
    {
        playerCombat?.EndCombatCameraZoom();
    }
}
