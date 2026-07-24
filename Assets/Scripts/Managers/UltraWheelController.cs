using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Spins an equal-segment prize wheel to an explicitly selected segment.
/// Segment indices increase clockwise around the wheel.
/// </summary>
public class UltraWheelController : MonoBehaviour
{
    public const int ServerValueCount =
        StPatricksGoldDefinition.UltraWheelValueCount;

    [Header("References")]
    [Tooltip("The RectTransform that rotates. Its pivot should be centered at (0.5, 0.5).")]
    [SerializeField] private RectTransform wheelTransform;

    [Header("Wheel Identity")]
    [Tooltip("1 = Red, 2 = Blue, 3 = third Ultra wheel.")]
    [SerializeField, Range(1, 3)] private int wheelNumber = 1;

    [Header("Wheel Layout")]
    [SerializeField, Min(1)] private int segmentCount = 21;
    [Tooltip(
        "The local angle of segment 0's center when the wheel Z rotation is zero. " +
        "Positive Unity angles are counter-clockwise.")]
    [SerializeField] private float startingAngleOffset;
    [Tooltip("Small final wheel-rotation adjustment used to align segment centers with the pointer.")]
    [SerializeField] private float alignmentOffset;

    [Header("Server Stop Mapping")]
    [Tooltip(
        "Enable when the server sends one of 10 logical stop indices rather than a physical 0-20 segment.")]
    [SerializeField] private bool mapServerStopIndexToPhysicalSegment = true;
    [Tooltip(
        "One entry per physical segment, clockwise from segment 0. " +
        "Each value is the server stop index displayed by that segment.")]
    [SerializeField] private int[] segmentServerStopIndices =
    {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        0
    };

    [Header("Segment Value Texts")]
    [Tooltip(
        "Assign one existing TMP text per physical wheel segment, clockwise from segment 0. " +
        "The 10 server values are repeated across these 21 labels using Segment Server Stop Indices.")]
    [SerializeField] private TMP_Text[] segmentValueTexts = new TMP_Text[21];
    [Tooltip("Optional text shown before every server value, for example '$' or 'x'.")]
    [SerializeField] private string valuePrefix;
    [Tooltip("Optional text shown after every server value.")]
    [SerializeField] private string valueSuffix;

    [Header("Spin")]
    [SerializeField, Min(1)] private int completeRotations = 5;
    [Tooltip("Total spin time. A longer duration makes the final slowdown more noticeable.")]
    [SerializeField, Min(0.1f)] private float spinDuration = 5f;
    [SerializeField] private bool spinClockwise = true;
    [Tooltip("OutQuint produces a long, smooth slowdown before the exact server-selected stop.")]
    [SerializeField] private Ease spinEase = Ease.OutQuint;

    [Header("Editor Test")]
    [SerializeField, Min(0)] private int testWinningIndex;

    public event Action<int> SpinCompleted;

    public bool IsSpinning { get; private set; }
    public int SelectedIndex { get; private set; } = -1;
    public int SelectedServerStopIndex { get; private set; } = -1;
    public int WheelNumber => wheelNumber;
    public float SegmentAngle => 360f / Mathf.Max(1, segmentCount);

    private Tween spinTween;
    private int nextMatchingSegmentSearchStart;

    private void Awake()
    {
        if (wheelTransform == null)
        {
            wheelTransform = GetComponent<RectTransform>();
        }

        if (wheelTransform == null)
        {
            Debug.LogError("[UltraWheelController] Assign the wheel RectTransform.");
        }
    }

    /// <summary>
    /// Spins to the supplied result index. This method never selects or
    /// randomizes a winner; winningIndex completely determines the destination.
    /// </summary>
    public bool SpinToIndex(int winningIndex, Action onComplete = null)
    {
        if (wheelTransform == null)
        {
            Debug.LogError("[UltraWheelController] Cannot spin without a wheel RectTransform.");
            return false;
        }

        if (segmentCount <= 0)
        {
            Debug.LogError("[UltraWheelController] Segment Count must be greater than zero.");
            return false;
        }

        if (winningIndex < 0 || winningIndex >= segmentCount)
        {
            Debug.LogError(
                $"[UltraWheelController] Winning index {winningIndex} is outside 0-{segmentCount - 1}.");
            return false;
        }

        KillSpin();

        SelectedIndex = winningIndex;
        IsSpinning = true;

        float exactFinalAngle = CalculateTargetAngle(winningIndex);
        float currentAngle = wheelTransform.localEulerAngles.z;
        float animatedFinalAngle = CalculateAnimatedFinalAngle(currentAngle, exactFinalAngle);
        Vector3 currentEulerAngles = wheelTransform.localEulerAngles;

        spinTween = wheelTransform
            .DOLocalRotate(
                new Vector3(
                    currentEulerAngles.x,
                    currentEulerAngles.y,
                    animatedFinalAngle),
                Mathf.Max(0.1f, spinDuration),
                RotateMode.FastBeyond360)
            .SetEase(spinEase)
            .OnComplete(() =>
            {
                // Snap to the mathematically exact equivalent angle so small
                // tween/float errors cannot leave the pointer between segments.
                Vector3 snappedEulerAngles = wheelTransform.localEulerAngles;
                snappedEulerAngles.z = exactFinalAngle;
                wheelTransform.localEulerAngles = snappedEulerAngles;

                spinTween = null;
                IsSpinning = false;
                SpinCompleted?.Invoke(winningIndex);
                onComplete?.Invoke();
            });

        return true;
    }

    /// <summary>
    /// Resolves the server's logical stop index to a physical wheel segment,
    /// then performs the exact indexed spin.
    /// </summary>
    public bool SpinToServerStopIndex(int serverStopIndex, Action onComplete = null)
    {
        if (!TryResolvePhysicalSegment(serverStopIndex, out int physicalSegmentIndex))
        {
            return false;
        }

        bool started = SpinToIndex(physicalSegmentIndex, onComplete);
        if (started)
        {
            SelectedServerStopIndex = serverStopIndex;
        }

        return started;
    }

    /// <summary>
    /// Writes the server's 10 logical wheel values onto all 21 physical segments.
    /// Duplicate physical segments display the same value and use the same stop index.
    /// </summary>
    public bool SetServerValues(IReadOnlyList<int> serverValues)
    {
        if (serverValues == null || serverValues.Count != ServerValueCount)
        {
            Debug.LogError(
                $"[UltraWheelController] Wheel {wheelNumber} requires exactly " +
                $"{ServerValueCount} server values.");
            return false;
        }

        if (segmentServerStopIndices == null ||
            segmentServerStopIndices.Length != segmentCount)
        {
            Debug.LogError(
                $"[UltraWheelController] Wheel {wheelNumber} requires exactly {segmentCount} " +
                "Segment Server Stop Indices before its values can be displayed.");
            return false;
        }

        if (segmentValueTexts == null || segmentValueTexts.Length != segmentCount)
        {
            Debug.LogError(
                $"[UltraWheelController] Wheel {wheelNumber} requires exactly {segmentCount} " +
                "Segment Value Text assignments.");
            return false;
        }

        bool allTextsAssigned = true;
        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            int serverValueIndex = segmentServerStopIndices[segmentIndex];
            if (serverValueIndex < 0 || serverValueIndex >= serverValues.Count)
            {
                Debug.LogError(
                    $"[UltraWheelController] Wheel {wheelNumber} segment {segmentIndex} maps " +
                    $"to invalid server value index {serverValueIndex}.");
                return false;
            }

            TMP_Text valueText = segmentValueTexts[segmentIndex];
            if (valueText == null)
            {
                allTextsAssigned = false;
                continue;
            }

            valueText.text =
                FormatServerValue(serverValues[serverValueIndex]);
        }

        if (!allTextsAssigned)
        {
            Debug.LogWarning(
                $"[UltraWheelController] Wheel {wheelNumber} has unassigned Segment Value Texts. " +
                "Assigned labels were updated; empty Inspector slots were skipped.");
        }

        return allTextsAssigned;
    }

    /// <summary>
    /// Updates the physical segment copies for one logical server stop. This is
    /// used when the spin response supplies only the selected stop's base award.
    /// </summary>
    public bool SetServerValue(int serverValueIndex, double serverValue)
    {
        if (serverValueIndex < 0 || serverValueIndex >= ServerValueCount)
        {
            Debug.LogError(
                $"[UltraWheelController] Wheel {wheelNumber} received server value index " +
                $"{serverValueIndex}, expected 0-{ServerValueCount - 1}.");
            return false;
        }

        if (segmentServerStopIndices == null ||
            segmentServerStopIndices.Length != segmentCount ||
            segmentValueTexts == null ||
            segmentValueTexts.Length != segmentCount)
        {
            Debug.LogError(
                $"[UltraWheelController] Wheel {wheelNumber} requires exactly {segmentCount} " +
                "stop mappings and Segment Value Text assignments.");
            return false;
        }

        bool foundMappedSegment = false;
        bool allMappedTextsAssigned = true;
        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            if (segmentServerStopIndices[segmentIndex] != serverValueIndex)
            {
                continue;
            }

            foundMappedSegment = true;
            TMP_Text valueText = segmentValueTexts[segmentIndex];
            if (valueText == null)
            {
                allMappedTextsAssigned = false;
                continue;
            }

            valueText.text = FormatServerValue(serverValue);
        }

        if (!foundMappedSegment)
        {
            Debug.LogError(
                $"[UltraWheelController] Wheel {wheelNumber} has no physical segment mapped " +
                $"to server value index {serverValueIndex}.");
            return false;
        }

        return allMappedTextsAssigned;
    }

    public bool TryResolvePhysicalSegment(
        int serverStopIndex,
        out int physicalSegmentIndex)
    {
        physicalSegmentIndex = -1;

        if (!mapServerStopIndexToPhysicalSegment)
        {
            if (serverStopIndex < 0 || serverStopIndex >= segmentCount)
            {
                Debug.LogError(
                    $"[UltraWheelController] Wheel {wheelNumber} received physical stop index " +
                    $"{serverStopIndex}, expected 0-{segmentCount - 1}.");
                return false;
            }

            physicalSegmentIndex = serverStopIndex;
            return true;
        }

        if (segmentServerStopIndices == null ||
            segmentServerStopIndices.Length != segmentCount)
        {
            Debug.LogError(
                $"[UltraWheelController] Wheel {wheelNumber} requires exactly {segmentCount} " +
                "Segment Server Stop Indices.");
            return false;
        }

        for (int offset = 0; offset < segmentCount; offset++)
        {
            int segmentIndex =
                (nextMatchingSegmentSearchStart + offset) % segmentCount;
            if (segmentServerStopIndices[segmentIndex] != serverStopIndex)
            {
                continue;
            }

            physicalSegmentIndex = segmentIndex;
            nextMatchingSegmentSearchStart = (segmentIndex + 1) % segmentCount;
            return true;
        }

        Debug.LogError(
            $"[UltraWheelController] Wheel {wheelNumber} has no physical segment mapped " +
            $"to server stop index {serverStopIndex}.");
        return false;
    }

    /// <summary>
    /// Returns the normalized Z rotation that places the selected segment's
    /// center under the fixed pointer.
    /// </summary>
    public float CalculateTargetAngle(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= segmentCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(segmentIndex),
                segmentIndex,
                $"Segment index must be between 0 and {segmentCount - 1}.");
        }

        // Segment indices increase clockwise, so their local center angles
        // decrease in Unity's counter-clockwise-positive coordinate system.
        float segmentCenterLocalAngle =
            startingAngleOffset - segmentIndex * SegmentAngle;

        // Rotating by the inverse local angle brings that center to the fixed
        // pointer at zero degrees. Alignment Offset provides final fine-tuning.
        return NormalizeAngle(-segmentCenterLocalAngle + alignmentOffset);
    }

    public void SetOffsets(float startOffset, float fineAlignmentOffset)
    {
        startingAngleOffset = startOffset;
        alignmentOffset = fineAlignmentOffset;
    }

    public void KillSpin()
    {
        if (spinTween != null)
        {
            spinTween.Kill();
            spinTween = null;
        }

        IsSpinning = false;
    }

    [ContextMenu("Test Spin To Index")]
    private void TestSpinToIndex()
    {
        SpinToIndex(Mathf.Clamp(testWinningIndex, 0, Mathf.Max(0, segmentCount - 1)));
    }

    private float CalculateAnimatedFinalAngle(float currentAngle, float exactFinalAngle)
    {
        int rotations = Mathf.Max(1, completeRotations);
        if (spinClockwise)
        {
            float clockwiseDistance = Mathf.Repeat(currentAngle - exactFinalAngle, 360f);
            return currentAngle - rotations * 360f - clockwiseDistance;
        }

        float counterClockwiseDistance = Mathf.Repeat(exactFinalAngle - currentAngle, 360f);
        return currentAngle + rotations * 360f + counterClockwiseDistance;
    }

    private static float NormalizeAngle(float angle)
    {
        return Mathf.Repeat(angle, 360f);
    }

    private string FormatServerValue(double serverValue)
    {
        return valuePrefix +
               serverValue.ToString("0.##", CultureInfo.InvariantCulture) +
               valueSuffix;
    }

    private void OnValidate()
    {
        wheelNumber = Mathf.Clamp(wheelNumber, 1, 3);
        segmentCount = Mathf.Max(1, segmentCount);
        completeRotations = Mathf.Max(1, completeRotations);
        spinDuration = Mathf.Max(0.1f, spinDuration);
        testWinningIndex = Mathf.Clamp(testWinningIndex, 0, segmentCount - 1);

        if (segmentValueTexts == null || segmentValueTexts.Length != segmentCount)
        {
            Array.Resize(ref segmentValueTexts, segmentCount);
        }
    }

    private void OnDisable()
    {
        KillSpin();
    }

    private void OnDestroy()
    {
        KillSpin();
    }
}
