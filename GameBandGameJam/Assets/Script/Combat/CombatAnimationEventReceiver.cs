#nullable enable
using UnityEngine;

/// <summary>
/// Place on the same GameObject as the Animator. Animation Events on attack clips
/// call these methods by name (frame-accurate cancel / hitbox windows).
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

    // Animation Event — active hit frames.
    public void EnableHitbox()
    {
        hitbox?.EnableHitbox();
    }

    // Animation Event — end active hit frames.
    public void DisableHitbox()
    {
        hitbox?.DisableHitbox();
    }
}
