using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "GameStageConfig", 
   menuName = "Configs/GameStageConfig")]
public class GameStageConfig : ScriptableObject
{
   public EnumDictionary<GameStageType , SceneAsset> gameStages = new EnumDictionary<GameStageType , SceneAsset>();
}