#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCombatConfig", menuName = "Configs/PlayerCombatConfig")]
public class PlayerCombatConfig : ScriptableObject
{
    public float defaultComboResetWindow = 0.5f;
    public float simultaneousInputWindow = 0.05f;
    public int consecutiveInvalidThreshold = 2;

    [Header("Boundary Ring Out")]
    [Min(0.1f)] public float boundaryKillProximity = 3f;
    [Min(1f)] public float boundaryKillLaunchDistance = 40f;

    public EnumDictionary<ComboType, ComboData> combos = new();

    public bool HasAuthoredCombos()
    {
        foreach (var pair in combos)
        {
            if (pair.Key == ComboType.None)
            {
                continue;
            }

            if (pair.Value?.sequence is { Length: > 0 })
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!HasAuthoredCombos())
        {
            PlayerCombatConfigDefaults.ApplyTo(this);
        }
    }
    
#endif
}
