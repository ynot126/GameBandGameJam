using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance ;
    
    void Awake()
    {
        // Enforce the singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Delete duplicates
            return;
        }

        Instance = (T)this;
        
        // Optional: Persist across scene loads
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    public abstract void Initialize();
}
