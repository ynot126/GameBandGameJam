#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class TitleView : BaseView
{
   [SerializeField] TitleScreenButton startButton = null!;
   [SerializeField] TitleScreenButton quitButton = null!;
   // [SerializeField] Button shortCutButton = null!;
   public event Action? OnStartButtonPressed;
   public event Action? OnShortCutButtonPressed;
   public event Action? OnQuitButtonPressed;

   public void Initialize()
   {
      startButton.OnButtonPressed+=()=>OnStartButtonPressed?.Invoke();
      quitButton.OnButtonPressed+=()=>OnQuitButtonPressed?.Invoke();

      var titleScreenButtons = GetComponentsInChildren<TitleScreenButton>(true);
      for (var i = 0; i < titleScreenButtons.Length; i++)
      {
         titleScreenButtons[i].Initialize();
      }
          
// #if UNITY_EDITOR || DEVELOPMENT_BUILD
//       shortCutButton.onClick.AddListener(()=> OnShortCutButtonPressed?.Invoke());
// #else
//       shortCutButton.gameObject.SetActive(false);
// #endif
   }
}
