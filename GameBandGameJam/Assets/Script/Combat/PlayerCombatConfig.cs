#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCombatConfig", menuName = "Configs/PlayerCombatConfig")]
public class PlayerCombatConfig : ScriptableObject
{
    public float defaultComboResetWindow = 0.5f;
    public float simultaneousInputWindow = 0.05f;
    public int consecutiveInvalidThreshold = 2;
    public ComboRecipe[] recipes = System.Array.Empty<ComboRecipe>();
    public AttackDefinition[] attacks = System.Array.Empty<AttackDefinition>();

#if UNITY_EDITOR
    void OnValidate()
    {
        if ((recipes == null || recipes.Length == 0) && (attacks == null || attacks.Length == 0))
        {
            PlayerCombatConfigDefaults.ApplyTo(this);
        }
    }

    [ContextMenu("Apply Default Combat Data")]
    void ApplyDefaultsInEditor()
    {
        PlayerCombatConfigDefaults.ApplyTo(this);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
