#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStageDrive : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] PlayerController playerControllerPrefab = null!;
    [SerializeField] Transform playerSpawnPoint = null!;
    
    [Header("Camera")]
    [SerializeField] GameCameraController gameCameraController = null!;

    PlayerController playerController = null!;
    public void Start()
    {
        Initialize();
        StartAsync().Forget();
    }

    void Initialize()
    {
        playerController = Instantiate(playerControllerPrefab, playerSpawnPoint.position, Quaternion.identity);
        playerController.Initialize();
    }

    async UniTask StartAsync()
    {
        await UniTask.Yield();
        gameCameraController.StartTrackingPlayer(playerController);
    }
}
