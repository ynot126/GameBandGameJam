#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameCameraController : Singleton<GameCameraController>
{
    [Header("Tracking")]
    [SerializeField] Vector3 positionOffset = new(0f, 8f, -10f);
    [SerializeField, Range(0f, 1f)] float positionLerpPercentage = 0.15f;
    [SerializeField, Min(0f)] float snapDistance = 25f;

    [Header("Rotation")]
    [SerializeField] Vector3 rotationEulerAngles = new(30f, 0f, 0f);
    [SerializeField, Range(0f, 1f)] float rotationLerpPercentage = 0.15f;

    [Header("Zoom")]
    [SerializeField, Min(0f)] float zoomTransitionSeconds = 0.25f;

    Transform? trackedPlayerTransform;
    bool trackingPlayer;
    float zoomPullAmount;
    Vector3 trackingPosition;
    Vector3 shakeOffset;
    CancellationTokenSource? zoomCts;
    CancellationTokenSource? shakeCts;

    void Update()
    {
        if (trackingPlayer && trackedPlayerTransform)
        {
            UpdatePlayerTracking(trackedPlayerTransform.position);
        }

        transform.position = trackingPosition + shakeOffset;
    }

    public void StartTrackingPlayer(Transform playerTransform)
    {
        trackedPlayerTransform = playerTransform;
        trackingPlayer = true;
        trackingPosition = transform.position;
    }

    /// <summary>
    /// Animates the camera closer by <paramref name="zoomDepth"/> world units along the tracking offset.
    /// Pair with <see cref="EndZoom"/> from an Animation Event when framing should restore.
    /// </summary>
    public void BeginZoom(float zoomDepth)
    {
        AnimateZoomToAsync(Mathf.Max(0f, zoomDepth)).Forget();
    }

    /// <summary>
    /// Animates the camera back to the default tracking offset after <see cref="BeginZoom"/>.
    /// </summary>
    public void EndZoom()
    {
        AnimateZoomToAsync(0f).Forget();
    }

    /// <summary>
    /// Zooms the camera closer by <paramref name="zoomDepth"/> world units along the tracking offset,
    /// holds for <paramref name="holdSeconds"/>, then restores the original framing.
    /// </summary>
    public void ZoomIn(float holdSeconds, float zoomDepth)
    {
        ZoomInAsync(holdSeconds, zoomDepth).Forget();
    }

    /// <summary>
    /// Shakes the camera for <paramref name="duration"/> seconds.
    /// <paramref name="strength"/> is the maximum positional offset in world units.
    /// <paramref name="frequency"/> is how fast the shake oscillates (higher = more jittery).
    /// </summary>
    public void Shake(float duration, float strength, float frequency)
    {
        ShakeAsync(duration, strength, frequency).Forget();
    }

    async UniTaskVoid AnimateZoomToAsync(float targetPull)
    {
        zoomCts?.Cancel();
        zoomCts?.Dispose();
        zoomCts = new CancellationTokenSource();
        var token = zoomCts.Token;

        try
        {
            await AnimateZoomPull(targetPull, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async UniTaskVoid ZoomInAsync(float holdSeconds, float zoomDepth)
    {
        zoomCts?.Cancel();
        zoomCts?.Dispose();
        zoomCts = new CancellationTokenSource();
        var token = zoomCts.Token;

        var clampedDepth = Mathf.Max(0f, zoomDepth);
        var clampedHold = Mathf.Max(0f, holdSeconds);

        try
        {
            await AnimateZoomPull(clampedDepth, token);
            if (clampedHold > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(clampedHold), cancellationToken: token);
            }

            await AnimateZoomPull(0f, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async UniTaskVoid ShakeAsync(float duration, float strength, float frequency)
    {
        shakeCts?.Cancel();
        shakeCts?.Dispose();
        shakeCts = new CancellationTokenSource();
        var token = shakeCts.Token;

        var clampedDuration = Mathf.Max(0f, duration);
        var clampedStrength = Mathf.Max(0f, strength);
        var clampedFrequency = Mathf.Max(0f, frequency);

        if (clampedDuration <= 0f || clampedStrength <= 0f)
        {
            shakeOffset = Vector3.zero;
            return;
        }

        var seedX = UnityEngine.Random.value * 100f;
        var seedY = UnityEngine.Random.value * 100f;
        var seedZ = UnityEngine.Random.value * 100f;
        var elapsed = 0f;

        try
        {
            while (elapsed < clampedDuration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var fade = 1f - Mathf.Clamp01(elapsed / clampedDuration);
                var sampleTime = elapsed * clampedFrequency;

                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(seedX, sampleTime) * 2f - 1f) * clampedStrength * fade,
                    (Mathf.PerlinNoise(seedY, sampleTime) * 2f - 1f) * clampedStrength * fade,
                    (Mathf.PerlinNoise(seedZ, sampleTime) * 2f - 1f) * clampedStrength * fade);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                shakeOffset = Vector3.zero;
            }
        }
    }

    async UniTask AnimateZoomPull(float targetPull, CancellationToken token)
    {
        var startPull = zoomPullAmount;
        if (zoomTransitionSeconds <= 0f)
        {
            zoomPullAmount = targetPull;
            return;
        }

        var elapsed = 0f;
        while (elapsed < zoomTransitionSeconds)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / zoomTransitionSeconds);
            zoomPullAmount = Mathf.Lerp(startPull, targetPull, t);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        zoomPullAmount = targetPull;
    }

    void UpdatePlayerTracking(Vector3 playerPosition)
    {
        var targetPosition = playerPosition + GetEffectivePositionOffset();

        if (snapDistance > 0f &&
            Vector3.Distance(trackingPosition, targetPosition) >= snapDistance)
        {
            trackingPosition = targetPosition;
        }
        else
        {
            trackingPosition = Vector3.Lerp(
                trackingPosition,
                targetPosition,
                positionLerpPercentage);
        }

        var targetRotation = Quaternion.Euler(rotationEulerAngles);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationLerpPercentage);
    }

    Vector3 GetEffectivePositionOffset()
    {
        if (zoomPullAmount <= 0f)
        {
            return positionOffset;
        }

        var magnitude = positionOffset.magnitude;
        if (magnitude <= 0.0001f)
        {
            return positionOffset;
        }

        var zoomedMagnitude = Mathf.Max(0.01f, magnitude - zoomPullAmount);
        return positionOffset.normalized * zoomedMagnitude;
    }

    void OnDestroy()
    {
        zoomCts?.Cancel();
        zoomCts?.Dispose();
        zoomCts = null;

        shakeCts?.Cancel();
        shakeCts?.Dispose();
        shakeCts = null;
    }
}
