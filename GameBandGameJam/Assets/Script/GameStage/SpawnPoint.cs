#nullable enable
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

// This class is purely for artist to easier to look at the scene
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] Enemy enemyPrefab = null!;
    
    public async UniTask<Enemy> Spawn(Transform playerTransform)
    {
        var enemy = Instantiate(enemyPrefab);
        enemy.Initialize(playerTransform);
        await enemy.SpawnAnimation();
        return enemy;
    }
    #if UNITY_EDITOR
    const string PreviewName = "EnemyPreview";
    void Awake()
    {
        var preview = transform.Find(PreviewName);
        if (!preview) return;
        Destroy(preview.gameObject);
    }
    [Button]
    private void SpawnPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        RemovePreview();

        var preview = (GameObject)PrefabUtility.InstantiatePrefab(
            enemyPrefab.gameObject,
            gameObject.scene);

        Undo.RegisterCreatedObjectUndo(preview, "Create enemy preview");
        Undo.SetTransformParent(
            preview.transform,
            transform,
            "Parent enemy preview");

        preview.name = PreviewName;
        preview.tag = "EditorOnly";

        // Match what Instantiate(prefab, position, rotation) does.
        preview.transform.SetPositionAndRotation(
            transform.position,
            transform.rotation);

        preview.transform.localScale = enemyPrefab.transform.localScale;

        Selection.activeGameObject = preview;
    }

    [Button]
    void RemovePreview()
    {
        var preview = transform.Find(PreviewName);
        if (preview != null)
            Undo.DestroyObjectImmediate(preview.gameObject);
    }
    #endif
}
