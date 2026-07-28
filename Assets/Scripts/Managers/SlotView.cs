using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SlotSymbolAnimationManager symbolAnimationManager;
    [Tooltip("Provides the configured 2x, 3x, 4x, and 5x Wild multiplier icons.")]
    [SerializeField] private UIManager uiManager;

    [Header("St. Patrick's Gold Symbol Sprites")]
    [SerializeField] private Sprite spriteAce;                // ID: 0
    [SerializeField] private Sprite spriteKing;               // ID: 1
    [SerializeField] private Sprite spriteQueen;              // ID: 2
    [SerializeField] private Sprite spriteJack;               // ID: 3
    [SerializeField] private Sprite spriteTen;                // ID: 4
    [SerializeField] private Sprite spriteBeerGlass;          // ID: 5
    [SerializeField] private Sprite spriteGreenHat;           // ID: 6
    [SerializeField] private Sprite spriteMagnet;             // ID: 7
    [SerializeField] private Sprite spriteCigar;              // ID: 8
    [SerializeField] private Sprite spriteWild;               // ID: 9
    [SerializeField] private Sprite spriteScatterWheel;       // ID: 10
    [SerializeField] private Sprite spriteUltraWheel;         // ID: 11
    [SerializeField] private Sprite spriteTempleRiches;       // ID: 12

    // Internal array built from named sprites
    private Sprite[] symbolSprites;

    [Header("Reel Containers")]
    [SerializeField] private Transform[] reelTransforms;

    [Header("Reel Images - 7 images per reel")]
    [SerializeField] private List<ReelImages> reelImagesList;

    [Header("Spin Settings")]
    [Tooltip("Height of one symbol used only to calculate reel movement. Vertical Layout Group spacing is added automatically; symbol sizes are never changed.")]
    [SerializeField] private float symbolHeight = 205f;
    [Tooltip("Base duration in seconds for a reel to move by one symbol height.")]
    [SerializeField] private float spinSpeed = 0.05f;
    [Tooltip("Multiplier for the visible reel travel speed. A value of 1 matches the Wild West spin timing.")]
    [SerializeField, Range(0.25f, 1f)] private float reelSpeedMultiplier = 0.55f;
    [Tooltip("Visible reel travel multiplier used while Fast or Skip Spin is selected.")]
    [UnityEngine.Serialization.FormerlySerializedAs("quickSpinReelSpeedMultiplier")]
    [SerializeField, Range(0.25f, 2f)] private float fastSpinReelSpeedMultiplier = 1f;
    [Tooltip("Delay between each reel beginning its start animation.")]
    [SerializeField] private float reelStartStagger = 0.08f;
    [SerializeField] private float reelStopStagger = 0.12f;

    [Header("Stop Animation Settings")]
    [Tooltip("Duration multiplier for the final decelerating symbol cycle. A value of 3 preserves the incoming speed with OutCubic easing.")]
    [SerializeField, Range(2f, 5f)] private float finalStopDurationMultiplier = 3f;
    [SerializeField] private float stopBounceDistance = 10f;
    [SerializeField] private float stopBounceReturnDuration = 0.16f;

    [Header("Quick Spin Settings")]
    [SerializeField] private float quickStopStagger = 0.06f;
    [SerializeField] private float quickStopDuration = 0.2f;
    [SerializeField] private int minSpinCyclesBeforeStop = 3;

    [Header("Win Animation Settings")]
    [Tooltip("Duration of one complete symbol loop at normal 1x playback speed.")]
    [SerializeField] private float winSymbolLoopDuration = 1.6f;
    [Tooltip("Playback-speed multiplier applied to every winning symbol except Wild.")]
    [SerializeField, Min(0.01f)] private float nonWildWinAnimationSpeedMultiplier = 1f;
    [Tooltip("Playback-speed multiplier applied to Wild winning symbols.")]
    [SerializeField, Min(0.01f)] private float wildWinAnimationSpeedMultiplier = 1f;
    [Tooltip("The next win stage starts after this many complete Wild animation loops.")]
    [UnityEngine.Serialization.FormerlySerializedAs("winSymbolCyclesPerStage")]
    [SerializeField, Min(1)] private int wildWinLoopsBeforeNextStage = 2;

    private List<Tween> spinTweens = new List<Tween>();
    private List<int> reelCycleCount = new List<int>();
    private Coroutine winAnimationCoroutine;
    private Coroutine stopSpinCoroutine;
    private VerticalLayoutGroup[] reelLayoutGroups;
    private Vector3[] reelRestingLocalPositions;
    private readonly Dictionary<Image, GameObject> winIndicators =
        new Dictionary<Image, GameObject>();
    private readonly Dictionary<Image, Image> wildMultiplierIndicators =
        new Dictionary<Image, Image>();
    private readonly Dictionary<Image, Coroutine> wildMultiplierAnimations =
        new Dictionary<Image, Coroutine>();
    private readonly Dictionary<Image, WildMultiplierTransformState>
        wildMultiplierTransformStates =
            new Dictionary<Image, WildMultiplierTransformState>();

    internal event System.Action<int, double> WinLineAmountPresentationChanged;

    private sealed class WinningLinePresentation
    {
        public HashSet<int> Positions;
        public int DisplayRow;
        public double WinAmount;
        public int WildMultiplier;
        public List<WildDetail> WildDetails;
    }

    private sealed class WildMultiplierTransformState
    {
        public Vector2 AnchoredPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }


    internal List<List<int>> currentDisplayMatrix;

    private bool isSpinning;
    private SpinSpeed activeSpinSpeed = SpinSpeed.Normal;

    #region Initialization

    private void Start()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>(
                FindObjectsInactive.Include);
        }

        BuildSymbolSpriteArray();
        InitializeReels();
    }

    private void BuildSymbolSpriteArray()
    {
        symbolSprites = new Sprite[StPatricksGoldDefinition.SymbolCount];
        symbolSprites[StPatricksGoldSymbolIds.Ace] = spriteAce;
        symbolSprites[StPatricksGoldSymbolIds.King] = spriteKing;
        symbolSprites[StPatricksGoldSymbolIds.Queen] = spriteQueen;
        symbolSprites[StPatricksGoldSymbolIds.Jack] = spriteJack;
        symbolSprites[StPatricksGoldSymbolIds.Ten] = spriteTen;
        symbolSprites[StPatricksGoldSymbolIds.BeerGlass] = spriteBeerGlass;
        symbolSprites[StPatricksGoldSymbolIds.GreenHat] = spriteGreenHat;
        symbolSprites[StPatricksGoldSymbolIds.Magnet] = spriteMagnet;
        symbolSprites[StPatricksGoldSymbolIds.Cigar] = spriteCigar;
        symbolSprites[StPatricksGoldSymbolIds.Wild] = spriteWild;
        symbolSprites[StPatricksGoldSymbolIds.ScatterWheel] = spriteScatterWheel;
        symbolSprites[StPatricksGoldSymbolIds.UltraWheel] = spriteUltraWheel;
        symbolSprites[StPatricksGoldSymbolIds.TempleRiches] = spriteTempleRiches;

        // Validate
        for (int i = 0; i < symbolSprites.Length; i++)
        {
            if (symbolSprites[i] == null)
            {
                Debug.LogError($"[SlotView] SL-SPG sprite for ID {i} ({StPatricksGoldSymbolIds.GetName(i)}) is not assigned in the Inspector.");
            }
        }
    }

    private void InitializeReels()
    {
        currentDisplayMatrix = new List<List<int>>();
        int defaultCols = StPatricksGoldDefinition.ReelCount;
        int defaultRows = StPatricksGoldDefinition.RowCount;
        reelCycleCount.Clear();
        for (int col = 0; col < defaultCols; col++)
        {
            List<int> column = new List<int>();
            for (int r = 0; r < defaultRows; r++)
            {
                column.Add(0);
            }
            currentDisplayMatrix.Add(column);
            reelCycleCount.Add(0);
        }

        // Cache VerticalLayoutGroup references from reel containers
        reelLayoutGroups = new VerticalLayoutGroup[reelTransforms.Length];
        reelRestingLocalPositions = new Vector3[reelTransforms.Length];
        for (int i = 0; i < reelTransforms.Length; i++)
        {
            if (reelTransforms[i] != null)
            {
                reelLayoutGroups[i] = reelTransforms[i].GetComponent<VerticalLayoutGroup>();
                reelRestingLocalPositions[i] = reelTransforms[i].localPosition;
            }
        }

        HideWinAnimationImages();
        HideWinIndicators();
        HideWildMultiplierIndicators();
    }

    private void HideWinAnimationImages()
    {
        if (reelImagesList == null)
        {
            return;
        }

        for (int column = 0; column < reelImagesList.Count; column++)
        {
            List<Image> animationImages = reelImagesList[column]?.winAnimationImages;
            if (animationImages == null)
            {
                continue;
            }

            for (int imageIndex = 0; imageIndex < animationImages.Count; imageIndex++)
            {
                Image animationImage = animationImages[imageIndex];
                if (animationImage == null)
                {
                    continue;
                }

                Color color = animationImage.color;
                animationImage.color = new Color(color.r, color.g, color.b, 0f);
            }
        }
    }

    private void HideWinIndicators()
    {
        if (reelImagesList == null)
        {
            return;
        }

        for (int column = 0; column < reelImagesList.Count; column++)
        {
            List<Image> symbolImages = reelImagesList[column]?.images;
            if (symbolImages == null)
            {
                continue;
            }

            for (int imageIndex = 0; imageIndex < symbolImages.Count; imageIndex++)
            {
                SetWinIndicatorActive(symbolImages[imageIndex], false);
            }
        }
    }

    private void HideWildMultiplierIndicators()
    {
        if (reelImagesList == null)
        {
            return;
        }

        for (int column = 0; column < reelImagesList.Count; column++)
        {
            List<Image> symbolImages = reelImagesList[column]?.images;
            if (symbolImages == null)
            {
                continue;
            }

            for (int imageIndex = 0; imageIndex < symbolImages.Count; imageIndex++)
            {
                HideWildMultiplierIndicator(symbolImages[imageIndex]);
            }
        }
    }

    internal void SetInitialMatrix(List<List<int>> matrix)
    {
        if (matrix == null || matrix.Count == 0) return;

        currentDisplayMatrix = matrix;

        int cols = matrix.Count;
        reelCycleCount.Clear();
        for (int col = 0; col < cols; col++)
        {
            reelCycleCount.Add(0);
        }

        for (int col = 0; col < cols; col++)
        {
            SetReelSymbols(col, matrix[col]);
            RestoreReelPosition(col);

            // Hide buffer images (indices 0, 1, 5, 6), and set visible images (2, 3, 4) to alpha 1
            SetImageAlpha(col, 0, 0f);
            SetImageAlpha(col, 1, 0f);
            SetImageAlpha(col, 2, 1f);
            SetImageAlpha(col, 3, 1f);
            SetImageAlpha(col, 4, 1f);
            SetImageAlpha(col, 5, 0f);
            SetImageAlpha(col, 6, 0f);

        }
    }

    #endregion

    #region Symbol Display

    private void SetReelSymbols(int columnIndex, List<int> visibleSymbolIds)
    {
        if (columnIndex >= reelImagesList.Count)
        {
            Debug.LogError($"SetReelSymbols: Invalid column index {columnIndex}, max is {reelImagesList.Count - 1}");
            return;
        }

        int expectedRows = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.rowCount
            : StPatricksGoldDefinition.RowCount;
        if (visibleSymbolIds == null || visibleSymbolIds.Count != expectedRows)
        {
            Debug.LogError($"SetReelSymbols: Invalid visibleSymbolIds count {visibleSymbolIds?.Count}, expected {expectedRows}");
            return;
        }

        var reel = reelImagesList[columnIndex];

        if (reel.images == null || reel.images.Count != 7)
        {
            Debug.LogError($"SetReelSymbols: Reel {columnIndex} has invalid image count {reel.images?.Count}, expected 7");
            return;
        }

        for (int imageIndex = 0; imageIndex < reel.images.Count; imageIndex++)
        {
            HideWildMultiplierIndicator(reel.images[imageIndex]);
        }

        int visibleRows = visibleSymbolIds.Count;
        // Visible rows sit at indices 2, 3, 4 (middle of 7)
        for (int row = 0; row < visibleRows; row++)
        {
            int imageIndex = 2 + row;
            int symbolId = visibleSymbolIds[row];
            reel.images[imageIndex].sprite = GetSymbolSprite(symbolId);
        }

        int symbolCount = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.symbols.Count
            : StPatricksGoldDefinition.SymbolCount;

        // Fill 2 buffer images above the visible area (indices 0 and 1)
        for (int imageIndex = 0; imageIndex < 2; imageIndex++)
        {
            reel.images[imageIndex].sprite = GetSymbolSprite(GetRandomSpinSymbolId(symbolCount));
        }

        // Fill 2 buffer images below the visible area (indices 5 and 6)
        for (int imageIndex = 5; imageIndex < 7; imageIndex++)
        {
            reel.images[imageIndex].sprite = GetSymbolSprite(GetRandomSpinSymbolId(symbolCount));
        }

    }

    private Sprite GetSymbolSprite(int symbolId)
    {
        // Validate symbolId range (0-15)
        if (symbolId < 0 || symbolId >= symbolSprites.Length)
        {
            Debug.LogWarning($"[SlotView] Invalid symbolId {symbolId}, using default sprite 0. Total sprites: {symbolSprites.Length}");
            return symbolSprites[0];
        }

        if (symbolSprites[symbolId] == null)
        {
            Debug.LogError($"[SlotView] Symbol sprite for ID {symbolId} is null!");
            return symbolSprites[0];
        }

        return symbolSprites[symbolId];
    }

    #endregion

    #region Spin Animation

    internal void StartSpin(SpinSpeed spinSpeedMode)
    {
        if (isSpinning) return;

        activeSpinSpeed = spinSpeedMode;
        isSpinning = true;
        KillAllTweens();

        for (int i = 0; i < reelCycleCount.Count; i++)
        {
            reelCycleCount[i] = 0;
        }

        int cols = currentDisplayMatrix != null
            ? currentDisplayMatrix.Count
            : StPatricksGoldDefinition.ReelCount;

        for (int col = 0; col < cols; col++)
        {
            // Buffer images (0, 1, 5, 6) already hold valid sprites from the end of the previous spin.
            // Keeping them as-is prevents visual popping/glitches when the spin starts.

            // Set all 7 images of each reel to alpha 1 at start of spin so they are visible during movement
            for (int i = 0; i < 7; i++)
            {
                SetImageAlpha(col, i, 1f);
            }

            StartReelCycleWithDelay(col, col * reelStartStagger);
        }
    }

    private void StartReelCycleWithDelay(int columnIndex, float delay)
    {
        if (columnIndex >= reelTransforms.Length) return;

        while (spinTweens.Count <= columnIndex) spinTweens.Add(null);
        if (spinTweens[columnIndex] != null) { spinTweens[columnIndex].Kill(); spinTweens[columnIndex] = null; }

        // Begin the reel motion cleanly without an opposite-direction wind-up.
        // The small left-to-right delay retains the mechanical reel cascade.
        if (delay <= 0f)
        {
            StartReelCycle(columnIndex);
            return;
        }

        Tween startDelayTween = DOVirtual.DelayedCall(delay, () =>
        {
            if (isSpinning)
            {
                StartReelCycle(columnIndex);
            }
        });
        spinTweens[columnIndex] = startDelayTween;
    }

    private void StartReelCycle(int columnIndex)
    {
        if (columnIndex >= reelTransforms.Length) return;
        if (!isSpinning) return;

        Transform slotTransform = reelTransforms[columnIndex];
        Vector3 restingPosition = GetReelRestingPosition(columnIndex);
        float cycleDistance = GetReelCycleDistance(columnIndex);
        slotTransform.localPosition = restingPosition;

        const float defaultSpeedMultiplier = 1f;
        float configuredSpeedMultiplier = activeSpinSpeed != SpinSpeed.Normal
            ? fastSpinReelSpeedMultiplier
            : reelSpeedMultiplier;
        float speedMultiplier = configuredSpeedMultiplier > 0f
            ? Mathf.Clamp(configuredSpeedMultiplier, 0.25f, 2f)
            : defaultSpeedMultiplier;
        float symbolCycleDuration = Mathf.Max(0.01f, spinSpeed) / speedMultiplier;

        Sequence cycleSequence = DOTween.Sequence();

        cycleSequence.Append(
            slotTransform.DOLocalMoveY(restingPosition.y - cycleDistance, symbolCycleDuration)
                .SetEase(Ease.Linear)
        );

        cycleSequence.OnComplete(() => {
            if (isSpinning)
            {
                CycleReelSymbols(columnIndex);

                slotTransform.localPosition = restingPosition;

                if (columnIndex < reelCycleCount.Count)
                {
                    reelCycleCount[columnIndex]++;
                }

                StartReelCycle(columnIndex);
            }
        });

        cycleSequence.Play();

        if (spinTweens.Count <= columnIndex)
            spinTweens.Add(cycleSequence);
        else
            spinTweens[columnIndex] = cycleSequence;
    }

    private void CycleReelSymbols(int columnIndex, int forcedTopSymbolId = -1)
    {
        var reel = reelImagesList[columnIndex];
        if (reel.images == null || reel.images.Count != 7) return;

        for (int i = 6; i > 0; i--)
        {
            reel.images[i].sprite = reel.images[i - 1].sprite;
        }

        int maxSymbolId = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.symbols.Count
            : StPatricksGoldDefinition.SymbolCount;
        
        int nextSymbolId = forcedTopSymbolId >= 0
            ? forcedTopSymbolId
            : GetRandomSpinSymbolId(maxSymbolId);
        reel.images[0].sprite = GetSymbolSprite(nextSymbolId);
    }

    private int GetRandomSpinSymbolId(int symbolCount)
    {
        int validSymbolCount = Mathf.Clamp(symbolCount, 1, symbolSprites.Length);
        return Random.Range(0, validSymbolCount);
    }

    #endregion

    #region Stop Spin

    internal void StopSpin(List<List<int>> resultMatrix, System.Action onComplete)
    {
        if (!TryValidateResultMatrix(resultMatrix, out string error))
        {
            Debug.LogError($"[SlotView] Cannot stop reels with an invalid result matrix: {error}");
            CancelSpin();
            onComplete?.Invoke();
            return;
        }

        if (!isSpinning)
        {
            currentDisplayMatrix = resultMatrix;
            int cols = resultMatrix.Count;
            for (int col = 0; col < cols; col++)
            {
                SetReelSymbols(col, resultMatrix[col]);
                ApplyStoppedReelLayout(col);
                RestoreReelPosition(col);
            }
            
            onComplete?.Invoke();
            return;
        }

        stopSpinCoroutine = StartCoroutine(StopSpinSequence(resultMatrix, onComplete, false));
    }

    private IEnumerator StopSpinSequence(List<List<int>> resultMatrix, System.Action onComplete, bool isQuickStop)
    {
        currentDisplayMatrix = resultMatrix;

        int cols = resultMatrix.Count;

        // Wait until minimum spin cycles are complete
        while (true)
        {
            bool allReelsReady = true;
            for (int col = 0; col < cols; col++)
            {
                if (reelCycleCount[col] < minSpinCyclesBeforeStop)
                {
                    allReelsReady = false;
                    break;
                }
            }

            if (allReelsReady) break;
            yield return null;
        }

        float stagger = isQuickStop ? quickStopStagger : reelStopStagger;
        int stoppedReels = 0;

        // Start stopping each reel with stagger
        for (int col = 0; col < cols; col++)
        {
            float delay = col * stagger;
            StartCoroutine(StopSingleReel(
                col,
                resultMatrix[col],
                delay,
                isQuickStop,
                () => stoppedReels++
            ));
        }

        while (stoppedReels < cols)
        {
            yield return null;
        }

        isSpinning = false;
        stopSpinCoroutine = null;

        onComplete?.Invoke();
    }

    private IEnumerator StopSingleReel(
        int columnIndex,
        List<int> targetSymbols,
        float delay,
        bool isQuickStop,
        System.Action onStopped)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (columnIndex < spinTweens.Count && spinTweens[columnIndex] != null)
        {
            spinTweens[columnIndex].Kill();
        }

        Transform slotTransform = reelTransforms[columnIndex];
        Vector3 restingPosition = GetReelRestingPosition(columnIndex);
        float cycleDistance = GetReelCycleDistance(columnIndex);
        float configuredSpeedMultiplier = activeSpinSpeed != SpinSpeed.Normal
            ? fastSpinReelSpeedMultiplier
            : reelSpeedMultiplier;
        float speedMultiplier = Mathf.Clamp(configuredSpeedMultiplier, 0.25f, 2f);
        float cycleDuration = Mathf.Max(0.01f, spinSpeed) / speedMultiplier;

        // Finish the currently visible cycle instead of replacing the visible
        // symbols. The bottom result symbol enters through the hidden top buffer.
        float currentY = slotTransform.localPosition.y;
        float progress = Mathf.Clamp01(
            (restingPosition.y - currentY) / cycleDistance
        );
        float remainingDuration = cycleDuration * (1f - progress);
        if (remainingDuration > 0.001f)
        {
            Tween finishCurrentCycle = slotTransform
                .DOLocalMoveY(restingPosition.y - cycleDistance, remainingDuration)
                .SetEase(Ease.Linear);
            spinTweens[columnIndex] = finishCurrentCycle;
            yield return finishCurrentCycle.WaitForCompletion();
        }

        CycleReelSymbols(columnIndex, targetSymbols[targetSymbols.Count - 1]);
        slotTransform.localPosition = restingPosition;

        // Feed the remaining result symbols from bottom to top, then add two
        // hidden fillers. The supplied result reaches indices 2, 3 and 4 through
        // actual reel movement rather than appearing there in a single frame.
        int landingCycles = targetSymbols.Count + 1;
        float bounceSpeedFactor = isQuickStop ? 0.7f : 1f;
        float bounceDistance = Mathf.Max(0f, stopBounceDistance) * bounceSpeedFactor;
        for (int cycle = 0; cycle < landingCycles; cycle++)
        {
            bool isFinalCycle = cycle == landingCycles - 1;
            float movementDuration = isFinalCycle
                ? (isQuickStop
                    ? Mathf.Max(0.01f, quickStopDuration)
                    : cycleDuration * Mathf.Max(2f, finalStopDurationMultiplier))
                : cycleDuration;
            Ease movementEase = isFinalCycle ? Ease.OutCubic : Ease.Linear;
            float movementTargetY = restingPosition.y - cycleDistance;
            if (isFinalCycle)
            {
                movementTargetY -= bounceDistance;
            }

            Tween landingCycle = slotTransform
                .DOLocalMoveY(movementTargetY, movementDuration)
                .SetEase(movementEase);
            spinTweens[columnIndex] = landingCycle;
            yield return landingCycle.WaitForCompletion();

            int targetIndex = targetSymbols.Count - 2 - cycle;
            int forcedSymbolId = targetIndex >= 0 ? targetSymbols[targetIndex] : -1;
            CycleReelSymbols(columnIndex, forcedSymbolId);
            slotTransform.localPosition = new Vector3(
                restingPosition.x,
                isFinalCycle
                    ? restingPosition.y - bounceDistance
                    : restingPosition.y,
                restingPosition.z
            );
        }

        Sequence stopPop = CreateStopPop(columnIndex, isQuickStop);
        spinTweens[columnIndex] = stopPop;
        yield return stopPop.WaitForCompletion();

        ApplyStoppedReelLayout(columnIndex);
        onStopped?.Invoke();
    }

    private Sequence CreateStopPop(int columnIndex, bool isQuickStop)
    {
        Transform reelTransform = reelTransforms[columnIndex];
        Vector3 restingPosition = GetReelRestingPosition(columnIndex);
        Sequence stopPop = DOTween.Sequence();
        float speedFactor = isQuickStop ? 0.7f : 1f;
        float returnDuration = Mathf.Max(0.01f, stopBounceReturnDuration * speedFactor);

        // The final cycle already travelled past the result position. This is
        // only the rebound, so the reel never stops and then dips a second time.
        stopPop.Append(
            reelTransform
                .DOLocalMoveY(restingPosition.y, returnDuration)
                .SetEase(Ease.OutBounce)
        );

        return stopPop;
    }

    #endregion

    #region Quick Spin

    internal void ShowServerResultImmediately(
        List<List<int>> resultMatrix,
        System.Action onComplete = null)
    {
        if (!TryValidateResultMatrix(resultMatrix, out string error))
        {
            Debug.LogError($"[SlotView] Cannot show an invalid server result matrix: {error}");
            CancelSpin();
            onComplete?.Invoke();
            return;
        }

        StopAllCoroutines();
        stopSpinCoroutine = null;
        winAnimationCoroutine = null;
        KillAllTweens();

        currentDisplayMatrix = resultMatrix;
        isSpinning = false;

        for (int column = 0; column < resultMatrix.Count; column++)
        {
            SetReelSymbols(column, resultMatrix[column]);
            ApplyStoppedReelLayout(column);
            RestoreReelPosition(column);
        }

        onComplete?.Invoke();
    }

    internal void QuickStop(List<List<int>> resultMatrix, System.Action onComplete = null)
    {
        if (!isSpinning)
        {
            currentDisplayMatrix = resultMatrix;
            int cols = resultMatrix.Count;
            for (int col = 0; col < cols; col++)
            {
                if (col < reelTransforms.Length)
                {
                    SetReelSymbols(col, resultMatrix[col]);
                    ApplyStoppedReelLayout(col);
                    RestoreReelPosition(col);
                }
            }
            
            onComplete?.Invoke();
            return;
        }

        stopSpinCoroutine = StartCoroutine(StopSpinSequence(resultMatrix, onComplete, true));
    }

    #endregion






    #region Win Line Animation

    internal void ShowWinLineAnimation(List<WinLine> winLines, System.Action onComplete)
    {

        if (winLines == null || winLines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StopWinAnimations();
        winAnimationCoroutine = StartCoroutine(PlayWinningSymbols(winLines, onComplete));
    }

    internal bool ShowPrioritySymbolAnimation(
        IEnumerable<int> flatPositions,
        float duration,
        System.Action onComplete)
    {
        if (flatPositions == null)
        {
            return false;
        }

        var uniquePositions = new HashSet<int>(flatPositions);
        if (uniquePositions.Count == 0)
        {
            return false;
        }

        StopWinAnimations();
        winAnimationCoroutine = StartCoroutine(
            PlayPrioritySymbolAnimation(
                uniquePositions,
                Mathf.Max(0.1f, duration),
                onComplete));
        return true;
    }

    internal void CancelWinAnimation()
    {
        StopWinAnimations();
    }

    private IEnumerator PlayPrioritySymbolAnimation(
        IEnumerable<int> flatPositions,
        float duration,
        System.Action onComplete)
    {
        AnimateWinPositions(flatPositions, duration);
        yield return new WaitForSecondsRealtime(duration);

        StopWinAnimations(false);
        winAnimationCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayWinningSymbols(List<WinLine> winLines, System.Action onComplete)
    {
        bool isAuto = (gameManager != null && gameManager.isAutoPlaying);
        float cycleDuration = Mathf.Max(0.1f, winSymbolLoopDuration);
        float wildLoopDuration =
            cycleDuration /
            Mathf.Max(0.01f, wildWinAnimationSpeedMultiplier);
        float stageDuration =
            wildLoopDuration * Mathf.Max(1, wildWinLoopsBeforeNextStage);

        // Celebrate the complete result first, then present each returned line
        // separately so overlapping wins are still easy to read.
        HashSet<int> allWinningPositions = new HashSet<int>();
        List<WinningLinePresentation> individualWinningLines =
            new List<WinningLinePresentation>();
        foreach (WinLine winLine in winLines)
        {
            if (winLine?.positions == null) continue;

            HashSet<int> linePositions = new HashSet<int>(winLine.positions);
            if (linePositions.Count == 0) continue;

            individualWinningLines.Add(new WinningLinePresentation
            {
                Positions = linePositions,
                DisplayRow = GetPreferredCenterColumnRow(linePositions),
                WinAmount = winLine.winAmount,
                WildMultiplier = winLine.wildMultiplier,
                WildDetails = winLine.wildDetails
            });

            foreach (int flatIndex in linePositions)
            {
                allWinningPositions.Add(flatIndex);
            }
        }

        if (allWinningPositions.Count == 0)
        {
            winAnimationCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        // The separate TotalWin text remains visible for the complete-result
        // celebration. Per-row amounts begin with the individual lines below.
        WinLineAmountPresentationChanged?.Invoke(-1, 0);
        AnimateWinPositions(allWinningPositions, cycleDuration);
        yield return new WaitForSecondsRealtime(stageDuration);
        StopWinAnimations(false);

        if (isAuto)
        {
            // Autoplay shows every individual line once, then advances normally.
            for (int lineIndex = 0; lineIndex < individualWinningLines.Count; lineIndex++)
            {
                WinningLinePresentation line = individualWinningLines[lineIndex];
                WinLineAmountPresentationChanged?.Invoke(
                    line.DisplayRow,
                    line.WinAmount);
                ShowWildMultiplierIcons(line, cycleDuration);
                AnimateWinPositions(
                    line.Positions,
                    cycleDuration);
                yield return new WaitForSecondsRealtime(stageDuration);
                StopWinAnimations(false);
            }

            winAnimationCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        // Normal play stays idle while the individual lines repeat. Starting the
        // next spin calls StopWinAnimations through KillAllTweens and ends this loop.
        onComplete?.Invoke();
        while (true)
        {
            for (int lineIndex = 0; lineIndex < individualWinningLines.Count; lineIndex++)
            {
                WinningLinePresentation line = individualWinningLines[lineIndex];
                WinLineAmountPresentationChanged?.Invoke(
                    line.DisplayRow,
                    line.WinAmount);
                ShowWildMultiplierIcons(line, cycleDuration);
                AnimateWinPositions(
                    line.Positions,
                    cycleDuration);
                yield return new WaitForSecondsRealtime(stageDuration);
                StopWinAnimations(false);
            }
        }
    }

    private int GetPreferredCenterColumnRow(IEnumerable<int> flatPositions)
    {
        int cols = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.reelCount
            : StPatricksGoldDefinition.ReelCount;
        int rows = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.rowCount
            : StPatricksGoldDefinition.RowCount;

        if (cols <= 0 || rows <= 0 || flatPositions == null)
        {
            return -1;
        }

        int centerColumn = cols / 2;
        bool hasTop = false;
        bool hasMiddle = false;
        bool hasBottom = false;

        foreach (int flatIndex in flatPositions)
        {
            int row = flatIndex / cols;
            int column = flatIndex % cols;
            if (column != centerColumn || row < 0 || row >= rows)
            {
                continue;
            }

            if (row == 0)
            {
                hasTop = true;
            }
            else if (row == 1)
            {
                hasMiddle = true;
            }
            else if (row == 2)
            {
                hasBottom = true;
            }
        }

        // The center text wins whenever the middle-center cell is part of the
        // line, including top+middle and middle+bottom combinations.
        if (hasMiddle) return 1;
        if (hasTop) return 0;
        if (hasBottom) return 2;
        return -1;
    }

    private void AnimateWinPositions(
        IEnumerable<int> flatPositions,
        float animationDuration)
    {
        if (flatPositions == null) return;

        int cols = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.reelCount
            : StPatricksGoldDefinition.ReelCount;
        int rows = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.rowCount
            : StPatricksGoldDefinition.RowCount;
        HashSet<int> animatedPositions = new HashSet<int>();

        foreach (int flatIndex in flatPositions)
        {
            if (!animatedPositions.Add(flatIndex)) continue;

            int row = flatIndex / cols;
            int col = flatIndex % cols;
            if (col < 0 || col >= cols || row < 0 || row >= rows) continue;

            AnimateWinSymbol(col, row, animationDuration);
        }
    }

    private void AnimateWinSymbol(
        int column,
        int row,
        float animationDuration)
    {
        if (column >= reelImagesList.Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Invalid column {column}, max is {reelImagesList.Count - 1}");
            return;
        }

        var reel = reelImagesList[column];
        if (reel.images == null || reel.images.Count < 5)
        {
            Debug.LogError($"[AnimateWinSymbol] Reel {column} has invalid images list");
            return;
        }

        int visualRow = GetVisualRow(column, row);
        int imageIndex = 2 + visualRow;
        if (imageIndex >= reel.images.Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Image index {imageIndex} out of range for reel {column}");
            return;
        }

        Image symbolImage = reel.images[imageIndex];
        if (symbolImage == null)
        {
            Debug.LogError($"[AnimateWinSymbol] Symbol image is NULL at col: {column}, row: {row}, imageIndex: {imageIndex}");
            return;
        }

        PlaySymbolAnimation(column, imageIndex, animationDuration);
    }

    private void StopWinAnimations(bool stopCoroutine = true)
    {
        if (stopCoroutine && winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }

        WinLineAmountPresentationChanged?.Invoke(-1, 0);

        // Restore all symbol images after win animations
        for (int col = 0; col < reelImagesList.Count; col++)
        {
            var reel = reelImagesList[col];
            if (reel.images != null)
            {
                for (int imageIndex = 0; imageIndex < reel.images.Count; imageIndex++)
                {
                    ResetSymbolAnimation(col, imageIndex);
                }
            }
        }

        for (int col = 0; col < reelImagesList.Count; col++)
        {
            ApplyStoppedReelLayout(col);
        }
    }

    private int GetVisualRow(int col, int row)
    {
        return row;
    }

    private Image GetSymbolWinAnimationImage(int column, int imageIndex)
    {
        if (column < 0 || column >= reelImagesList.Count)
        {
            return null;
        }

        var reel = reelImagesList[column];
        if (reel.winAnimationImages == null)
        {
            return null;
        }

        const int firstVisibleImageIndex = 2;
        const int visibleImageCount = 3;

        // A compact list maps directly to the three visible reel rows.
        if (reel.winAnimationImages.Count == visibleImageCount)
        {
            int visibleIndex = imageIndex - firstVisibleImageIndex;
            return visibleIndex >= 0 && visibleIndex < visibleImageCount
                ? reel.winAnimationImages[visibleIndex]
                : null;
        }

        if (imageIndex < 0 || imageIndex >= reel.winAnimationImages.Count)
        {
            return null;
        }

        return reel.winAnimationImages[imageIndex];
    }

    private Image GetSymbolImage(int column, int imageIndex)
    {
        if (column < 0 || column >= reelImagesList.Count)
        {
            return null;
        }

        var reel = reelImagesList[column];
        if (reel.images == null || imageIndex < 0 || imageIndex >= reel.images.Count)
        {
            return null;
        }

        return reel.images[imageIndex];
    }

    private int GetSymbolId(Sprite symbolSprite)
    {
        if (symbolSprite == null || symbolSprites == null)
        {
            return -1;
        }

        for (int symbolId = 0; symbolId < symbolSprites.Length; symbolId++)
        {
            if (symbolSprites[symbolId] == symbolSprite)
            {
                return symbolId;
            }
        }

        return -1;
    }

    private void PlaySymbolAnimation(
        int column,
        int imageIndex,
        float animationDuration)
    {
        Image symbolImage = GetSymbolImage(column, imageIndex);
        if (symbolImage == null)
        {
            Debug.LogWarning($"[AnimateWinSymbol] Missing symbol Image for col {column}, imageIndex {imageIndex}");
            return;
        }

        SetWinIndicatorActive(symbolImage, true);

        if (symbolAnimationManager == null)
        {
            Debug.LogWarning("[AnimateWinSymbol] SlotSymbolAnimationManager is not assigned to SlotView");
            return;
        }

        Image animationImage = GetSymbolWinAnimationImage(column, imageIndex);
        if (animationImage == null)
        {
            Debug.LogWarning($"[AnimateWinSymbol] Missing child animation Image for col {column}, imageIndex {imageIndex}");
            return;
        }

        int symbolId = GetSymbolId(symbolImage.sprite);
        float speedMultiplier =
            symbolId == StPatricksGoldSymbolIds.Wild
                ? wildWinAnimationSpeedMultiplier
                : nonWildWinAnimationSpeedMultiplier;
        float symbolLoopDuration =
            animationDuration / Mathf.Max(0.01f, speedMultiplier);

        symbolAnimationManager.PlayAnimation(
            symbolId,
            symbolImage,
            animationImage,
            symbolLoopDuration);
    }

    private void ResetSymbolAnimation(int column, int imageIndex)
    {
        Image symbolImage = GetSymbolImage(column, imageIndex);
        if (symbolImage == null)
        {
            return;
        }

        SetWinIndicatorActive(symbolImage, false);
        HideWildMultiplierIndicator(symbolImage);

        Image animationImage = GetSymbolWinAnimationImage(column, imageIndex);
        if (symbolAnimationManager != null)
        {
            symbolAnimationManager.StopAnimation(symbolImage, animationImage);
        }
        else
        {
            Color overlayColor = animationImage != null ? animationImage.color : Color.white;
            if (animationImage != null)
            {
                animationImage.color =
                    new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
            }
        }

        Color c = symbolImage.color;
        symbolImage.color = new Color(c.r, c.g, c.b, 1f);
        symbolImage.enabled = true;
    }

    private void SetWinIndicatorActive(Image symbolImage, bool isActive)
    {
        GameObject winIndicator = GetWinIndicator(symbolImage);
        if (winIndicator != null)
        {
            winIndicator.SetActive(isActive);
        }
    }

    private GameObject GetWinIndicator(Image symbolImage)
    {
        if (symbolImage == null)
        {
            return null;
        }

        if (winIndicators.TryGetValue(
                symbolImage,
                out GameObject cachedIndicator) &&
            cachedIndicator != null)
        {
            return cachedIndicator;
        }

        Transform symbolTransform = symbolImage.transform;
        Transform winTransform = symbolTransform.Find("Win");

        if (winTransform == null)
        {
            Transform[] descendants =
                symbolTransform.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < descendants.Length; index++)
            {
                Transform descendant = descendants[index];
                if (descendant != null &&
                    descendant != symbolTransform &&
                    string.Equals(
                        descendant.name,
                        "Win",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    winTransform = descendant;
                    break;
                }
            }
        }

        GameObject winIndicator =
            winTransform != null ? winTransform.gameObject : null;
        if (winIndicator != null)
        {
            winIndicators[symbolImage] = winIndicator;
        }

        return winIndicator;
    }

    private void ShowWildMultiplierIcons(
        WinningLinePresentation line,
        float symbolLoopDuration)
    {
        if (line == null || uiManager == null)
        {
            return;
        }

        int cols = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.reelCount
            : StPatricksGoldDefinition.ReelCount;
        int rows = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.rowCount
            : StPatricksGoldDefinition.RowCount;
        HashSet<int> presentedPositions = new HashSet<int>();

        if (line.WildDetails != null)
        {
            for (int index = 0; index < line.WildDetails.Count; index++)
            {
                WildDetail detail = line.WildDetails[index];
                if (detail == null ||
                    detail.col < 0 ||
                    detail.col >= cols ||
                    detail.row < 0 ||
                    detail.row >= rows ||
                    GetSymbolIdAt(detail.col, detail.row) !=
                        StPatricksGoldSymbolIds.Wild ||
                    uiManager.GetWildMultiplierIcon(detail.multiplier) == null)
                {
                    continue;
                }

                int flatIndex = detail.row * cols + detail.col;
                if (!presentedPositions.Add(flatIndex))
                {
                    continue;
                }

                ShowWildMultiplierIcon(
                    detail.col,
                    detail.row,
                    detail.multiplier,
                    symbolLoopDuration);
            }
        }

        if (presentedPositions.Count > 0 ||
            line.WildMultiplier <= 1 ||
            line.Positions == null)
        {
            return;
        }

        // Older responses may omit wildDetails. The aggregate multiplier is
        // unambiguous only when the line contains one Wild.
        int fallbackColumn = -1;
        int fallbackRow = -1;
        int wildCount = 0;
        foreach (int flatIndex in line.Positions)
        {
            int row = flatIndex / cols;
            int column = flatIndex % cols;
            if (column < 0 ||
                column >= cols ||
                row < 0 ||
                row >= rows ||
                GetSymbolIdAt(column, row) != StPatricksGoldSymbolIds.Wild)
            {
                continue;
            }

            fallbackColumn = column;
            fallbackRow = row;
            wildCount++;
        }

        if (wildCount == 1 &&
            uiManager.GetWildMultiplierIcon(line.WildMultiplier) != null)
        {
            ShowWildMultiplierIcon(
                fallbackColumn,
                fallbackRow,
                line.WildMultiplier,
                symbolLoopDuration);
        }
    }

    private void ShowWildMultiplierIcon(
        int column,
        int row,
        int multiplier,
        float symbolLoopDuration)
    {
        int imageIndex = 2 + GetVisualRow(column, row);
        Image symbolImage = GetSymbolImage(column, imageIndex);
        Image multiplierImage = GetWildMultiplierIndicator(symbolImage);
        Sprite finalSprite = uiManager != null
            ? uiManager.GetWildMultiplierIcon(multiplier)
            : null;

        if (multiplierImage == null || finalSprite == null)
        {
            return;
        }

        HideWildMultiplierIndicator(symbolImage);
        multiplierImage.sprite = finalSprite;
        Color color = multiplierImage.color;
        multiplierImage.color = new Color(
            color.r,
            color.g,
            color.b,
            1f);
        multiplierImage.enabled = true;
        multiplierImage.gameObject.SetActive(true);

        Image wildAnimationImage =
            GetSymbolWinAnimationImage(column, imageIndex);
        Coroutine animation = StartCoroutine(
            AnimateWildMultiplierIcon(
                multiplierImage,
                wildAnimationImage,
                symbolLoopDuration));
        wildMultiplierAnimations[multiplierImage] = animation;
    }

    private IEnumerator AnimateWildMultiplierIcon(
        Image multiplierImage,
        Image wildAnimationImage,
        float symbolLoopDuration)
    {
        if (multiplierImage == null)
        {
            yield break;
        }

        WildMultiplierTransformState transformState =
            GetWildMultiplierTransformState(multiplierImage);
        RectTransform multiplierRect = multiplierImage.rectTransform;
        if (transformState == null || multiplierRect == null)
        {
            yield break;
        }

        float popScale = uiManager != null
            ? uiManager.GetWildMultiplierPopScale()
            : 1.1f;
        float shakeAngle = uiManager != null
            ? uiManager.GetWildMultiplierShakeAngle()
            : 2f;
        int shakesPerSymbolLoop = uiManager != null
            ? uiManager.GetWildMultiplierShakesPerSymbolLoop()
            : 4;
        float loopDuration = Mathf.Max(0.01f, symbolLoopDuration);

        // The multiplier always stays at its Inspector-authored position.
        multiplierRect.anchoredPosition = transformState.AnchoredPosition;
        multiplierRect.localRotation = transformState.LocalRotation;
        multiplierRect.localScale = transformState.LocalScale;

        float elapsed = 0f;

        while (multiplierImage != null &&
               multiplierImage.gameObject.activeInHierarchy)
        {
            float loopProgress = (elapsed % loopDuration) / loopDuration;
            float shakePhase =
                loopProgress *
                Mathf.PI *
                2f *
                shakesPerSymbolLoop;
            float shakeRotation = Mathf.Sin(shakePhase) * shakeAngle;
            multiplierRect.localRotation =
                transformState.LocalRotation *
                Quaternion.Euler(0f, 0f, shakeRotation);

            // Follow the visual grow/shrink of the exact Wild animation frame
            // currently displayed by SlotSymbolAnimationManager.
            float wildVisualSize01 = 0f;
            if (symbolAnimationManager != null)
            {
                symbolAnimationManager.TryGetAnimationVisualSize(
                    wildAnimationImage,
                    out wildVisualSize01);
            }

            float scaleMultiplier =
                Mathf.Lerp(1f, popScale, wildVisualSize01);
            multiplierRect.localScale = new Vector3(
                transformState.LocalScale.x * scaleMultiplier,
                transformState.LocalScale.y * scaleMultiplier,
                transformState.LocalScale.z);
            multiplierRect.anchoredPosition =
                transformState.AnchoredPosition;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void HideWildMultiplierIndicator(Image symbolImage)
    {
        Image multiplierImage = GetWildMultiplierIndicator(symbolImage);
        if (multiplierImage == null)
        {
            return;
        }

        if (wildMultiplierAnimations.TryGetValue(
                multiplierImage,
                out Coroutine animation) &&
            animation != null)
        {
            StopCoroutine(animation);
        }

        wildMultiplierAnimations.Remove(multiplierImage);
        RestoreWildMultiplierTransform(multiplierImage);
        Color color = multiplierImage.color;
        multiplierImage.color = new Color(
            color.r,
            color.g,
            color.b,
            1f);
        multiplierImage.gameObject.SetActive(false);
    }

    private WildMultiplierTransformState GetWildMultiplierTransformState(
        Image multiplierImage)
    {
        if (multiplierImage == null)
        {
            return null;
        }

        if (wildMultiplierTransformStates.TryGetValue(
                multiplierImage,
                out WildMultiplierTransformState existingState))
        {
            return existingState;
        }

        RectTransform multiplierRect = multiplierImage.rectTransform;
        if (multiplierRect == null)
        {
            return null;
        }

        var state = new WildMultiplierTransformState
        {
            AnchoredPosition = multiplierRect.anchoredPosition,
            LocalRotation = multiplierRect.localRotation,
            LocalScale = multiplierRect.localScale
        };
        wildMultiplierTransformStates[multiplierImage] = state;
        return state;
    }

    private void RestoreWildMultiplierTransform(Image multiplierImage)
    {
        WildMultiplierTransformState state =
            GetWildMultiplierTransformState(multiplierImage);
        if (state == null || multiplierImage == null)
        {
            return;
        }

        RectTransform multiplierRect = multiplierImage.rectTransform;
        multiplierRect.anchoredPosition = state.AnchoredPosition;
        multiplierRect.localRotation = state.LocalRotation;
        multiplierRect.localScale = state.LocalScale;
    }

    private Image GetWildMultiplierIndicator(Image symbolImage)
    {
        if (symbolImage == null)
        {
            return null;
        }

        if (wildMultiplierIndicators.TryGetValue(
                symbolImage,
                out Image cachedIndicator) &&
            cachedIndicator != null)
        {
            return cachedIndicator;
        }

        Transform symbolTransform = symbolImage.transform;
        Transform multiplierTransform =
            symbolTransform.Find("WildMultiplier") ??
            symbolTransform.Find("WildMultiuplier");

        if (multiplierTransform == null)
        {
            Transform[] descendants =
                symbolTransform.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < descendants.Length; index++)
            {
                Transform descendant = descendants[index];
                if (descendant == null || descendant == symbolTransform)
                {
                    continue;
                }

                if (string.Equals(
                        descendant.name,
                        "WildMultiplier",
                        System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        descendant.name,
                        "WildMultiuplier",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    multiplierTransform = descendant;
                    break;
                }
            }
        }

        Image multiplierImage = multiplierTransform != null
            ? multiplierTransform.GetComponent<Image>()
            : null;
        if (multiplierImage != null)
        {
            wildMultiplierIndicators[symbolImage] = multiplierImage;
        }

        return multiplierImage;
    }

    private int GetSymbolIdAt(int col, int row)
    {
        if (currentDisplayMatrix != null && col < currentDisplayMatrix.Count)
        {
            var columnSymbols = currentDisplayMatrix[col];
            if (columnSymbols != null && row >= 0 && row < columnSymbols.Count)
            {
                return columnSymbols[row];
            }
        }
        return -1;
    }

    #endregion



    #region Helper Methods

    internal List<List<int>> GetCurrentDisplayMatrix()
    {
        return currentDisplayMatrix;
    }

    internal bool IsSpinning()
    {
        return isSpinning;
    }

    internal bool TryValidateResultMatrix(List<List<int>> matrix, out string error)
    {
        error = null;

        if (matrix == null || matrix.Count == 0)
        {
            error = "Result matrix is null or empty.";
            return false;
        }

        if (reelTransforms == null || reelTransforms.Length == 0)
        {
            error = "No reel transforms are assigned to SlotView.";
            return false;
        }

        if (matrix.Count != reelTransforms.Length)
        {
            error = $"Result matrix has {matrix.Count} columns, but SlotView has {reelTransforms.Length} reels.";
            return false;
        }

        if (reelImagesList == null || reelImagesList.Count != reelTransforms.Length)
        {
            error = $"SlotView has {reelImagesList?.Count ?? 0} reel image groups; expected {reelTransforms.Length}.";
            return false;
        }

        int expectedRows = gameManager?.stPatricksGoldConfig != null
            ? gameManager.stPatricksGoldConfig.rowCount
            : StPatricksGoldDefinition.RowCount;
        if (expectedRows <= 0)
        {
            error = $"SlotView has an invalid configured row count: {expectedRows}.";
            return false;
        }

        if (expectedRows != StPatricksGoldDefinition.RowCount)
        {
            error = $"SL-SPG requires exactly {StPatricksGoldDefinition.RowCount} visible rows, but the game configuration requires {expectedRows}.";
            return false;
        }

        if (symbolSprites == null || symbolSprites.Length == 0)
        {
            error = "SlotView symbol sprites have not been initialized.";
            return false;
        }

        for (int column = 0; column < matrix.Count; column++)
        {
            if (reelTransforms[column] == null)
            {
                error = $"Reel transform {column} is not assigned.";
                return false;
            }

            ReelImages reelImages = reelImagesList[column];
            if (reelImages == null || reelImages.images == null || reelImages.images.Count != 7)
            {
                error = $"Reel {column} must have exactly 7 assigned images.";
                return false;
            }

            List<int> resultColumn = matrix[column];
            if (resultColumn == null || resultColumn.Count != expectedRows)
            {
                error = $"Result column {column} has {resultColumn?.Count ?? 0} rows; expected {expectedRows}.";
                return false;
            }

            for (int row = 0; row < resultColumn.Count; row++)
            {
                int symbolId = resultColumn[row];
                if (symbolId < 0 || symbolId >= symbolSprites.Length || symbolSprites[symbolId] == null)
                {
                    error = $"Result symbol ID {symbolId} at column {column}, row {row} has no assigned SlotView sprite.";
                    return false;
                }
            }
        }

        return true;
    }

    internal void CancelSpin()
    {
        if (!isSpinning) return;

        isSpinning = false;

        StopAllCoroutines();
        stopSpinCoroutine = null;
        winAnimationCoroutine = null;
        KillAllTweens();

        if (!TryValidateResultMatrix(currentDisplayMatrix, out string error))
        {
            Debug.LogError($"[SlotView] Could not restore the previous matrix after cancelling the spin: {error}");
            return;
        }

        int columns = currentDisplayMatrix.Count;
        for (int column = 0; column < columns; column++)
        {
            List<int> symbols = currentDisplayMatrix[column];
            SetReelSymbols(column, symbols);
            ApplyStoppedReelLayout(column);
            RestoreReelPosition(column);
        }
    }



    private void KillAllTweens()
    {
        foreach (var tween in spinTweens)
        {
            tween?.Kill();
        }
        spinTweens.Clear();

        StopWinAnimations();
    }

    #endregion

    #region Reel Layout

    /// <summary>
    /// Restores stopped-reel visibility without changing the Inspector-configured
    /// Vertical Layout Group alignment, spacing, or symbol sizes.
    /// </summary>
    private void ApplyStoppedReelLayout(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= reelImagesList.Count) return;

        SetImageAlpha(columnIndex, 0, 0f);
        SetImageAlpha(columnIndex, 1, 0f);
        SetImageAlpha(columnIndex, 2, 1f);
        SetImageAlpha(columnIndex, 3, 1f);
        SetImageAlpha(columnIndex, 4, 1f);
        SetImageAlpha(columnIndex, 5, 0f);
        SetImageAlpha(columnIndex, 6, 0f);
    }

    private Vector3 GetReelRestingPosition(int columnIndex)
    {
        if (reelRestingLocalPositions != null &&
            columnIndex >= 0 &&
            columnIndex < reelRestingLocalPositions.Length)
        {
            return reelRestingLocalPositions[columnIndex];
        }

        if (reelTransforms != null &&
            columnIndex >= 0 &&
            columnIndex < reelTransforms.Length &&
            reelTransforms[columnIndex] != null)
        {
            return reelTransforms[columnIndex].localPosition;
        }

        return Vector3.zero;
    }

    private void RestoreReelPosition(int columnIndex)
    {
        if (reelTransforms == null ||
            columnIndex < 0 ||
            columnIndex >= reelTransforms.Length ||
            reelTransforms[columnIndex] == null)
        {
            return;
        }

        reelTransforms[columnIndex].localPosition =
            GetReelRestingPosition(columnIndex);
    }

    private float GetReelCycleDistance(int columnIndex)
    {
        float configuredSpacing = 0f;
        if (reelLayoutGroups != null &&
            columnIndex >= 0 &&
            columnIndex < reelLayoutGroups.Length &&
            reelLayoutGroups[columnIndex] != null)
        {
            configuredSpacing = reelLayoutGroups[columnIndex].spacing;
        }

        return Mathf.Max(0.01f, symbolHeight + configuredSpacing);
    }

    /// <summary>
    /// Universal helper to set the alpha of a specific image in a reel.
    /// </summary>
    private void SetImageAlpha(int columnIndex, int imageIndex, float alpha)
    {
        if (columnIndex >= reelImagesList.Count) return;
        var reel = reelImagesList[columnIndex];
        if (reel.images == null || imageIndex >= reel.images.Count) return;

        Image img = reel.images[imageIndex];
        if (img != null)
        {
            Color c = img.color;
            img.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        KillAllTweens();
    }

    #endregion
}

[System.Serializable]
public class ReelImages
{
    public List<Image> images = new List<Image>(7);
    [Tooltip("Assign the animation child Images for Image (2), Image (3), and Image (4), in that order.")]
    public List<Image> winAnimationImages = new List<Image>(3);
}
