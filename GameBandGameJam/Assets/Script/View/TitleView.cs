#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class TitleView : BaseView
{
   [SerializeField] Button startButton = null!;
   [SerializeField] Button quitButton = null!;
   [SerializeField] Button shortCutButton = null!;
   public event Action? OnStartButtonPressed;
   public event Action? OnShortCutButtonPressed;
   public event Action? OnQuitButtonPressed;

   public void Initialize()
   {
      startButton.onClick.AddListener(()=> OnStartButtonPressed?.Invoke());
      quitButton.onClick.AddListener(()=> OnQuitButtonPressed?.Invoke());
          
#if UNITY_EDITOR || DEVELOPMENT_BUILD
      shortCutButton.onClick.AddListener(()=> OnShortCutButtonPressed?.Invoke());
#else
      shortCutButton.gameObject.SetActive(false);
#endif
   }
}
