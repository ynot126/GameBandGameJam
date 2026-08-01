using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] int maxHealth = 100;


    public void Damage(int damage)
    {
        Debug.Log(gameObject.name + " damage: " + damage);
    }
}
