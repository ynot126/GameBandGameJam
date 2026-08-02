#nullable enable
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageNumberVisual : MonoBehaviour
{
    const float PopDuration = 0.15f;
    const float FloatDuration = 0.7f;
    const float FloatHeight = 1.25f;
    const float SidewaysDrift = 0.4f;
    const float FadeStartNormalized = 0.4f;

    [SerializeField] TextMeshProUGUI damageText = null!;

    public void Initialize(Vector3 damagePosition, int damage)
    {
        var cam = Camera.main;
        if (!cam)
        {
            Debug.LogWarning("No camera found");
            return;
        }

        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
            cam.transform.rotation * Vector3.up);

        transform.position = damagePosition;
        damageText.text = damage.ToString();

        DamageAnimation().Forget();
    }

    async UniTask DamageAnimation()
    {
        transform.localScale = Vector3.zero;

        var color = damageText.color;
        color.a = 1f;
        damageText.color = color;

        var drift = Random.Range(-SidewaysDrift, SidewaysDrift);
        var endPosition = transform.position + Vector3.up * FloatHeight + transform.right * drift;
        var fadeDelay = FloatDuration * FadeStartNormalized;
        var fadeDuration = FloatDuration - fadeDelay;

        await DOTween.Sequence()
            .SetLink(gameObject)
            .Append(transform.DOScale(1f, PopDuration).SetEase(Ease.OutBack))
            .Join(transform.DOMove(endPosition, FloatDuration).SetEase(Ease.OutCubic))
            .Insert(fadeDelay, damageText.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));

        Destroy(gameObject);
    }
}
