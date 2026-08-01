using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] int maxHealth = 100;
    int currentHealth;

    public void Damage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Death();
    }

    void Death()
    {
        Destroy(gameObject);
    }
}