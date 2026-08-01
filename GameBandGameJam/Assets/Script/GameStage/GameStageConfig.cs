#nullable enable
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GameStageConfig",
   menuName = "Configs/GameStageConfig")]
public class GameStageConfig : ScriptableObject
{
   public EnumDictionary<GameStageType, string> gameStages = new EnumDictionary<GameStageType, string>();

   [Button]
   void OnValidate()
   {
#if UNITY_EDITOR
      foreach (var gameStage in gameStages)
      {
         var sceneName = gameStage.Value;
         if (string.IsNullOrEmpty(sceneName))
            continue;

         var isEnabledInBuild = false;

         foreach (var buildScene in EditorBuildSettings.scenes)
         {
            if (!buildScene.enabled)
               continue;

            var buildSceneName = System.IO.Path.GetFileNameWithoutExtension(buildScene.path);
            if (buildSceneName == sceneName)
            {
               isEnabledInBuild = true;
               break;
            }
         }

         if (!isEnabledInBuild)
         {
            Debug.LogWarning(
               $"Scene '{sceneName}' assigned to {gameStage.Key} is not enabled in Build Settings.",
               this);
         }
      }
#endif
   }
}
