#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "ImpactFrameParticleConfig", menuName = "Configs/ImpactFrameParticleConfig")]
public class ImpactFrameParticleConfig : ScriptableObject
{
    [SerializeField] GameObject[] particlePrefabs = System.Array.Empty<GameObject>();

    public void PlayRandomAt(Vector3 position, Quaternion rotation)
    {
        if (particlePrefabs == null || particlePrefabs.Length == 0)
        {
            return;
        }

        var validCount = 0;
        for (var i = 0; i < particlePrefabs.Length; i++)
        {
            if (particlePrefabs[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return;
        }

        var pick = Random.Range(0, validCount);
        GameObject? prefab = null;
        for (var i = 0; i < particlePrefabs.Length; i++)
        {
            if (particlePrefabs[i] == null)
            {
                continue;
            }

            if (pick == 0)
            {
                prefab = particlePrefabs[i];
                break;
            }

            pick--;
        }

        if (prefab == null)
        {
            return;
        }

        var instance = Instantiate(prefab, position, rotation);
        var particleSystem = instance.GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
        {
            if (!particleSystem.isPlaying)
            {
                particleSystem.Play(true);
            }

            var main = particleSystem.main;
            var lifetime = main.duration;
            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            {
                lifetime += main.startLifetime.constant;
            }
            else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                lifetime += main.startLifetime.constantMax;
            }
            else
            {
                lifetime += 1f;
            }

            Destroy(instance, Mathf.Max(0.1f, lifetime + 0.25f));
            return;
        }

        Destroy(instance, 2f);
    }
}
