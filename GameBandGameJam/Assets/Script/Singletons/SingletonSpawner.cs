#nullable enable
using System.Collections.Generic;
using UnityEngine;

public class SingletonSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> singletonPrefabs = null!;

    public void Awake()
    {
        foreach (GameObject prefab in singletonPrefabs)
        {
            if (prefab == null) continue;
            Instantiate(prefab);
        }
        Destroy(gameObject);
    }
}