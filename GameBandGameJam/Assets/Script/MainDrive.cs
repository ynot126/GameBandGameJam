#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

public class MainDrive : MonoBehaviour
{
    [Header("View")]
    [SerializeField] TitleView titleViewPrefab = null!;
    
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
        return titleView;
    }

    void HandleTitleViewStartButton()
    {
        Debug.Log("Start button pressed");
    }
    #endregion
}
