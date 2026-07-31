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
    
    [Header("View")]
    [SerializeField] GameView gameViewPrefab = null!;

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
        
        // GameView
        var gameView = Instantiate(gameViewPrefab);
        gameView.UpdateHealthText(player.CurrentHealth, player.MaxHealth);
        player.OnHealthChanged += () => gameView.UpdateHealthText(player.CurrentHealth, player.MaxHealth);
        ViewManager.Instance.PushView(gameView);
        
        // start Camera
        gameCameraController.StartTrackingPlayer(player.transform);
    }
}
