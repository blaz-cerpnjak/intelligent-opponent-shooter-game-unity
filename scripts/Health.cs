using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int startingHealth = 100;
    public int currentHealth;

    public UnityEvent OnDeath;
    public UnityEvent<int> OnDamage;

    private void Awake()
    {
        currentHealth = startingHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        OnDamage?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnDeath?.Invoke();
        }
    }
}
