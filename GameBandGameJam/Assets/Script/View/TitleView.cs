#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class TitleView : BaseView
{
   [SerializeField] Button startButton = null!;
   public event Action? OnStartButtonPressed;

   public void Initialize()
   {
      startButton.onClick.AddListener(()=> OnStartButtonPressed?.Invoke());
   }
}
