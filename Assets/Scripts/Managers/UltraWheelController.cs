using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Spins an equal-segment prize wheel to an explicitly selected segment.
/// Segment indices increase clockwise around the wheel.
/// </summary>
public class UltraWheelController : MonoBehaviour
{
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

    [Header("Spin")]
    [SerializeField, Min(1)] private int completeRotations = 5;
    [SerializeField, Min(0.1f)] private float spinDuration = 5f;
    [SerializeField] private bool spinClockwise = true;
    [SerializeField] private Ease spinEase = Ease.OutQuint;

    [Header("Editor Test")]
    [SerializeField, Min(0)] private int testWinningIndex;

    public event Action<int> SpinCompleted;

    public bool IsSpinning { get; private set; }
    public int SelectedIndex { get; private set; } = -1;
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

        return SpinToIndex(physicalSegmentIndex, onComplete);
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

    private void OnValidate()
    {
        wheelNumber = Mathf.Clamp(wheelNumber, 1, 3);
        segmentCount = Mathf.Max(1, segmentCount);
        completeRotations = Mathf.Max(1, completeRotations);
        spinDuration = Mathf.Max(0.1f, spinDuration);
        testWinningIndex = Mathf.Clamp(testWinningIndex, 0, segmentCount - 1);
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
