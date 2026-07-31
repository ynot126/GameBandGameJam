#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainDrive : MonoBehaviour
{
    [Header("View")]
    [SerializeField] TitleView titleViewPrefab = null!;
    [SerializeField] ShortCutView shortCutViewPrefab = null!;
    
    [Header("Config")]
    [SerializeField]GameStageConfig gameStageConfig = null!;
    
    void Start()
    {
        Initialize();
        StartAsync().Forget();
    }

    void Initialize()
    {
        
    }

    async UniTask StartAsync()
    {
        var titleView =CreateTitleView();
        ViewManager.Instance.PushView(titleView);
        await UniTask.Yield();
    }

    #region TitileView

    TitleView CreateTitleView()
    {
        var titleView = Instantiate(titleViewPrefab);
        titleView.Initialize();
        titleView.OnStartButtonPressed += HandleTitleViewStartButton;
        titleView.OnShortCutButtonPressed += HandleTitleViewShortCutButton;
        return titleView;
    }

    void HandleTitleViewStartButton()
    {
        Debug.Log("Start button pressed");
    }

    void HandleTitleViewShortCutButton()
    {
        var shortCutView = CreateShortCutView();
        ViewManager.Instance.PushView(shortCutView);
    }
    #endregion

    #region ShortCutView

    ShortCutView CreateShortCutView()
    {
        var shortCutView = Instantiate(shortCutViewPrefab);
        shortCutView.Initialize();
        shortCutView.OnBackButtonPressed += HandleShortCutViewBackButton;
        shortCutView.OnShortCutButtonPressed += HandleShortCutViewStageSelectButton;
        return shortCutView;
    }

    void HandleShortCutViewBackButton()
    {
        ViewManager.Instance.PopView();
    }

    void HandleShortCutViewStageSelectButton(GameStageType gameStageType)
    {
        LoadStage(gameStageType);
    }
    #endregion

    void  LoadStage(GameStageType gameStageType)
    {
        var sceneAsset = gameStageConfig.gameStages[gameStageType];
        ViewManager.Instance.ClearStack();
        SceneManager.LoadSceneAsync(sceneAsset.name , LoadSceneMode.Single);
    }
}
