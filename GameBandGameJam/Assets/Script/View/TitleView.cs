#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class TitleView : BaseView
{
   [SerializeField] Button startButton = null!;
   [SerializeField] Button shortCutButton = null!;
   public event Action? OnStartButtonPressed;
   public event Action? OnShortCutButtonPressed;

   public void Initialize()
   {
      startButton.onClick.AddListener(()=> OnStartButtonPressed?.Invoke());
      
#if UNITY_EDITOR
      shortCutButton.onClick.AddListener(()=> OnShortCutButtonPressed?.Invoke());
#else
      shortCutButton.gameObject.SetActive(false);
#endif
   }
}
