using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ScatterWheelPresentationManager : MonoBehaviour
{
    private const int ColumnCount = 5;
    private const int RowCount = 3;
    private const int SegmentCount = 8;
    private const int ConstantSpeedCompleteRotations = 1;
    private const int MinimumDecelerationRotations = 3;
    private const int AuthoredAccelerationFrameCount = 24;
    private const float AuthoredPeakDegreesPerFrame = 47f;
    private const float FinalFrameAlignmentDegrees = 14f;

    [Serializable]
    private sealed class ScatterWheelColumnReferences
    {
        [Tooltip("CanRotate belonging to the top visible symbol.")]
        public RectTransform topSymbol = null;
        [Tooltip("Text parent containing the top wheel's eight TMP labels.")]
        public RectTransform topTextParent = null;

        [Tooltip("CanRotate belonging to the middle visible symbol.")]
        public RectTransform middleSymbol = null;
        [Tooltip("Text parent containing the middle wheel's eight TMP labels.")]
        public RectTransform middleTextParent = null;

        [Tooltip("CanRotate belonging to the bottom visible symbol.")]
        public RectTransform bottomSymbol = null;
        [Tooltip("Text parent containing the bottom wheel's eight TMP labels.")]
        public RectTransform bottomTextParent = null;

        public RectTransform GetRow(int row)
        {
            switch (row)
            {
                case 0:
                    return topSymbol;
                case 1:
                    return middleSymbol;
                case 2:
                    return bottomSymbol;
                default:
                    return null;
            }
        }

        public RectTransform GetTextParent(int row)
        {
            switch (row)
            {
                case 0:
                    return topTextParent;
                case 1:
                    return middleTextParent;
                case 2:
                    return bottomTextParent;
                default:
                    return null;
            }
        }
    }

    [SerializeField, HideInInspector] private SlotView slotView;
    [SerializeField, HideInInspector]
    private SlotSymbolAnimationManager symbolAnimationManager;

    [Header("CanRotate Grid (5 Columns x 3 Symbols)")]
    [SerializeField]
    private ScatterWheelColumnReferences column1 =
        new ScatterWheelColumnReferences();
    [SerializeField]
    private ScatterWheelColumnReferences column2 =
        new ScatterWheelColumnReferences();
    [SerializeField]
    private ScatterWheelColumnReferences column3 =
        new ScatterWheelColumnReferences();
    [SerializeField]
    private ScatterWheelColumnReferences column4 =
        new ScatterWheelColumnReferences();
    [SerializeField]
    private ScatterWheelColumnReferences column5 =
        new ScatterWheelColumnReferences();

    private readonly List<ActiveScatterWheel> activeWheels =
        new List<ActiveScatterWheel>();
    private Coroutine presentationCoroutine;
    private Action presentationCompleted;

    private sealed class ActiveScatterWheel
    {
        public Image SymbolImage;
        public Image AnimationImage;
        public Image BackgroundFxImage;
        public RectTransform WheelTransform;
        public RectTransform WheelRimTransform;
        public RectTransform LeafTransform;
        public RectTransform WinIndicatorTransform;
        public RectTransform OutputTextTransform;
        public TMP_Text OutputTmpText;
        public Text OutputLegacyText;
        public RectTransform TextParent;
        public List<string> ServerValues;
        public string WinningAwardText;
        public int StopIndex;
        public Vector3 RestingEulerAngles;
        public Vector3 RestingWheelScale;
        public Vector3 RestingWheelRimScale;
        public Vector3 RestingLeafScale;
        public Vector2 RestingOutputTextPosition;
        public Tween SpinTween;
    }

    private void Awake()
    {
        if (slotView == null)
        {
            slotView = GetComponent<SlotView>();
        }

        if (symbolAnimationManager == null)
        {
            symbolAnimationManager =
                FindFirstObjectByType<SlotSymbolAnimationManager>(
                    FindObjectsInactive.Include);
        }

        HideAllWheels();
    }

    private void OnDisable()
    {
        CancelPresentation();
    }

    internal void Configure(
        SlotView view,
        SlotSymbolAnimationManager animationManager)
    {
        if (view != null)
        {
            slotView = view;
        }

        if (animationManager != null)
        {
            symbolAnimationManager = animationManager;
        }
    }

    internal bool ShowScatterWheelFeature(
        ServerScatterBonus scatterBonus,
        ScatterWheelConfig scatterWheelConfig,
        Action onComplete)
    {
        if (scatterBonus?.wheelSpins == null ||
            scatterBonus.wheelSpins.Count == 0)
        {
            Debug.LogWarning(
                "[ScatterWheel] The feature was triggered without any " +
                "wheelSpins results.");
            return false;
        }

        if (slotView == null || symbolAnimationManager == null)
        {
            Debug.LogError(
                "[ScatterWheel] SlotView and SlotSymbolAnimationManager " +
                "must both be assigned.");
            return false;
        }

        CancelPresentation();
        presentationCompleted = onComplete;
        presentationCoroutine = StartCoroutine(
            PlayScatterWheelFeature(
                scatterBonus,
                scatterWheelConfig));
        return true;
    }

    internal void CancelPresentation()
    {
        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
            presentationCoroutine = null;
        }

        presentationCompleted = null;
        ResetActiveWheels();
    }

    internal void HideColumn(int column)
    {
        for (int row = 0; row < RowCount; row++)
        {
            HideWheel(column, row);
        }
    }

    internal void HideWheel(int column, int row)
    {
        RectTransform wheel = GetCanRotate(column, row);
        if (wheel != null)
        {
            wheel.gameObject.SetActive(false);
            SetActive(FindSibling(wheel, "WheelRim"), false);
            SetActive(FindSibling(wheel, "Leaf"), false);
            SetActive(FindSibling(wheel, "Win"), false);
            SetActive(FindOutputText(wheel), false);
        }
    }

    internal void HideAllWheels()
    {
        for (int column = 0; column < ColumnCount; column++)
        {
            HideColumn(column);
        }
    }

    private IEnumerator PlayScatterWheelFeature(
        ServerScatterBonus scatterBonus,
        ScatterWheelConfig scatterWheelConfig)
    {
        // Allow StartCoroutine to assign its handle before any malformed
        // result can complete the presentation.
        yield return null;

        List<ServerGridPosition> displayedPositions =
            GetDisplayedScatterWheelPositions();
        float animationDuration =
            symbolAnimationManager.GetScatterWheelIntroDuration();
        int animationFrameCount = Mathf.Max(
            1,
            symbolAnimationManager.GetScatterWheelMainFrameCount());
        float matchedPeakSpeed =
            AuthoredPeakDegreesPerFrame *
            animationFrameCount /
            Mathf.Max(0.01f, animationDuration);
        float matchedAccelerationDuration =
            animationDuration *
            AuthoredAccelerationFrameCount /
            animationFrameCount;
        int completedIntroCount = 0;
        int completedSpinCount = 0;
        int completedBackgroundFxCount = 0;
        int completedOutputTextCount = 0;

        for (int spinIndex = 0;
             spinIndex < scatterBonus.wheelSpins.Count;
             spinIndex++)
        {
            ServerScatterWheelSpin wheelSpin =
                scatterBonus.wheelSpins[spinIndex];
            if (wheelSpin == null ||
                !TryResolvePosition(
                    wheelSpin,
                    spinIndex,
                    scatterBonus.triggerPositions,
                    displayedPositions,
                    out int row,
                    out int column))
            {
                Debug.LogWarning(
                    $"[ScatterWheel] wheelSpins[{spinIndex}] has no valid " +
                    "symbol position.");
                continue;
            }

            if (wheelSpin.stopIndex < 0 ||
                wheelSpin.stopIndex >= SegmentCount)
            {
                Debug.LogError(
                    $"[ScatterWheel] Row {row}, column {column} requires a " +
                    $"server stopIndex from 0 to {SegmentCount - 1}.");
                continue;
            }

            if (!slotView.TryGetScatterWheelSymbolImages(
                    column,
                    row,
                    out Image symbolImage,
                    out Image animationImage,
                    out Image backgroundFxImage))
            {
                Debug.LogError(
                    $"[ScatterWheel] No Scatter symbol is displayed at row " +
                    $"{row}, column {column}.");
                continue;
            }

            RectTransform canRotate = GetCanRotate(column, row);
            RectTransform wheelRim =
                FindSibling(canRotate, "WheelRim");
            RectTransform leaf =
                FindSibling(canRotate, "Leaf");
            RectTransform winIndicator =
                FindSibling(canRotate, "Win");
            RectTransform outputText =
                FindOutputText(canRotate);
            TMP_Text outputTmpText =
                outputText != null
                    ? outputText.GetComponentInChildren<TMP_Text>(true)
                    : null;
            Text outputLegacyText =
                outputText != null
                    ? outputText.GetComponentInChildren<Text>(true)
                    : null;
            if (wheelRim == null)
            {
                Debug.LogWarning(
                    $"[ScatterWheel] Row {row}, column {column} has no " +
                    "direct sibling named WheelRim beside CanRotate.");
            }
            if (leaf == null)
            {
                Debug.LogWarning(
                    $"[ScatterWheel] Row {row}, column {column} has no " +
                    "direct sibling named Leaf beside CanRotate.");
            }
            if (outputText == null)
            {
                Debug.LogWarning(
                    $"[ScatterWheel] Row {row}, column {column} has no " +
                    "direct result text named OutputText beside CanRotate.");
            }
            else if (outputTmpText == null &&
                     outputLegacyText == null)
            {
                Debug.LogWarning(
                    $"[ScatterWheel] Row {row}, column {column} OutputText " +
                    "has no TMP_Text or UI Text component.");
            }
            RectTransform textParent =
                GetTextParent(column, row);
            if (canRotate == null || textParent == null)
            {
                Debug.LogError(
                    $"[ScatterWheel] Assign Column {column + 1}, " +
                    $"{GetRowName(row)} CanRotate and Text Parent in the " +
                    "Inspector.");
                continue;
            }

            List<string> serverValues = ResolveWheelValues(
                wheelSpin,
                scatterWheelConfig);
            var presentation = new ActiveScatterWheel
            {
                SymbolImage = symbolImage,
                AnimationImage = animationImage,
                BackgroundFxImage = backgroundFxImage,
                WheelTransform = canRotate,
                WheelRimTransform = wheelRim,
                LeafTransform = leaf,
                WinIndicatorTransform = winIndicator,
                OutputTextTransform = outputText,
                OutputTmpText = outputTmpText,
                OutputLegacyText = outputLegacyText,
                TextParent = textParent,
                ServerValues = serverValues,
                WinningAwardText =
                    wheelSpin.awardValue.ToString(
                        "0.##",
                        CultureInfo.InvariantCulture),
                StopIndex = ResolveWinningStopIndex(wheelSpin),
                RestingEulerAngles = canRotate.localEulerAngles,
                RestingWheelScale = canRotate.localScale,
                RestingWheelRimScale =
                    wheelRim != null
                        ? wheelRim.localScale
                        : Vector3.one,
                RestingLeafScale =
                    leaf != null
                        ? leaf.localScale
                        : Vector3.one,
                RestingOutputTextPosition =
                    outputText != null
                        ? outputText.anchoredPosition
                        : Vector2.zero
            };
            activeWheels.Add(presentation);

            canRotate.gameObject.SetActive(false);
            SetActive(wheelRim, false);
            SetActive(leaf, false);
            SetActive(winIndicator, false);
            SetActive(outputText, false);
            canRotate.localEulerAngles =
                presentation.RestingEulerAngles;
            symbolImage.enabled = true;

            ActiveScatterWheel capturedPresentation =
                presentation;
            bool animationStarted =
                symbolAnimationManager.PlayScatterWheelIntro(
                    symbolImage,
                    animationImage,
                    backgroundFxImage,
                    () =>
                    {
                        if (!activeWheels.Contains(
                                capturedPresentation))
                        {
                            return;
                        }

                        // The Main Wheel overlay is now hidden. The Background
                        // FX continues at its own slower playback speed while
                        // the authored wheel starts rotating.
                        completedIntroCount++;
                        StartContinuedWheelSpin(
                            capturedPresentation,
                            capturedPresentation.StopIndex,
                            matchedPeakSpeed,
                            matchedAccelerationDuration,
                            () =>
                            {
                                completedSpinCount++;
                                ShowAndMoveOutputText(
                                    capturedPresentation,
                                    () =>
                                        completedOutputTextCount++);
                            });
                    },
                    () => completedBackgroundFxCount++);

            if (!animationStarted)
            {
                activeWheels.Remove(presentation);
                ResetWheel(presentation);
            }
            else
            {
                // Match normal and Ultra winning symbols: the Win frame is
                // visible from the first frame of the Scatter animation.
                SetActive(
                    presentation.WinIndicatorTransform,
                    true);
            }
        }

        if (activeWheels.Count == 0)
        {
            CompletePresentation();
            yield break;
        }

        while (completedIntroCount < activeWheels.Count)
        {
            yield return null;
        }

        while (completedSpinCount < activeWheels.Count)
        {
            yield return null;
        }

        while (completedOutputTextCount < activeWheels.Count)
        {
            yield return null;
        }

        // A slow Background FX pass may outlast the wheel rotation. Keep the
        // presentation alive until every one-shot FX sequence has completed.
        while (completedBackgroundFxCount < activeWheels.Count)
        {
            yield return null;
        }

        CompletePresentation();
    }

    private void StartContinuedWheelSpin(
        ActiveScatterWheel presentation,
        int stopIndex,
        float matchedPeakDegreesPerSecond,
        float accelerationDuration,
        Action onComplete)
    {
        if (presentation?.WheelTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        RectTransform wheelTransform = presentation.WheelTransform;
        Vector3 handoffEulerAngles =
            presentation.RestingEulerAngles;
        handoffEulerAngles.z = Mathf.Repeat(
            presentation.RestingEulerAngles.z +
            FinalFrameAlignmentDegrees,
            360f);
        wheelTransform.localEulerAngles = handoffEulerAngles;

        // Handoff order: the main animation is hidden, server text is
        // assigned, then CanRotate, WheelRim, and Leaf smoothly enlarge
        // together while only CanRotate starts rotating.
        ApplyWheelTexts(
            wheelTransform,
            presentation.TextParent,
            presentation.ServerValues);

        if (presentation.SymbolImage != null)
        {
            presentation.SymbolImage.enabled = false;
        }

        symbolAnimationManager.PlayScatterWheelHandoff(
            wheelTransform,
            presentation.RestingWheelScale,
            presentation.WheelRimTransform,
            presentation.RestingWheelRimScale,
            presentation.LeafTransform,
            presentation.RestingLeafScale);

        float segmentAngle = 360f / SegmentCount;
        float exactFinalAngle = Mathf.Repeat(
            presentation.RestingEulerAngles.z +
            stopIndex * segmentAngle,
            360f);
        float startAngle = handoffEulerAngles.z;
        float peakSpeed =
            Mathf.Max(1f, matchedPeakDegreesPerSecond);
        float accelerationTime =
            Mathf.Max(0.05f, accelerationDuration);

        // The 85-frame artwork has already eased to zero angular velocity by
        // frame 80. Restarting at full speed causes the visible jerk that was
        // happening at the handoff. Ease.InSine reverses the authored
        // slowdown and reaches the measured 47-degrees-per-frame peak without
        // a velocity jump.
        float accelerationDistance =
            peakSpeed *
            accelerationTime *
            2f /
            Mathf.PI;
        float constantDistance =
            ConstantSpeedCompleteRotations * 360f;
        float constantTime =
            constantDistance / peakSpeed;
        float angleAfterConstantSpeed =
            startAngle -
            accelerationDistance -
            constantDistance;
        float decelerationDistance =
            Mathf.Repeat(
                angleAfterConstantSpeed - exactFinalAngle,
                360f) +
            MinimumDecelerationRotations * 360f;

        // Ease.OutSine begins at the same peak velocity and ends at zero.
        float decelerationTime =
            decelerationDistance *
            Mathf.PI /
            (2f * peakSpeed);
        float totalDuration =
            accelerationTime +
            constantTime +
            decelerationTime;

        presentation.SpinTween = DOVirtual
            .Float(
                0f,
                totalDuration,
                totalDuration,
                elapsed =>
                {
                    if (presentation.WheelTransform == null)
                    {
                        return;
                    }

                    float travelledDistance;
                    if (elapsed < accelerationTime)
                    {
                        float normalizedTime =
                            elapsed / accelerationTime;
                        travelledDistance =
                            accelerationDistance *
                            (1f -
                             Mathf.Cos(
                                 normalizedTime *
                                 Mathf.PI *
                                 0.5f));
                    }
                    else if (elapsed <
                             accelerationTime + constantTime)
                    {
                        float constantElapsed =
                            elapsed - accelerationTime;
                        travelledDistance =
                            accelerationDistance +
                            peakSpeed * constantElapsed;
                    }
                    else
                    {
                        float decelerationElapsed =
                            elapsed -
                            accelerationTime -
                            constantTime;
                        float normalizedTime =
                            Mathf.Clamp01(
                                decelerationElapsed /
                                Mathf.Max(
                                    0.01f,
                                    decelerationTime));
                        travelledDistance =
                            accelerationDistance +
                            constantDistance +
                            decelerationDistance *
                            Mathf.Sin(
                                normalizedTime *
                                Mathf.PI *
                                0.5f);
                    }

                    Vector3 currentEulerAngles =
                        presentation.RestingEulerAngles;
                    currentEulerAngles.z =
                        startAngle - travelledDistance;
                    presentation.WheelTransform.localEulerAngles =
                        currentEulerAngles;
                })
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (presentation.WheelTransform != null)
                {
                    Vector3 snappedEulerAngles =
                        presentation.RestingEulerAngles;
                    snappedEulerAngles.z = exactFinalAngle;
                    presentation.WheelTransform.localEulerAngles =
                        snappedEulerAngles;
                }

                presentation.SpinTween = null;
                onComplete?.Invoke();
            });
    }

    private List<ServerGridPosition>
        GetDisplayedScatterWheelPositions()
    {
        var positions = new List<ServerGridPosition>();
        for (int column = 0; column < ColumnCount; column++)
        {
            for (int row = 0; row < RowCount; row++)
            {
                if (slotView.IsScatterWheelAt(column, row))
                {
                    positions.Add(
                        new ServerGridPosition
                        {
                            row = row,
                            col = column
                        });
                }
            }
        }

        return positions;
    }

    private bool TryResolvePosition(
        ServerScatterWheelSpin wheelSpin,
        int spinIndex,
        List<ServerGridPosition> triggerPositions,
        List<ServerGridPosition> displayedPositions,
        out int row,
        out int column)
    {
        row = 0;
        column = 0;

        if (wheelSpin.hasPosition)
        {
            row = wheelSpin.row;
            column = wheelSpin.col;
        }
        else if (triggerPositions != null &&
                 spinIndex < triggerPositions.Count &&
                 triggerPositions[spinIndex] != null)
        {
            row = triggerPositions[spinIndex].row;
            column = triggerPositions[spinIndex].col;
        }
        else if (displayedPositions != null &&
                 spinIndex < displayedPositions.Count)
        {
            row = displayedPositions[spinIndex].row;
            column = displayedPositions[spinIndex].col;
        }
        else
        {
            return false;
        }

        return column >= 0 &&
               column < ColumnCount &&
               row >= 0 &&
               row < RowCount &&
               slotView.IsScatterWheelAt(column, row);
    }

    private List<string> ResolveWheelValues(
        ServerScatterWheelSpin wheelSpin,
        ScatterWheelConfig scatterWheelConfig)
    {
        if (wheelSpin?.values != null &&
            wheelSpin.values.Count == SegmentCount)
        {
            return new List<string>(wheelSpin.values);
        }

        if (wheelSpin?.awards != null &&
            wheelSpin.awards.Count == SegmentCount)
        {
            var values = new List<string>(SegmentCount);
            for (int valueIndex = 0;
                 valueIndex < wheelSpin.awards.Count;
                 valueIndex++)
            {
                values.Add(
                    wheelSpin.awards[valueIndex].ToString(
                        "0.##",
                        CultureInfo.InvariantCulture));
            }

            return values;
        }

        if (scatterWheelConfig?.wheels != null)
        {
            for (int tableIndex = 0;
                 tableIndex < scatterWheelConfig.wheels.Count;
                 tableIndex++)
            {
                ScatterWheelAwardTable table =
                    scatterWheelConfig.wheels[tableIndex];
                if (table == null ||
                    table.wheelId != wheelSpin.wheelIndex + 1 ||
                    table.awards == null ||
                    table.awards.Count != SegmentCount)
                {
                    continue;
                }

                var values =
                    new List<string>(table.awards.Count);
                for (int valueIndex = 0;
                     valueIndex < table.awards.Count;
                     valueIndex++)
                {
                    values.Add(
                        table.awards[valueIndex].ToString(
                            CultureInfo.InvariantCulture));
                }

                return values;
            }
        }

        Debug.LogError(
            $"[ScatterWheel] Wheel {wheelSpin?.wheelIndex ?? 0} requires " +
            $"{SegmentCount} server awards. The result currently supplies " +
            "only awardValue, which is not enough to fill the wheel.");
        return new List<string>();
    }

    private int ResolveWinningStopIndex(
        ServerScatterWheelSpin wheelSpin)
    {
        if (wheelSpin == null)
        {
            return 0;
        }

        if (wheelSpin.stopIndex >= 0 &&
            wheelSpin.stopIndex < SegmentCount)
        {
            return wheelSpin.stopIndex;
        }

        Debug.LogError(
            $"[ScatterWheel] Wheel {wheelSpin.wheelIndex} received stopIndex " +
            $"{wheelSpin.stopIndex}, expected 0-{SegmentCount - 1}.");
        return Mathf.Clamp(wheelSpin.stopIndex, 0, SegmentCount - 1);
    }

    private void ShowAndMoveOutputText(
        ActiveScatterWheel presentation,
        Action onComplete)
    {
        if (presentation?.OutputTextTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        RectTransform outputText =
            presentation.OutputTextTransform;
        outputText.anchoredPosition =
            presentation.RestingOutputTextPosition;
        if (presentation.OutputTmpText != null)
        {
            presentation.OutputTmpText.text =
                presentation.WinningAwardText;
        }
        if (presentation.OutputLegacyText != null)
        {
            presentation.OutputLegacyText.text =
                presentation.WinningAwardText;
        }

        SetActive(outputText, true);
        if (presentation.LeafTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        bool moveStarted =
            symbolAnimationManager != null &&
            symbolAnimationManager.PlayScatterWheelResultTextMove(
                outputText,
                presentation.LeafTransform.anchoredPosition.y,
                onComplete);
        if (!moveStarted)
        {
            onComplete?.Invoke();
        }
    }

    private void ApplyWheelTexts(
        RectTransform wheelTransform,
        RectTransform textParent,
        IReadOnlyList<string> serverValues)
    {
        if (textParent == null)
        {
            Debug.LogError(
                "[ScatterWheel] The assigned Text Parent is missing.");
            return;
        }

        textParent.gameObject.SetActive(true);
        TMP_Text[] textComponents =
            textParent.GetComponentsInChildren<TMP_Text>(true);
        var orderedTexts =
            new List<TMP_Text>(textComponents);
        orderedTexts.Sort(
            (left, right) =>
                GetClockwiseTextAngle(wheelTransform, left)
                    .CompareTo(
                        GetClockwiseTextAngle(
                            wheelTransform,
                            right)));

        for (int index = 0; index < orderedTexts.Count; index++)
        {
            if (orderedTexts[index] != null)
            {
                orderedTexts[index].gameObject.SetActive(true);
                orderedTexts[index].enabled = true;
                Color textColor = orderedTexts[index].color;
                textColor.a = 1f;
                orderedTexts[index].color = textColor;
                orderedTexts[index].text = string.Empty;
            }
        }

        int valueCount = serverValues?.Count ?? 0;
        int assignmentCount = Mathf.Min(
            SegmentCount,
            Mathf.Min(orderedTexts.Count, valueCount));
        for (int index = 0;
             index < assignmentCount;
             index++)
        {
            orderedTexts[index].text =
                serverValues[index] ?? string.Empty;
        }

        if (orderedTexts.Count != SegmentCount ||
            valueCount != SegmentCount)
        {
            Debug.LogWarning(
                $"[ScatterWheel] Expected {SegmentCount} TMP labels and " +
                $"server values, but found {orderedTexts.Count} labels and " +
                $"{valueCount} values.");
        }
    }

    private static float GetClockwiseTextAngle(
        RectTransform wheelTransform,
        TMP_Text text)
    {
        if (wheelTransform == null || text == null)
        {
            return 0f;
        }

        Vector3 localPosition =
            wheelTransform.InverseTransformPoint(
                text.rectTransform.position);
        return Mathf.Repeat(
            Mathf.Atan2(localPosition.x, localPosition.y) *
            Mathf.Rad2Deg,
            360f);
    }

    private RectTransform GetCanRotate(int column, int row)
    {
        ScatterWheelColumnReferences columnReferences =
            GetColumn(column);
        return columnReferences?.GetRow(row);
    }

    private RectTransform GetTextParent(int column, int row)
    {
        ScatterWheelColumnReferences columnReferences =
            GetColumn(column);
        return columnReferences?.GetTextParent(row);
    }

    private static RectTransform FindSibling(
        RectTransform source,
        string siblingName)
    {
        if (source == null ||
            source.parent == null ||
            string.IsNullOrWhiteSpace(siblingName))
        {
            return null;
        }

        return source.parent.Find(siblingName) as RectTransform;
    }

    private static RectTransform FindOutputText(
        RectTransform source)
    {
        if (source == null || source.parent == null)
        {
            return null;
        }

        Transform parent = source.parent;
        for (int childIndex = 0;
             childIndex < parent.childCount;
             childIndex++)
        {
            Transform child = parent.GetChild(childIndex);
            if (child == null)
            {
                continue;
            }

            string normalizedName =
                child.name
                    .Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Trim()
                    .ToLowerInvariant();
            if (normalizedName == "outputtext" ||
                normalizedName == "resulttext" ||
                normalizedName == "awardtext" ||
                normalizedName == "output")
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    private static void SetActive(
        RectTransform target,
        bool isActive)
    {
        if (target != null)
        {
            target.gameObject.SetActive(isActive);
        }
    }

    private static void RestoreTransform(
        RectTransform target,
        Vector3 scale)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = scale;
        target.gameObject.SetActive(false);
    }

    private ScatterWheelColumnReferences GetColumn(int column)
    {
        switch (column)
        {
            case 0:
                return column1;
            case 1:
                return column2;
            case 2:
                return column3;
            case 3:
                return column4;
            case 4:
                return column5;
            default:
                return null;
        }
    }

    private static string GetRowName(int row)
    {
        switch (row)
        {
            case 0:
                return "Top";
            case 1:
                return "Middle";
            case 2:
                return "Bottom";
            default:
                return $"Row {row}";
        }
    }

    private void CompletePresentation()
    {
        presentationCoroutine = null;
        Action onComplete = presentationCompleted;
        presentationCompleted = null;
        onComplete?.Invoke();
    }

    private void ResetActiveWheels()
    {
        for (int index = 0;
             index < activeWheels.Count;
             index++)
        {
            ResetWheel(activeWheels[index]);
        }

        activeWheels.Clear();
    }

    private void ResetWheel(ActiveScatterWheel presentation)
    {
        if (presentation == null)
        {
            return;
        }

        if (presentation.SpinTween != null)
        {
            presentation.SpinTween.Kill();
            presentation.SpinTween = null;
        }

        if (presentation.WheelTransform != null)
        {
            if (symbolAnimationManager != null)
            {
                symbolAnimationManager.StopScatterWheelHandoff(
                    presentation.WheelTransform);
            }

            presentation.WheelTransform.localEulerAngles =
                presentation.RestingEulerAngles;
            RestoreTransform(
                presentation.WheelTransform,
                presentation.RestingWheelScale);
        }
        RestoreTransform(
            presentation.WheelRimTransform,
            presentation.RestingWheelRimScale);
        RestoreTransform(
            presentation.LeafTransform,
            presentation.RestingLeafScale);
        SetActive(
            presentation.WinIndicatorTransform,
            false);
        if (presentation.OutputTextTransform != null)
        {
            if (symbolAnimationManager != null)
            {
                symbolAnimationManager.StopScatterWheelResultTextMove(
                    presentation.OutputTextTransform);
            }

            presentation.OutputTextTransform.anchoredPosition =
                presentation.RestingOutputTextPosition;
            SetActive(
                presentation.OutputTextTransform,
                false);
        }

        if (presentation.SymbolImage != null)
        {
            presentation.SymbolImage.enabled = true;
        }

        if (symbolAnimationManager != null)
        {
            symbolAnimationManager.StopScatterWheelIntro(
                presentation.SymbolImage,
                presentation.AnimationImage,
                presentation.BackgroundFxImage);
        }
    }
}
