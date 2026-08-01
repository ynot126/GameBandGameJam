using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] int maxHealth = 100;
    int currentHealth;

    public void Initialize()
    {
        currentHealth = maxHealth;
    }
    public async UniTask SpawnAnimation()
    {
        transform.localScale = Vector3.zero;
        await transform.DOScale(1f, 1f);
    }
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