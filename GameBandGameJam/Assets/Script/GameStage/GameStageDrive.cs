#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStageDrive : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] Player playerPrefab = null!;
    [SerializeField] Transform playerSpawnPoint = null!;
    
    [Header("Camera")]
    [SerializeField] GameCameraController gameCameraController = null!;

    Player player = null!;
    public void Start()
    {
        Initialize();
        StartAsync().Forget();
    }

    void Initialize()
    {
        player = Instantiate(playerPrefab, playerSpawnPoint.position, Quaternion.identity);
        player.Initialize(GameDataManager.Instance.GetPlayerData());
    }

    async UniTask StartAsync()
    {
        await UniTask.Yield();
        gameCameraController.StartTrackingPlayer(player.transform);
    }
}
