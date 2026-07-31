# nullable enable
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] PlayerController playerController = null!;

    PlayerData playerData = null!;
    public void Initialize(PlayerData aPlayerData)
    {
        playerData = aPlayerData;  
        playerController.Initialize(playerData.speed);
    }
}
