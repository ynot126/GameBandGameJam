#nullable enable
using TMPro;
using UnityEngine;

public class GameView : BaseView
{
   [SerializeField] TextMeshProUGUI healthText = null!;
   
   public void UpdateHealthText(int currentHealth, int newHealth)
   {
      healthText.text = $"Player Health: {currentHealth}/{newHealth}";
   }
}
