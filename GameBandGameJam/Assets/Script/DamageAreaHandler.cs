#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DamageAreaHandler : MonoBehaviour
{
    const float IndicatorDurationSeconds = 0.2f;

    [SerializeField] Transform sphereIndicator = null!;
    CancellationTokenSource? indicatorCts;

    public void Initialize()
    {
        sphereIndicator.gameObject.SetActive(false);
    }

    public List<IDamageable> GetDamageables(DamageDetectionData damageDetectionData)
    {
#if UNITY_EDITOR
        ShowIndicator(damageDetectionData.detectionCenter, damageDetectionData.detectionRadius).Forget();
#endif
        var colliders = Physics.OverlapSphere(
            damageDetectionData.detectionCenter,
            damageDetectionData.detectionRadius,
            damageDetectionData.layerMask,
            QueryTriggerInteraction.Ignore
        );
        return colliders.Select(detectedCollider => detectedCollider.GetComponentInParent<IDamageable>()).Where(damageable => damageable != null).ToList();
    }

    async UniTask ShowIndicator(Vector3 center, float radius)
    {
        indicatorCts?.Cancel();
        indicatorCts?.Dispose();
        indicatorCts = new CancellationTokenSource();
        var token = indicatorCts.Token;

        sphereIndicator.position = center;
        // Default Unity sphere mesh has radius 0.5, so diameter scale matches OverlapSphere radius.
        sphereIndicator.localScale = Vector3.one * (radius * 2f);
        sphereIndicator.gameObject.SetActive(true);

        await UniTask.Delay(TimeSpan.FromSeconds(IndicatorDurationSeconds), cancellationToken: token);
        sphereIndicator.gameObject.SetActive(false);
    }
}
public class DamageDetectionData
{
    public Vector3 detectionCenter;
    public float detectionRadius;
    public int layerMask;
}
