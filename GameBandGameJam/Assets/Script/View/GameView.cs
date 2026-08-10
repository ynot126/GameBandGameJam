#nullable enable
using TMPro;
using UnityEngine;

public class GameView : BaseView
{
   [SerializeField] TextMeshProUGUI healthText = null!;
   [SerializeField] TextMeshProUGUI staminaText = null!;

   public void UpdateHealthText(int currentHealth, int newHealth)
   {
      healthText.text = $"Player Health: {currentHealth}/{newHealth}";
   }

   public void UpdateStaminaText(int currentStamina, int maxStamina)
   {
      staminaText.text = $"Player Stamina: {currentStamina}/{maxStamina}";
   }
}
