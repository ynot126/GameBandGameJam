#nullable enable
using System.Collections.Generic;
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
    [SerializeField] TwoHandChooseView twoHandChooseViewPrefab = null!;
    
    [Header("Enemy")]
    [SerializeField] List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    
    [Header("Stage Component")]
    [SerializeField] StageDoor stageDoor = null!;

    Player player = null!;
    List<Enemy> enemies =  new List<Enemy>();
    public void Start()
    {
        Initialize();
        StartAsync().Forget();
    }

    void Initialize()
    {
        player = Instantiate(playerPrefab, playerSpawnPoint.position, Quaternion.identity);
        player.Initialize(GameDataManager.Instance.GetPlayerData());
        
        stageDoor.Initialize();
        stageDoor.OnEnterDoor += HandleStageDoorEnter;
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

        // Spawn enemy
        enemies = new List<Enemy>();
        foreach (var spawn in spawnPoints)
        {
            SpawnEnemy(spawn).Forget();
        }
    }

    async UniTask SpawnEnemy(SpawnPoint spawnPoint)
    {
        var enemy = await spawnPoint.Spawn();
        enemies.Add(enemy);
    }

    void HandleStageDoorEnter()
    {
        var twoHandView = CreateTwoHandView();
        ViewManager.Instance.PushView(twoHandView);
    }

    #region TwoHandView

    TwoHandChooseView CreateTwoHandView()
    {
        var view = Instantiate(twoHandChooseViewPrefab);
        view.Initialize();
        view.OnSelect += HandleTwoHandleSelect;
        return view;
    }

    void HandleTwoHandleSelect()
    {
        Debug.Log("A hand is selected");
        ViewManager.Instance.PopView();
    }
    #endregion
}
