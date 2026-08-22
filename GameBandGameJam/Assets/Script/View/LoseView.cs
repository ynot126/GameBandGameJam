#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class LoseView : BaseView
{
    [SerializeField] Button restartButton = null!;

    public event Action? OnRestartButtonPressed;

    public void Initialize()
    {
        restartButton.onClick.AddListener(()=> OnRestartButtonPressed?.Invoke());
    }

}
