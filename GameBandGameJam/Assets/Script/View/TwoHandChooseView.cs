#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class TwoHandChooseView : BaseView
{
    [SerializeField] Button leftButton;
    [SerializeField] Button rightButton;

    public event Action? OnSelect;
    public void Initialize()
    {
        leftButton.onClick.AddListener(()=>OnSelect?.Invoke());
        rightButton.onClick.AddListener(()=>OnSelect?.Invoke());
    }
}
