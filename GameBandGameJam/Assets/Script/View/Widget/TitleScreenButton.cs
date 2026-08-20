#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleScreenButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] RectTransform textTransform = null!;
    [SerializeField] Image image = null!;
    [SerializeField] Button button = null!;
    [SerializeField] float highlightMoveDistance = 40f;
    [SerializeField] float animationDuration = 0.2f;

    public event Action? OnButtonPressed;
    Vector2 originalAnchoredPosition;
    CancellationTokenSource? animationCts;

    public void Initialize()
    {
        button.onClick.AddListener(()=>OnButtonPressed?.Invoke()); 
        originalAnchoredPosition = textTransform.anchoredPosition;
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
        textTransform.anchoredPosition = originalAnchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateHighlightAsync(highlighted: true).Forget();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateHighlightAsync(highlighted: false).Forget();
    }

    async UniTaskVoid AnimateHighlightAsync(bool highlighted)
    {
        animationCts?.Cancel();
        animationCts?.Dispose();
        animationCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        var token = animationCts.Token;

        var targetPosition = originalAnchoredPosition;
        if (highlighted)
        {
            targetPosition.x -= highlightMoveDistance;
        }

        var targetAlpha = highlighted ? 1f : 0f;

        textTransform.DOKill();
        image.DOKill();

        var moveTween = textTransform
            .DOAnchorPos(targetPosition, animationDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);
        var fadeTween = image
            .DOFade(targetAlpha, animationDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);

        try
        {
            await UniTask.WhenAll(
                moveTween.ToUniTask(cancellationToken: token),
                fadeTween.ToUniTask(cancellationToken: token));
        }
        catch (OperationCanceledException)
        {
        }
    }
}
