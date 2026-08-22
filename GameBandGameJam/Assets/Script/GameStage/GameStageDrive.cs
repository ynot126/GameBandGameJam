#nullable enable
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStageDrive : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] Player playerPrefab = null!;
    [SerializeField] Transform playerSpawnPoint = null!;
    
    [Header("View")]
    [SerializeField] GameView gameViewPrefab = null!;
    [SerializeField] TwoHandChooseView twoHandChooseViewPrefab = null!;
    [SerializeField] LoseView loseViewPrefab = null!;
    
    [Header("Enemy")]
    [SerializeField] List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Skill")]
    [SerializeField] SkillConfig skillConfig = null!;

    [Header("Config")] 
    [SerializeField] GameStageConfig gameStageConfig = null!;

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
        player.OnDeath += () =>
        {
            var loseView = CreateLoseView();
            ViewManager.Instance.PushView(loseView);
        };

        ApplySkill();
    }

    async UniTask StartAsync()
    {
        await UniTask.Yield();
        
        // GameView
        var gameView = Instantiate(gameViewPrefab);
        gameView.UpdateHealthText(player.CurrentHealth, player.MaxHealth);
        player.OnHealthChanged += () => gameView.UpdateHealthText(player.CurrentHealth, player.MaxHealth);
        gameView.UpdateStaminaText(player.Stamina.CurrentStamina, player.Stamina.MaxStamina);
        player.Stamina.OnStaminaChanged += () =>
            gameView.UpdateStaminaText(player.Stamina.CurrentStamina, player.Stamina.MaxStamina);
        ViewManager.Instance.PushView(gameView);
        
        // start Camera
        GameCameraController.Instance.StartTrackingPlayer(player.transform);

        // Spawn enemy
        enemies = new List<Enemy>();
        foreach (var spawn in spawnPoints)
        {
            SpawnEnemy(spawn).Forget();
        }
    }

    async UniTask SpawnEnemy(SpawnPoint spawnPoint)
    {
        var enemy = await spawnPoint.Spawn(player.transform);
        enemies.Add(enemy);
        enemy.OnDeath += ()=>HandleEnemyDeath(enemy);
    }

    void HandleEnemyDeath(Enemy enemy)
    {
        enemies.Remove(enemy);
        if (enemies.Count != 0)
        {
            return;
        }

        var twoHandView = CreateTwoHandView();
        ViewManager.Instance.PushView(twoHandView);
    }

    void ApplySkill()
    {
        foreach (var skillType in GameDataManager.Instance.GetPlayerData().skills)
        {
            var skillPrefab = skillConfig.GetSkill(skillType);
            if (skillPrefab == null)
            {
                continue;
            }

            var skillInstance = Instantiate(skillPrefab);
            skillInstance.Initialize(player);
            skillInstance.ApplySkill();
        }
    }
    #region TwoHandView

    TwoHandChooseView CreateTwoHandView()
    {
        var view = Instantiate(twoHandChooseViewPrefab);
        var playerData = GameDataManager.Instance.GetPlayerData();
        view.Initialize(skillConfig, playerData.skills);
        view.OnSelect += HandleTwoHandleSelect;
        return view;
    }

    void HandleTwoHandleSelect(BaseSkill skill)
    {
        GameDataManager.Instance.GetPlayerData().skills.Add(skill.Type);
        LoadStage(GameStageType.GameScene);
    }
    #endregion

    #region Lose View

    LoseView CreateLoseView()
    {
        var view = Instantiate(loseViewPrefab);
        view.Initialize();
        view.OnRestartButtonPressed += HandleLoseViewRestart;
        return view;
    }

    void HandleLoseViewRestart()
    {
        LoadStage(GameStageType.MainScene);
    }

    #endregion
    void  LoadStage(GameStageType gameStageType)
    {
        GameDataManager.Instance.GetPlayerData().currentHealth = player.CurrentHealth;
        var sceneName = gameStageConfig.gameStages[gameStageType];
        ViewManager.Instance.ClearStack();
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
}
