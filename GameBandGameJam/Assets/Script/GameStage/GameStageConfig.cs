using UnityEditor;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "GameStageConfig", 
   menuName = "Configs/GameStageConfig")]
public class GameStageConfig : ScriptableObject
{
   public EnumDictionary<GameStageType , SceneAsset> gameStages = new EnumDictionary<GameStageType , SceneAsset>();

   [Button]
   void OnValidate()
   {
      foreach (var gameStage in gameStages)
      {
         var sceneAsset = gameStage.Value;
         if (sceneAsset == null)
            continue;

         var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
         var isEnabledInBuild = false;

         foreach (var buildScene in EditorBuildSettings.scenes)
         {
            if (buildScene.path == scenePath && buildScene.enabled)
            {
               isEnabledInBuild = true;
               break;
            }
         }

         if (!isEnabledInBuild)
         {
            Debug.LogWarning(
               $"Scene '{sceneAsset.name}' assigned to {gameStage.Key} is not enabled in Build Settings.",
               this);
         }
      }
   }
}