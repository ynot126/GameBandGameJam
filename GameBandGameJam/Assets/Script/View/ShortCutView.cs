#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class ShortCutView : BaseView
{
    [SerializeField] GameStageConfig gameStageConfig = null!;
    [SerializeField] Transform buttonContainer = null!;
    [SerializeField] ShortCutViewButton buttonPrefab = null!;
    [SerializeField] Button backButton = null!;
    public event Action<GameStageType>? OnShortCutButtonPressed;
    public event Action? OnBackButtonPressed;

    public void Initialize()
    {
        foreach (var gameStage in gameStageConfig.gameStages)
        {
            var scene = gameStage.Value;
            if (!scene)
                continue;

            var button = Instantiate(buttonPrefab, buttonContainer);
            button.Text.text = scene.name;
            button.Button.onClick.AddListener(
                () => OnShortCutButtonPressed?.Invoke(gameStage.Key));
        }
        
        backButton.onClick.AddListener(()=> OnBackButtonPressed?.Invoke());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackButtonPressed?.Invoke();
        }
    }
}
