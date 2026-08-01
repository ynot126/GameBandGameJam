#nullable enable

public class GameDataManager : Singleton<GameDataManager>
{
    PlayerData? playerData;

    public PlayerData GetPlayerData()
    {
        return playerData ??= new PlayerData();
    }
}
