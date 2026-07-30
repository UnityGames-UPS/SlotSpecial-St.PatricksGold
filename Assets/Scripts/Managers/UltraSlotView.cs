using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the three-reel, three-row slot shown before the Ultra Wheel feature.
/// Results are row-major and contain nine values:
/// 0 (or -1) = empty, 1 = Green wheel, 2 = Blue wheel, 3 = Red wheel.
/// </summary>
public class UltraSlotView : MonoBehaviour
{
    public const int ReelCount = 3;
    public const int RowCount = 3;
    public const int ResultCellCount = ReelCount * RowCount;
    public const int CenterRowIndex = 1;
    public const int EmptySymbolId = 0;
    public const int GreenWheelSymbolId = 1;
    public const int BlueWheelSymbolId = 2;
    public const int RedWheelSymbolId = 3;

    private const int ImagesPerReel = 5;
    private const int FirstVisibleImageIndex = 1;
    private const int LastVisibleImageIndex = FirstVisibleImageIndex + RowCount - 1;

    [Header("Ultra Symbols (1 Green, 2 Blue, 3 Red)")]
    [UnityEngine.Serialization.FormerlySerializedAs("spriteCoinOne")]
    [SerializeField] private Sprite spriteGreenWheelSymbol;
    [UnityEngine.Serialization.FormerlySerializedAs("spriteCoinTwo")]
    [SerializeField] private Sprite spriteBlueWheelSymbol;
    [UnityEngine.Serialization.FormerlySerializedAs("spriteCoinThree")]
    [SerializeField] private Sprite spriteRedWheelSymbol;

    [Header("Reels")]
    [Tooltip("Optional parent whose first three children are the ultra reels.")]
    [SerializeField] private Transform reelsRoot;
    [SerializeField] private Transform[] reelTransforms = new Transform[ReelCount];
    [SerializeField] private List<UltraReelImages> reelImagesList = new List<UltraReelImages>(ReelCount);

    [Header("Initial Result")]
    [Tooltip("Nine row-major values for the 3x3 result. Use 0 for empty, 1 for Green, 2 for Blue, and 3 for Red.")]
    [SerializeField] private int[] initialResult =
    {
        GreenWheelSymbolId,
        BlueWheelSymbolId,
        RedWheelSymbolId,
        BlueWheelSymbolId,
        RedWheelSymbolId,
        GreenWheelSymbolId,
        RedWheelSymbolId,
        GreenWheelSymbolId,
        BlueWheelSymbolId
    };

    [Header("Spin Strip")]
    [Tooltip("Values used only while spinning. Repeated values control their visual frequency.")]
    [SerializeField] private int[] spinStrip =
    {
        EmptySymbolId,
        GreenWheelSymbolId,
        EmptySymbolId,
        BlueWheelSymbolId,
        EmptySymbolId,
        RedWheelSymbolId
    };

    [Header("Spin Settings")]
    [Tooltip("Distance between adjacent reel images, including layout spacing.")]
    [SerializeField, Min(1f)] private float symbolStep = 421f;
    [Tooltip("Seconds for a reel to travel by one symbol at normal speed.")]
    [SerializeField, Min(0.01f)] private float spinCycleDuration = 0.12f;
    [SerializeField, Range(0.25f, 2f)] private float normalSpeedMultiplier = 1f;
    [SerializeField, Range(0.25f, 2f)] private float fastSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float reelStartStagger = 0.08f;
    [SerializeField, Min(0f)] private float reelStopStagger = 0.12f;
    [SerializeField, Min(0)] private int minimumCyclesBeforeStop = 3;

    [Header("Stop Settings")]
    [SerializeField, Range(2f, 5f)] private float finalStopDurationMultiplier = 3f;
    [SerializeField, Min(0f)] private float stopBounceDistance = 12f;
    [SerializeField, Min(0.01f)] private float stopBounceReturnDuration = 0.16f;
    [SerializeField, Min(0f)] private float quickStopStagger = 0.05f;
    [SerializeField, Min(0.01f)] private float quickStopDuration = 0.18f;

    public event Action<IReadOnlyList<int>> SpinCompleted;

    public bool IsSpinning => isSpinning;
    public IReadOnlyList<int> CurrentResult => currentResult;

    private readonly List<int> currentResult = new List<int>(ResultCellCount);
    private readonly List<List<int>> reelBufferSymbols = new List<List<int>>(ReelCount);
    private readonly List<int> reelCycleCounts = new List<int>(ReelCount);
    private readonly List<Tween> reelTweens = new List<Tween>(ReelCount);

    private Coroutine stopCoroutine;
    private bool isSpinning;
    private bool isStopping;
    private SpinSpeed activeSpinSpeed = SpinSpeed.Normal;
    private float restingY;

    private void Awake()
    {
        TryBuildReelReferencesFromRoot();
        InitializeState();

        if (!ShowConfiguredInitialResult())
        {
            Debug.LogError("[UltraSlotView] The ultra slot could not be initialized. Check its Inspector references.");
        }
    }

    /// <summary>
    /// Displays the Inspector-configured symbols before the Ultra slot spins.
    /// These symbols are visual setup only; the stopped result is supplied
    /// separately by the server.
    /// </summary>
    public bool ShowConfiguredInitialResult()
    {
        var startResult = new List<int>(ResultCellCount);
        for (int cell = 0; cell < ResultCellCount; cell++)
        {
            int symbolId = initialResult != null && cell < initialResult.Length
                ? NormalizeSymbolId(initialResult[cell])
                : EmptySymbolId;
            startResult.Add(symbolId);
        }

        return SetInitialResult(startResult);
    }

    /// <summary>
    /// Sets the stopped 3x3 result without playing a spin.
    /// </summary>
    public bool SetInitialResult(IList<int> result)
    {
        if (!TryValidateResult(result, out string error))
        {
            Debug.LogError($"[UltraSlotView] Invalid initial result: {error}");
            return false;
        }

        KillReelTweens();
        RestoreCurrentResultSprites();
        isSpinning = false;
        isStopping = false;
        StoreCurrentResult(result);

        for (int reel = 0; reel < ReelCount; reel++)
        {
            FillReelAroundResult(reel);
            ResetReelPosition(reel);
            ApplyStoppedReelVisibility(reel);
        }

        return true;
    }

    /// <summary>
    /// Starts all three reels. Call StopSpin, QuickStop, or ShowResultImmediately
    /// once the nine-cell result is available.
    /// </summary>
    public bool StartSpin(SpinSpeed speed = SpinSpeed.Normal)
    {
        if (isSpinning)
        {
            return false;
        }

        if (!TryValidateSetup(out string error))
        {
            Debug.LogError($"[UltraSlotView] Cannot start: {error}");
            return false;
        }

        KillReelTweens();
        RestoreCurrentResultSprites();
        activeSpinSpeed = speed;
        isSpinning = true;
        isStopping = false;

        for (int reel = 0; reel < ReelCount; reel++)
        {
            reelCycleCounts[reel] = 0;
            RenderReel(reel, false);
            StartReelWithDelay(reel, reel * reelStartStagger);
        }

        return true;
    }

    public void StopSpin(IList<int> result, Action onComplete = null)
    {
        BeginStop(result, false, onComplete);
    }

    public void QuickStop(IList<int> result, Action onComplete = null)
    {
        BeginStop(result, true, onComplete);
    }

    public void ShowResultImmediately(IList<int> result, Action onComplete = null)
    {
        if (!TryValidateResult(result, out string error))
        {
            Debug.LogError($"[UltraSlotView] Cannot show result: {error}");
            CancelSpin();
            onComplete?.Invoke();
            return;
        }

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
            stopCoroutine = null;
        }

        StopAllCoroutines();
        KillReelTweens();
        isSpinning = false;
        isStopping = false;
        StoreCurrentResult(result);

        for (int reel = 0; reel < ReelCount; reel++)
        {
            FillReelAroundResult(reel);
            ResetReelPosition(reel);
            ApplyStoppedReelVisibility(reel);
        }

        SpinCompleted?.Invoke(CurrentResult);
        onComplete?.Invoke();
    }

    internal bool TryGetWinningSymbolAnimationTargets(
        out List<UltraWinningSymbolAnimationTarget> targets)
    {
        RestoreCurrentResultSprites();
        targets = new List<UltraWinningSymbolAnimationTarget>();

        if (currentResult.Count != ResultCellCount ||
            reelImagesList == null ||
            reelImagesList.Count != ReelCount)
        {
            return false;
        }

        for (int row = 0; row < RowCount; row++)
        {
            for (int reel = 0; reel < ReelCount; reel++)
            {
                int resultIndex = GetResultIndex(row, reel);
                int symbolId = currentResult[resultIndex];
                if (symbolId == EmptySymbolId)
                {
                    continue;
                }

                int imageIndex = FirstVisibleImageIndex + row;
                Image baseImage =
                    reelImagesList[reel]?.images?[imageIndex];
                Image animationImage =
                    GetWinningAnimationImage(reel, imageIndex);
                Image winIndicatorImage =
                    GetWinIndicatorImage(baseImage);
                if (baseImage == null || animationImage == null)
                {
                    Debug.LogWarning(
                        $"[UltraSlotView] Assign the {GetWheelColorName(symbolId)} " +
                        $"winning animation Image for Ultra reel {reel + 1}, " +
                        $"visible row {row + 1}.");
                    continue;
                }

                targets.Add(new UltraWinningSymbolAnimationTarget
                {
                    SymbolId = symbolId,
                    BaseImage = baseImage,
                    AnimationImage = animationImage,
                    WinIndicatorImage = winIndicatorImage
                });
            }
        }

        return targets.Count > 0;
    }

    private Image GetWinningAnimationImage(
        int reel,
        int imageIndex)
    {
        if (reelImagesList == null ||
            reel < 0 ||
            reel >= reelImagesList.Count ||
            reelImagesList[reel]?.winAnimationImages == null)
        {
            return null;
        }

        List<Image> animationImages =
            reelImagesList[reel].winAnimationImages;
        const int visibleImageCount = RowCount;

        // The live server result places each active Ultra symbol on the
        // center row. Support one center overlay per reel as well as an
        // explicit Top/Middle/Bottom list.
        if (animationImages.Count == 1)
        {
            int centerImageIndex =
                FirstVisibleImageIndex + CenterRowIndex;
            return imageIndex == centerImageIndex
                ? animationImages[0]
                : null;
        }

        if (animationImages.Count == visibleImageCount)
        {
            int visibleIndex =
                imageIndex - FirstVisibleImageIndex;
            return visibleIndex >= 0 &&
                   visibleIndex < animationImages.Count
                ? animationImages[visibleIndex]
                : null;
        }

        return imageIndex >= 0 &&
               imageIndex < animationImages.Count
            ? animationImages[imageIndex]
            : null;
    }

    private static Image GetWinIndicatorImage(
        Image baseImage)
    {
        if (baseImage == null)
        {
            return null;
        }

        Transform winTransform =
            baseImage.transform.Find("Win");
        return winTransform != null &&
               winTransform.TryGetComponent(
                   out Image winIndicatorImage)
            ? winIndicatorImage
            : null;
    }

    /// <summary>
    /// Converts a row-major 3x3 server result into nine symbol IDs.
    /// Empty, blank, null, 0, and -1 values are all treated as empty positions.
    /// </summary>
    public bool TryParseServerResult(
        IList<string> serverResult,
        out List<int> parsedResult,
        out string error)
    {
        parsedResult = null;
        error = null;

        if (serverResult == null || serverResult.Count != ResultCellCount)
        {
            error = $"Ultra result must contain exactly {ResultCellCount} values for a 3x3 grid.";
            return false;
        }

        var converted = new List<int>(ResultCellCount);
        for (int cell = 0; cell < ResultCellCount; cell++)
        {
            string value = serverResult[cell];
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, "empty", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "blank", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            {
                converted.Add(EmptySymbolId);
                continue;
            }

            if (!int.TryParse(value, out int symbolId))
            {
                error = $"Ultra result value '{value}' at cell {cell} is not valid.";
                return false;
            }

            symbolId = NormalizeSymbolId(symbolId);
            if (!IsKnownSymbol(symbolId))
            {
                error = $"Ultra result symbol {symbolId} at cell {cell} must be empty, 1, 2, or 3.";
                return false;
            }

            converted.Add(symbolId);
        }

        parsedResult = converted;
        return true;
    }

    public bool TryValidateResult(IList<int> result, out string error)
    {
        if (!TryValidateSetup(out error))
        {
            return false;
        }

        if (result == null || result.Count != ResultCellCount)
        {
            error = $"Ultra result must contain exactly {ResultCellCount} values for a 3x3 grid.";
            return false;
        }

        for (int cell = 0; cell < ResultCellCount; cell++)
        {
            int symbolId = NormalizeSymbolId(result[cell]);
            if (!IsKnownSymbol(symbolId))
            {
                error = $"Ultra result symbol {result[cell]} at cell {cell} must be empty, 1, 2, or 3.";
                return false;
            }

            if (symbolId != EmptySymbolId && GetSymbolSprite(symbolId) == null)
            {
                error = $"The sprite for ultra symbol {symbolId} is not assigned.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public void CancelSpin()
    {
        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
            stopCoroutine = null;
        }

        StopAllCoroutines();
        KillReelTweens();
        RestoreCurrentResultSprites();
        isSpinning = false;
        isStopping = false;

        if (currentResult.Count != ResultCellCount || !TryValidateSetup(out _))
        {
            return;
        }

        for (int reel = 0; reel < ReelCount; reel++)
        {
            FillReelAroundResult(reel);
            ResetReelPosition(reel);
            ApplyStoppedReelVisibility(reel);
        }
    }

    [ContextMenu("Auto Assign Reels From Root")]
    private void AutoAssignReelsFromRoot()
    {
        if (!TryBuildReelReferencesFromRoot())
        {
            Debug.LogWarning("[UltraSlotView] Assign the Slots object to Reels Root first.");
        }
    }

    private void InitializeState()
    {
        currentResult.Clear();
        reelBufferSymbols.Clear();
        reelCycleCounts.Clear();
        reelTweens.Clear();

        for (int cell = 0; cell < ResultCellCount; cell++)
        {
            currentResult.Add(EmptySymbolId);
        }

        for (int reel = 0; reel < ReelCount; reel++)
        {
            reelCycleCounts.Add(0);
            reelTweens.Add(null);

            var buffer = new List<int>(ImagesPerReel);
            for (int image = 0; image < ImagesPerReel; image++)
            {
                buffer.Add(EmptySymbolId);
            }
            reelBufferSymbols.Add(buffer);
        }

        restingY = reelTransforms != null &&
                   reelTransforms.Length > 0 &&
                   reelTransforms[0] != null
            ? reelTransforms[0].localPosition.y
            : 0f;
    }

    private bool TryBuildReelReferencesFromRoot()
    {
        if (reelsRoot == null || reelsRoot.childCount < ReelCount)
        {
            return false;
        }

        bool needsTransforms = reelTransforms == null || reelTransforms.Length != ReelCount;
        if (!needsTransforms)
        {
            for (int reel = 0; reel < ReelCount; reel++)
            {
                if (reelTransforms[reel] == null)
                {
                    needsTransforms = true;
                    break;
                }
            }
        }

        if (needsTransforms)
        {
            reelTransforms = new Transform[ReelCount];
            for (int reel = 0; reel < ReelCount; reel++)
            {
                reelTransforms[reel] = reelsRoot.GetChild(reel);
            }
        }

        bool needsImages = reelImagesList == null || reelImagesList.Count != ReelCount;
        if (!needsImages)
        {
            for (int reel = 0; reel < ReelCount; reel++)
            {
                if (reelImagesList[reel] == null ||
                    reelImagesList[reel].images == null ||
                    reelImagesList[reel].images.Count != ImagesPerReel)
                {
                    needsImages = true;
                    break;
                }
            }
        }

        if (needsImages)
        {
            reelImagesList = new List<UltraReelImages>(ReelCount);
            for (int reel = 0; reel < ReelCount; reel++)
            {
                var group = new UltraReelImages();
                Transform reelTransform = reelTransforms[reel];
                for (int image = 0; image < reelTransform.childCount && group.images.Count < ImagesPerReel; image++)
                {
                    Image childImage = reelTransform.GetChild(image).GetComponent<Image>();
                    if (childImage != null)
                    {
                        group.images.Add(childImage);
                    }
                }
                reelImagesList.Add(group);
            }
        }

        return true;
    }

    private bool TryValidateSetup(out string error)
    {
        if (reelTransforms == null || reelTransforms.Length != ReelCount)
        {
            error = $"Assign exactly {ReelCount} reel transforms.";
            return false;
        }

        if (reelImagesList == null || reelImagesList.Count != ReelCount)
        {
            error = $"Assign exactly {ReelCount} reel image groups.";
            return false;
        }

        for (int reel = 0; reel < ReelCount; reel++)
        {
            if (reelTransforms[reel] == null)
            {
                error = $"Reel transform {reel} is not assigned.";
                return false;
            }

            UltraReelImages reelImages = reelImagesList[reel];
            if (reelImages == null ||
                reelImages.images == null ||
                reelImages.images.Count != ImagesPerReel)
            {
                error = $"Ultra reel {reel} must have exactly {ImagesPerReel} images.";
                return false;
            }

            for (int image = 0; image < ImagesPerReel; image++)
            {
                if (reelImages.images[image] == null)
                {
                    error = $"Image {image} on ultra reel {reel} is not assigned.";
                    return false;
                }
            }
        }

        if (spriteGreenWheelSymbol == null ||
            spriteBlueWheelSymbol == null ||
            spriteRedWheelSymbol == null)
        {
            error = "Assign the Green, Blue, and Red Ultra wheel symbol sprites.";
            return false;
        }

        if (spinStrip == null || spinStrip.Length == 0)
        {
            error = "The spin strip is empty.";
            return false;
        }

        for (int index = 0; index < spinStrip.Length; index++)
        {
            if (!IsKnownSymbol(NormalizeSymbolId(spinStrip[index])))
            {
                error = $"Spin Strip value {spinStrip[index]} at index {index} is invalid.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private void BeginStop(IList<int> result, bool quickStop, Action onComplete)
    {
        if (!TryValidateResult(result, out string error))
        {
            Debug.LogError($"[UltraSlotView] Cannot stop: {error}");
            CancelSpin();
            onComplete?.Invoke();
            return;
        }

        if (!isSpinning)
        {
            ShowResultImmediately(result, onComplete);
            return;
        }

        if (isStopping)
        {
            Debug.LogWarning("[UltraSlotView] A stop is already in progress.");
            return;
        }

        var safeResult = new List<int>(ResultCellCount);
        for (int cell = 0; cell < ResultCellCount; cell++)
        {
            safeResult.Add(NormalizeSymbolId(result[cell]));
        }

        isStopping = true;
        stopCoroutine = StartCoroutine(StopSequence(safeResult, quickStop, onComplete));
    }

    private IEnumerator StopSequence(List<int> result, bool quickStop, Action onComplete)
    {
        while (true)
        {
            bool ready = true;
            for (int reel = 0; reel < ReelCount; reel++)
            {
                if (reelCycleCounts[reel] < minimumCyclesBeforeStop)
                {
                    ready = false;
                    break;
                }
            }

            if (ready)
            {
                break;
            }
            yield return null;
        }

        int stoppedReels = 0;
        float stagger = quickStop ? quickStopStagger : reelStopStagger;
        for (int reel = 0; reel < ReelCount; reel++)
        {
            StartCoroutine(StopSingleReel(
                reel,
                GetResultColumn(result, reel),
                reel * stagger,
                quickStop,
                () => stoppedReels++));
        }

        while (stoppedReels < ReelCount)
        {
            yield return null;
        }

        StoreCurrentResult(result);
        isSpinning = false;
        isStopping = false;
        stopCoroutine = null;

        SpinCompleted?.Invoke(CurrentResult);
        onComplete?.Invoke();
    }

    private IEnumerator StopSingleReel(
        int reel,
        IReadOnlyList<int> targetColumn,
        float delay,
        bool quickStop,
        Action onStopped)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        KillReelTween(reel);

        Transform reelTransform = reelTransforms[reel];
        float cycleDuration = GetCycleDuration();
        float currentY = reelTransform.localPosition.y;
        float progress = Mathf.Clamp01(
            (restingY - currentY) / Mathf.Max(1f, symbolStep));
        float remainingDuration = cycleDuration * (1f - progress);

        if (remainingDuration > 0.001f)
        {
            Tween finishCycle = reelTransform
                .DOLocalMoveY(restingY - symbolStep, remainingDuration)
                .SetEase(Ease.Linear);
            reelTweens[reel] = finishCycle;
            yield return finishCycle.WaitForCompletion();
        }

        // Insert the bottom, middle, and top results in reverse order. After
        // the landing cycles they line up at visible buffer indices 1, 2, 3.
        CycleReel(reel, targetColumn[RowCount - 1]);
        ResetReelPosition(reel);

        int landingCycleCount = FirstVisibleImageIndex + RowCount - 1;
        for (int cycle = 0; cycle < landingCycleCount; cycle++)
        {
            bool finalCycle = cycle == landingCycleCount - 1;
            float bounceDistance = Mathf.Max(0f, stopBounceDistance) * (quickStop ? 0.7f : 1f);
            float targetY = restingY - symbolStep - (finalCycle ? bounceDistance : 0f);
            float duration = finalCycle
                ? (quickStop
                    ? quickStopDuration
                    : cycleDuration * Mathf.Max(2f, finalStopDurationMultiplier))
                : cycleDuration;

            Tween landingTween = reelTransform
                .DOLocalMoveY(targetY, duration)
                .SetEase(finalCycle ? Ease.OutCubic : Ease.Linear);
            reelTweens[reel] = landingTween;
            yield return landingTween.WaitForCompletion();

            int targetRow = RowCount - 2 - cycle;
            int? forcedTopSymbol = targetRow >= 0
                ? targetColumn[targetRow]
                : null;
            CycleReel(reel, forcedTopSymbol);
            reelTransform.localPosition = new Vector3(
                reelTransform.localPosition.x,
                finalCycle ? restingY - bounceDistance : restingY,
                reelTransform.localPosition.z);
        }

        Tween reboundTween = reelTransform
            .DOLocalMoveY(
                restingY,
                Mathf.Max(0.01f, stopBounceReturnDuration * (quickStop ? 0.7f : 1f)))
            .SetEase(Ease.OutBounce);
        reelTweens[reel] = reboundTween;
        yield return reboundTween.WaitForCompletion();

        ApplyStoppedReelVisibility(reel);
        onStopped?.Invoke();
    }

    private void StartReelWithDelay(int reel, float delay)
    {
        if (delay <= 0f)
        {
            StartReelCycle(reel);
            return;
        }

        reelTweens[reel] = DOVirtual.DelayedCall(delay, () =>
        {
            if (isSpinning)
            {
                StartReelCycle(reel);
            }
        });
    }

    private void StartReelCycle(int reel)
    {
        if (!isSpinning)
        {
            return;
        }

        Transform reelTransform = reelTransforms[reel];
        ResetReelPosition(reel);

        Tween cycleTween = reelTransform
            .DOLocalMoveY(restingY - symbolStep, GetCycleDuration())
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (!isSpinning)
                {
                    return;
                }

                CycleReel(reel, null);
                ResetReelPosition(reel);
                reelCycleCounts[reel]++;
                StartReelCycle(reel);
            });

        reelTweens[reel] = cycleTween;
    }

    private float GetCycleDuration()
    {
        float multiplier = activeSpinSpeed == SpinSpeed.Normal
            ? normalSpeedMultiplier
            : fastSpeedMultiplier;
        return Mathf.Max(0.01f, spinCycleDuration) / Mathf.Clamp(multiplier, 0.25f, 2f);
    }

    private void FillReelAroundResult(int reel)
    {
        List<int> buffer = reelBufferSymbols[reel];
        for (int image = 0; image < ImagesPerReel; image++)
        {
            buffer[image] = GetRandomSpinSymbol();
        }

        for (int row = 0; row < RowCount; row++)
        {
            buffer[FirstVisibleImageIndex + row] =
                NormalizeSymbolId(currentResult[GetResultIndex(row, reel)]);
        }
        RenderReel(reel, false);
    }

    /// <param name="forcedTopSymbol">
    /// Null selects a random strip value. A value of 0 explicitly inserts a blank.
    /// </param>
    private void CycleReel(int reel, int? forcedTopSymbol)
    {
        List<int> buffer = reelBufferSymbols[reel];
        for (int image = ImagesPerReel - 1; image > 0; image--)
        {
            buffer[image] = buffer[image - 1];
        }

        buffer[0] = forcedTopSymbol.HasValue
            ? NormalizeSymbolId(forcedTopSymbol.Value)
            : GetRandomSpinSymbol();
        RenderReel(reel, false);
    }

    private void RenderReel(int reel, bool stopped)
    {
        List<Image> images = reelImagesList[reel].images;
        List<int> buffer = reelBufferSymbols[reel];

        for (int image = 0; image < ImagesPerReel; image++)
        {
            int symbolId = NormalizeSymbolId(buffer[image]);
            bool isVisibleBufferPosition =
                !stopped ||
                (image >= FirstVisibleImageIndex && image <= LastVisibleImageIndex);
            SetImageSymbol(images[image], symbolId, isVisibleBufferPosition);
        }
    }

    private void ApplyStoppedReelVisibility(int reel)
    {
        RenderReel(reel, true);
    }

    private void SetImageSymbol(Image image, int symbolId, bool positionVisible)
    {
        // Unity UI children can be destroyed before this view receives its
        // teardown callbacks. Destroyed Unity objects compare equal to null,
        // so skip them instead of trying to restore a component that no
        // longer exists.
        if (image == null)
        {
            return;
        }

        bool hasSymbol = symbolId != EmptySymbolId;
        image.sprite = hasSymbol ? GetSymbolSprite(symbolId) : null;

        Color color = image.color;
        image.color = new Color(
            color.r,
            color.g,
            color.b,
            positionVisible && hasSymbol ? 1f : 0f);

        // Keep the Image enabled so a transparent blank still occupies its
        // normal VerticalLayoutGroup position.
        image.enabled = true;
    }

    private Sprite GetSymbolSprite(int symbolId)
    {
        switch (NormalizeSymbolId(symbolId))
        {
            case GreenWheelSymbolId:
                return spriteGreenWheelSymbol;
            case BlueWheelSymbolId:
                return spriteBlueWheelSymbol;
            case RedWheelSymbolId:
                return spriteRedWheelSymbol;
            default:
                return null;
        }
    }

    private static void SetImageAlpha(
        Image image,
        float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private void RestoreCurrentResultSprites()
    {
        if (currentResult.Count != ResultCellCount ||
            reelImagesList == null ||
            reelImagesList.Count != ReelCount)
        {
            return;
        }

        for (int row = 0; row < RowCount; row++)
        {
            for (int reel = 0; reel < ReelCount; reel++)
            {
                UltraReelImages reelImages = reelImagesList[reel];
                int imageIndex = FirstVisibleImageIndex + row;
                if (reelImages?.images == null ||
                    imageIndex < 0 ||
                    imageIndex >= reelImages.images.Count)
                {
                    continue;
                }

                int symbolId =
                    currentResult[GetResultIndex(row, reel)];
                Image baseImage =
                    reelImages.images[imageIndex];
                SetImageSymbol(
                    baseImage,
                    symbolId,
                    true);

                Image animationImage =
                    GetWinningAnimationImage(
                        reel,
                        imageIndex);
                if (animationImage != null)
                {
                    SetImageAlpha(animationImage, 0f);
                    animationImage.gameObject.SetActive(false);
                }

                Image winIndicatorImage =
                    GetWinIndicatorImage(baseImage);
                if (winIndicatorImage != null)
                {
                    SetImageAlpha(winIndicatorImage, 0f);
                    winIndicatorImage.gameObject.SetActive(false);
                }
            }
        }
    }

    private static string GetWheelColorName(int symbolId)
    {
        switch (symbolId)
        {
            case GreenWheelSymbolId:
                return "Green";
            case BlueWheelSymbolId:
                return "Blue";
            case RedWheelSymbolId:
                return "Red";
            default:
                return "Unknown";
        }
    }

    private int GetRandomSpinSymbol()
    {
        if (spinStrip == null || spinStrip.Length == 0)
        {
            return EmptySymbolId;
        }

        return NormalizeSymbolId(spinStrip[UnityEngine.Random.Range(0, spinStrip.Length)]);
    }

    private static int NormalizeSymbolId(int symbolId)
    {
        return symbolId <= EmptySymbolId ? EmptySymbolId : symbolId;
    }

    private static bool IsKnownSymbol(int symbolId)
    {
        return symbolId == EmptySymbolId ||
               symbolId == GreenWheelSymbolId ||
               symbolId == BlueWheelSymbolId ||
               symbolId == RedWheelSymbolId;
    }

    private void StoreCurrentResult(IList<int> result)
    {
        currentResult.Clear();
        for (int cell = 0; cell < ResultCellCount; cell++)
        {
            currentResult.Add(NormalizeSymbolId(result[cell]));
        }
    }

    public static List<int> CreateEmptyResult()
    {
        var result = new List<int>(ResultCellCount);
        for (int cell = 0; cell < ResultCellCount; cell++)
        {
            result.Add(EmptySymbolId);
        }

        return result;
    }

    public static int GetResultIndex(int row, int reel)
    {
        return row * ReelCount + reel;
    }

    private static List<int> GetResultColumn(IReadOnlyList<int> result, int reel)
    {
        var column = new List<int>(RowCount);
        for (int row = 0; row < RowCount; row++)
        {
            column.Add(NormalizeSymbolId(result[GetResultIndex(row, reel)]));
        }

        return column;
    }

    private void ResetReelPosition(int reel)
    {
        Transform reelTransform = reelTransforms[reel];
        reelTransform.localPosition = new Vector3(
            reelTransform.localPosition.x,
            restingY,
            reelTransform.localPosition.z);
    }

    private void KillReelTween(int reel)
    {
        if (reel < 0 || reel >= reelTweens.Count || reelTweens[reel] == null)
        {
            return;
        }

        reelTweens[reel].Kill();
        reelTweens[reel] = null;
    }

    private void KillReelTweens()
    {
        for (int reel = 0; reel < reelTweens.Count; reel++)
        {
            KillReelTween(reel);
        }
    }

    private void OnDisable()
    {
        CancelSpin();
    }

    private void OnDestroy()
    {
        KillReelTweens();
    }
}

[Serializable]
public class UltraReelImages
{
    public List<Image> images = new List<Image>(5);
    [Tooltip(
        "For the live centered server result, assign one center animation " +
        "Image. You can alternatively assign three Images in Top, Middle, " +
        "Bottom order.")]
    public List<Image> winAnimationImages =
        new List<Image>(3);
}
