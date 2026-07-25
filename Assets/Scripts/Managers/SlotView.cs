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
    [SerializeField] private float winSymbolLoopDuration = 1.5f;

    [Header("Reel Layout Settings")]
    [SerializeField] private float defaultSpacing = 0f;



    private float middlePosition = 0f;
    private float cycleDistance;


    private List<Tween> spinTweens = new List<Tween>();
    private List<int> reelCycleCount = new List<int>();
    private Coroutine winAnimationCoroutine;
    private Coroutine stopSpinCoroutine;
    private VerticalLayoutGroup[] reelLayoutGroups;


    internal List<List<int>> currentDisplayMatrix;

    private bool isSpinning;
    private SpinSpeed activeSpinSpeed = SpinSpeed.Normal;

    #region Initialization

    private void Start()
    {
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
        cycleDistance = symbolHeight;

        middlePosition = 0f;

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
        for (int i = 0; i < reelTransforms.Length; i++)
        {
            if (reelTransforms[i] != null)
            {
                reelLayoutGroups[i] = reelTransforms[i].GetComponent<VerticalLayoutGroup>();
            }
        }

        HideWinAnimationImages();
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
            SetReelSymbols(col, matrix[col], true);

            // Override initial Y position to 0
            if (col < reelTransforms.Length && reelTransforms[col] != null)
            {
                reelTransforms[col].localPosition = new Vector3(
                    reelTransforms[col].localPosition.x,
                    0f,
                    0f
                );
            }

            // Set layout group spacing to 0
            if (reelLayoutGroups != null && col < reelLayoutGroups.Length && reelLayoutGroups[col] != null)
            {
                reelLayoutGroups[col].spacing = 0f;
            }

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

    private void SetReelSymbols(int columnIndex, List<int> visibleSymbolIds, bool isInitial = false)
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

        if (isInitial && reelTransforms[columnIndex] != null)
        {
            reelTransforms[columnIndex].localPosition = new Vector3(
                reelTransforms[columnIndex].localPosition.x,
                middlePosition,
                0
            );
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

        slotTransform.localPosition = new Vector3(
            slotTransform.localPosition.x,
            middlePosition,
            0f
        );

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
            slotTransform.DOLocalMoveY(middlePosition - cycleDistance, symbolCycleDuration)
                .SetEase(Ease.Linear)
        );

        cycleSequence.OnComplete(() => {
            if (isSpinning)
            {
                CycleReelSymbols(columnIndex);

                slotTransform.localPosition = new Vector3(
                    slotTransform.localPosition.x,
                    middlePosition,
                    0f
                );

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
                SetReelSymbols(col, resultMatrix[col], false);
                ApplyStoppedReelLayout(col);
                if (col < reelTransforms.Length && reelTransforms[col] != null)
                {
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        middlePosition,
                        0
                    );
                }
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
        float configuredSpeedMultiplier = activeSpinSpeed != SpinSpeed.Normal
            ? fastSpinReelSpeedMultiplier
            : reelSpeedMultiplier;
        float speedMultiplier = Mathf.Clamp(configuredSpeedMultiplier, 0.25f, 2f);
        float cycleDuration = Mathf.Max(0.01f, spinSpeed) / speedMultiplier;

        // Finish the currently visible cycle instead of replacing the visible
        // symbols. The bottom result symbol enters through the hidden top buffer.
        float currentY = slotTransform.localPosition.y;
        float progress = Mathf.Clamp01(
            (middlePosition - currentY) / Mathf.Max(0.01f, cycleDistance)
        );
        float remainingDuration = cycleDuration * (1f - progress);
        if (remainingDuration > 0.001f)
        {
            Tween finishCurrentCycle = slotTransform
                .DOLocalMoveY(middlePosition - cycleDistance, remainingDuration)
                .SetEase(Ease.Linear);
            spinTweens[columnIndex] = finishCurrentCycle;
            yield return finishCurrentCycle.WaitForCompletion();
        }

        CycleReelSymbols(columnIndex, targetSymbols[targetSymbols.Count - 1]);
        slotTransform.localPosition = new Vector3(
            slotTransform.localPosition.x,
            middlePosition,
            0f
        );

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
            float movementTargetY = middlePosition - cycleDistance;
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
                slotTransform.localPosition.x,
                isFinalCycle ? middlePosition - bounceDistance : middlePosition,
                0f
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
        Sequence stopPop = DOTween.Sequence();
        float speedFactor = isQuickStop ? 0.7f : 1f;
        float returnDuration = Mathf.Max(0.01f, stopBounceReturnDuration * speedFactor);

        // The final cycle already travelled past the result position. This is
        // only the rebound, so the reel never stops and then dips a second time.
        stopPop.Append(
            reelTransform
                .DOLocalMoveY(middlePosition, returnDuration)
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
            SetReelSymbols(column, resultMatrix[column], false);
            ApplyStoppedReelLayout(column);

            reelTransforms[column].localPosition = new Vector3(
                reelTransforms[column].localPosition.x,
                middlePosition,
                0f
            );
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
                    SetReelSymbols(col, resultMatrix[col], false);
                    ApplyStoppedReelLayout(col);
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        middlePosition,
                        0
                    );
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
        AnimateWinPositions(flatPositions);
        yield return new WaitForSecondsRealtime(duration);

        StopWinAnimations(false);
        winAnimationCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayWinningSymbols(List<WinLine> winLines, System.Action onComplete)
    {
        bool isAuto = (gameManager != null && gameManager.isAutoPlaying);

        // Each winning position starts one frame sequence and keeps playing it.
        HashSet<int> allWinningPositions = new HashSet<int>();
        foreach (WinLine winLine in winLines)
        {
            if (winLine?.positions == null) continue;

            foreach (int flatIndex in winLine.positions)
            {
                allWinningPositions.Add(flatIndex);
            }
        }

        if (allWinningPositions.Count > 0)
        {
            AnimateWinPositions(allWinningPositions);
        }

        if (isAuto)
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.1f, winSymbolLoopDuration));
            StopWinAnimations(false);
            winAnimationCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        onComplete?.Invoke();
        while (true)
        {
            yield return null;
        }
    }

    private void AnimateWinPositions(IEnumerable<int> flatPositions)
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

            AnimateWinSymbol(col, row);
        }
    }

    private void AnimateWinSymbol(int column, int row)
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

        PlaySymbolAnimation(column, imageIndex);
    }

    private void StopWinAnimations(bool stopCoroutine = true)
    {
        if (stopCoroutine && winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }
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

    private void PlaySymbolAnimation(int column, int imageIndex)
    {
        Image symbolImage = GetSymbolImage(column, imageIndex);
        if (symbolImage == null)
        {
            Debug.LogWarning($"[AnimateWinSymbol] Missing symbol Image for col {column}, imageIndex {imageIndex}");
            return;
        }

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
        symbolAnimationManager.PlayAnimation(symbolId, symbolImage, animationImage);
    }

    private void ResetSymbolAnimation(int column, int imageIndex)
    {
        Image symbolImage = GetSymbolImage(column, imageIndex);
        if (symbolImage == null)
        {
            return;
        }

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
            SetReelSymbols(column, symbols, false);
            ApplyStoppedReelLayout(column);

            reelTransforms[column].localPosition = new Vector3(
                reelTransforms[column].localPosition.x,
                middlePosition,
                0f
            );
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
    /// Restores the standard three-row St. Patrick's Gold reel layout after a reel stops.
    /// The outer images are animation buffers and the middle three images are the result rows.
    /// </summary>
    private void ApplyStoppedReelLayout(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= reelImagesList.Count) return;

        if (reelLayoutGroups != null &&
            columnIndex < reelLayoutGroups.Length &&
            reelLayoutGroups[columnIndex] != null)
        {
            reelLayoutGroups[columnIndex].spacing = defaultSpacing;
        }

        SetImageAlpha(columnIndex, 0, 0f);
        SetImageAlpha(columnIndex, 1, 0f);
        SetImageAlpha(columnIndex, 2, 1f);
        SetImageAlpha(columnIndex, 3, 1f);
        SetImageAlpha(columnIndex, 4, 1f);
        SetImageAlpha(columnIndex, 5, 0f);
        SetImageAlpha(columnIndex, 6, 0f);
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
