#nullable enable
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerCombatConfig))]
public class PlayerCombatConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Apply Default Combat Data"))
        {
            var config = (PlayerCombatConfig)target;
            PlayerCombatConfigDefaults.ApplyTo(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("Combat/Create Player Combat Config Asset")]
    static void CreateConfigAsset()
    {
        const string path = "Assets/Config/PlayerCombatConfig.asset";
        var existing = AssetDatabase.LoadAssetAtPath<PlayerCombatConfig>(path);
        if (existing != null)
        {
            PlayerCombatConfigDefaults.ApplyTo(existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            Selection.activeObject = existing;
            Debug.Log($"Updated existing PlayerCombatConfig at {path}");
            return;
        }

        var config = CreateInstance<PlayerCombatConfig>();
        PlayerCombatConfigDefaults.ApplyTo(config);
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = config;
        Debug.Log($"Created PlayerCombatConfig at {path}");
    }
}
