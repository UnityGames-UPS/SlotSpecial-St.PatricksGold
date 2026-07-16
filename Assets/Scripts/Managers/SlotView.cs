using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Symbol Sprites - Assign by Name")]
    [SerializeField] private Sprite spriteRedTriple;         // ID: 0
    [SerializeField] private Sprite spritePurpleDouble;      // ID: 1
    [SerializeField] private Sprite spriteBlueWild;          // ID: 2
    [SerializeField] private Sprite spriteRed7;              // ID: 3
    [SerializeField] private Sprite spriteGolden7;           // ID: 4
    [SerializeField] private Sprite spriteBlack7;            // ID: 5
    [SerializeField] private Sprite spriteDoubleBar;         // ID: 6
    [SerializeField] private Sprite spriteBar;               // ID: 7
    [SerializeField] private Sprite spriteBlank;             // ID: 8

    // Internal array built from named sprites
    private Sprite[] symbolSprites;

    [Header("Reel Containers")]
    [SerializeField] private Transform[] reelTransforms;

    [Header("Reel Images - 7 images per reel")]
    [SerializeField] private List<ReelImages> reelImagesList;

    [Header("Spin Settings")]
    [SerializeField] private float symbolHeight = 100f;
    [SerializeField] private float spinSpeed = 0.05f;
    [SerializeField] private float reelStopStagger = 0.12f;

    [Header("Start Animation Settings")]
    [SerializeField] private float anticipationUpDistance = 30f;
    [SerializeField] private float anticipationUpDuration = 0.15f;

    [Header("Stop Animation Settings")]
    [SerializeField] private float stopOvershootDistance = 50f;
    [SerializeField] private float stopOvershootDuration = 0.15f;
    [SerializeField] private float stopBounceBackDuration = 0.25f;

    [Header("Quick Spin Settings")]
    [SerializeField] private float quickStopStagger = 0.06f;
    [SerializeField] private float quickStopOvershoot = 20f;
    [SerializeField] private float quickStopDuration = 0.2f;
    [SerializeField] private int minSpinCyclesBeforeStop = 3;

    [Header("Win Animation Settings")]
    [SerializeField] private float winSymbolLoopDuration = 1.5f;

    [Header("Blank Symbol Settings")]
    [SerializeField] private int blankSymbolId = 8;
    [SerializeField] private float blankSpacingValue = -100f;
    [SerializeField] private float blankMiddleYOffset = -160f;
    [SerializeField] private float blankMiddleSpacingValue = 60f;
    [SerializeField] private float blankTopBottomSpacingValue = 20f;
    [SerializeField] private float defaultSpacing = 0f;



    private float middlePosition = 0f;
    private float cycleDistance;


    private List<Tween> spinTweens = new List<Tween>();
    private List<Tween> winTweens = new List<Tween>();
    private List<Tween> spacingTweens = new List<Tween>();
    private List<int> reelCycleCount = new List<int>();
    private Coroutine winAnimationCoroutine;
    private VerticalLayoutGroup[] reelLayoutGroups;
    private BlankScenario[] currentBlankScenarios;
    private float[] startSpinYPositions;
    private float[] reelCycleProgress;
    private float[] reelAnticipationOffset;

    private bool[] isPreparingToStop;
    private List<int>[] reelTargetSymbols;
    private BlankScenario[] reelTargetScenarios;
    private float[] reelTargetYPositions;
    private bool[] reelIsQuickStop;


    internal List<List<int>> currentDisplayMatrix;

    private bool isSpinning;

    #region Initialization

    private void Start()
    {
        BuildSymbolSpriteArray();
        InitializeReels();
    }

    private void BuildSymbolSpriteArray()
    {
        symbolSprites = new Sprite[9];
        symbolSprites[0] = spriteRedTriple;
        symbolSprites[1] = spritePurpleDouble;
        symbolSprites[2] = spriteBlueWild;
        symbolSprites[3] = spriteRed7;
        symbolSprites[4] = spriteGolden7;
        symbolSprites[5] = spriteBlack7;
        symbolSprites[6] = spriteDoubleBar;
        symbolSprites[7] = spriteBar;
        symbolSprites[8] = spriteBlank;

        // Validate
        for (int i = 0; i < symbolSprites.Length; i++)
        {
            if (symbolSprites[i] == null)
            {
                Debug.LogError($"[SlotView] Symbol sprite at index {i} is not assigned in inspector!");
            }
        }
    }

    private void InitializeReels()
    {
        cycleDistance = symbolHeight;

        middlePosition = 0f;

        currentDisplayMatrix = new List<List<int>>();
        int defaultCols = 3;
        int defaultRows = 3;
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
        currentBlankScenarios = new BlankScenario[reelTransforms.Length];
        startSpinYPositions = new float[reelTransforms.Length];
        reelCycleProgress = new float[reelTransforms.Length];
        reelAnticipationOffset = new float[reelTransforms.Length];
        
        isPreparingToStop = new bool[reelTransforms.Length];
        reelTargetSymbols = new List<int>[reelTransforms.Length];
        reelTargetScenarios = new BlankScenario[reelTransforms.Length];
        reelTargetYPositions = new float[reelTransforms.Length];
        reelIsQuickStop = new bool[reelTransforms.Length];
        spacingTweens.Clear();
        for (int i = 0; i < reelTransforms.Length; i++)
        {
            spacingTweens.Add(null);
            if (reelTransforms[i] != null)
            {
                reelLayoutGroups[i] = reelTransforms[i].GetComponent<VerticalLayoutGroup>();
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

            // Initialize scenario tracking for this reel
            if (currentBlankScenarios != null && col < currentBlankScenarios.Length)
            {
                currentBlankScenarios[col] = BlankScenario.NoBlanks;
            }
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

        int expectedRows = gameManager?.gameConfig != null ? gameManager.gameConfig.rowCount : 3;
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

        int maxSymbolId = gameManager?.gameConfig != null ? gameManager.gameConfig.symbols.Count : 9;

        // Determine if top visible slot (row 0) is blank
        bool isTopBlank = visibleSymbolIds[0] == blankSymbolId;
        int topSpriteId = isTopBlank ? GetRandomNonBlankSymbolId(maxSymbolId) : blankSymbolId;

        // Fill 2 buffer images above the visible area (indices 0 and 1)
        reel.images[0].sprite = GetSymbolSprite(topSpriteId);
        reel.images[1].sprite = GetSymbolSprite(topSpriteId);

        // Determine if bottom visible slot (row 2) is blank
        bool isBottomBlank = visibleSymbolIds[2] == blankSymbolId;
        int bottomSpriteId = isBottomBlank ? GetRandomNonBlankSymbolId(maxSymbolId) : blankSymbolId;

        // Fill 2 buffer images below the visible area (indices 5 and 6)
        reel.images[5].sprite = GetSymbolSprite(bottomSpriteId);
        reel.images[6].sprite = GetSymbolSprite(bottomSpriteId);

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

    internal void StartSpin()
    {
        if (isSpinning) return;

        isSpinning = true;
        KillAllTweens();

        for (int i = 0; i < reelTransforms.Length; i++)
        {
            if (i < reelCycleCount.Count) reelCycleCount[i] = 0;
            if (isPreparingToStop != null && i < isPreparingToStop.Length) isPreparingToStop[i] = false;
        }

        int cols = currentDisplayMatrix != null ? currentDisplayMatrix.Count : 3;
        int maxSymbolId = gameManager?.gameConfig != null ? gameManager.gameConfig.symbols.Count : 9;

        for (int col = 0; col < cols; col++)
        {
            // Record current stopped Y position as the starting Y position for this spin
            if (col < reelTransforms.Length && reelTransforms[col] != null)
            {
                startSpinYPositions[col] = reelTransforms[col].localPosition.y;
            }
            else
            {
                startSpinYPositions[col] = 0f;
            }

            // Buffer images (0, 1, 5, 6) already hold valid sprites from the end of the previous spin.
            // Keeping them as-is prevents visual popping/glitches when the spin starts.

            // Set all 7 images of each reel to alpha 1 at start of spin so they are visible during movement
            for (int i = 0; i < 7; i++)
            {
                SetImageAlpha(col, i, 1f);
            }

            InitializeTweening(col);
        }
    }

    private void InitializeTweening(int columnIndex)
    {
        if (columnIndex >= reelTransforms.Length) return;

        Transform slotTransform = reelTransforms[columnIndex];
        reelAnticipationOffset[columnIndex] = 0f;

        while (spinTweens.Count <= columnIndex) spinTweens.Add(null);
        if (spinTweens[columnIndex] != null) { spinTweens[columnIndex].Kill(); spinTweens[columnIndex] = null; }

        // Quick 2-step bounce relative to the dynamically updating startSpinYPositions
        Sequence startSeq = DOTween.Sequence();
        startSeq.Append(
            DOTween.To(() => reelAnticipationOffset[columnIndex], x => reelAnticipationOffset[columnIndex] = x, anticipationUpDistance, anticipationUpDuration)
                .SetEase(Ease.OutQuad)
        );
        startSeq.Append(
            DOTween.To(() => reelAnticipationOffset[columnIndex], x => reelAnticipationOffset[columnIndex] = x, 0f, anticipationUpDuration * 0.5f)
                .SetEase(Ease.InQuad)
        );
        startSeq.OnUpdate(() => {
            if (slotTransform != null)
            {
                slotTransform.localPosition = new Vector3(
                    slotTransform.localPosition.x,
                    startSpinYPositions[columnIndex] + reelAnticipationOffset[columnIndex],
                    0
                );
            }
        });
        startSeq.OnComplete(() => { if (isSpinning) StartReelCycle(columnIndex); });
        spinTweens[columnIndex] = startSeq;
        startSeq.Play();
    }

    private void StartReelCycle(int columnIndex)
    {
        if (columnIndex >= reelTransforms.Length) return;
        if (!isSpinning) return;

        Transform slotTransform = reelTransforms[columnIndex];
        
        reelCycleProgress[columnIndex] = 0f;

        float currentSpeed = spinSpeed;

        Sequence cycleSequence = DOTween.Sequence();

        // Tween reelCycleProgress from 0 to -cycleDistance, updating position using current startSpinYPositions
        cycleSequence.Append(
            DOTween.To(() => reelCycleProgress[columnIndex], x => reelCycleProgress[columnIndex] = x, -cycleDistance, currentSpeed)
                .SetEase(Ease.Linear)
                .OnUpdate(() => {
                    if (slotTransform != null)
                    {
                        slotTransform.localPosition = new Vector3(
                            slotTransform.localPosition.x,
                            startSpinYPositions[columnIndex] + reelCycleProgress[columnIndex],
                            0
                        );
                    }
                })
        );

        cycleSequence.OnComplete(() => {
            if (isSpinning)
            {
                if (columnIndex < reelCycleCount.Count)
                {
                    reelCycleCount[columnIndex]++;
                }

                if (isPreparingToStop[columnIndex])
                {
                    isPreparingToStop[columnIndex] = false;
                    TriggerActualReelStop(columnIndex);
                }
                else
                {
                    CycleReelSymbols(columnIndex);

                    // Smoothly transition layout group spacing and Y offset to 0f after the first cycle completes
                    // (occurs in mid-spin at top speed, making the transition completely invisible)
                    if (reelCycleCount[columnIndex] == 1)
                    {
                        var layoutGroup = reelLayoutGroups[columnIndex];
                        if (layoutGroup != null)
                        {
                            if (spacingTweens[columnIndex] != null)
                            {
                                spacingTweens[columnIndex].Kill();
                            }
                            
                            int colIndex = columnIndex;
                            Sequence seq = DOTween.Sequence();
                            seq.Join(DOTween.To(() => layoutGroup.spacing, x => layoutGroup.spacing = x, 0f, 0.3f));
                            seq.Join(DOTween.To(() => startSpinYPositions[colIndex], x => startSpinYPositions[colIndex] = x, 0f, 0.3f));
                            seq.SetEase(Ease.OutQuad);
                            spacingTweens[columnIndex] = seq;
                        }
                    }

                    StartReelCycle(columnIndex);
                }
            }
        });

        cycleSequence.Play();

        if (spinTweens.Count <= columnIndex)
            spinTweens.Add(cycleSequence);
        else
            spinTweens[columnIndex] = cycleSequence;
    }

    private void CycleReelSymbols(int columnIndex)
    {
        var reel = reelImagesList[columnIndex];
        if (reel.images == null || reel.images.Count != 7) return;

        for (int i = 6; i > 0; i--)
        {
            reel.images[i].sprite = reel.images[i - 1].sprite;
        }

        int maxSymbolId = gameManager?.gameConfig != null ? gameManager.gameConfig.symbols.Count : 9;
        
        // Always pick a random non-blank symbol during active spin cycle
        int randomSymbolId = GetRandomNonBlankSymbolId(maxSymbolId);

        reel.images[0].sprite = GetSymbolSprite(randomSymbolId);
    }

    private int GetRandomNonBlankSymbolId(int maxSymbolId)
    {
        int id = Random.Range(0, maxSymbolId);
        int attempts = 0;
        while (id == blankSymbolId && attempts < 10)
        {
            id = Random.Range(0, maxSymbolId);
            attempts++;
        }
        if (id == blankSymbolId)
        {
            id = 0; // Fallback to 0 (RedTriple) if all attempts are blank
        }
        return id;
    }

    #endregion

    #region Stop Spin

    internal void StopSpin(List<List<int>> resultMatrix, System.Action onComplete)
    {
        if (!isSpinning)
        {
            currentDisplayMatrix = resultMatrix;
            int cols = resultMatrix.Count;
            for (int col = 0; col < cols; col++)
            {
                SetReelSymbols(col, resultMatrix[col], false);
                BlankScenario scenario = DetectBlankScenario(resultMatrix[col]);
                ApplyBlankScenario(col, scenario, resultMatrix[col]);
                if (col < reelTransforms.Length && reelTransforms[col] != null)
                {
                    float targetY = GetTargetYForScenario(scenario);
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        targetY,
                        0
                    );
                }
            }
            
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, onComplete, false));
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

        // Start stopping each reel with stagger
        for (int col = 0; col < cols; col++)
        {
            float delay = col * stagger;
            StartCoroutine(StopSingleReel(col, resultMatrix[col], delay, isQuickStop));
        }

        // Calculate longest stop time
        float longestStopTime;
        if (isQuickStop)
        {
            longestStopTime = ((cols - 1) * stagger) + quickStopDuration;
        }
        else
        {
            longestStopTime = ((cols - 1) * stagger) + stopOvershootDuration + stopBounceBackDuration;
        }

        yield return new WaitForSeconds(longestStopTime);

        isSpinning = false;

        onComplete?.Invoke();
    }

    private IEnumerator StopSingleReel(int columnIndex, List<int> targetSymbols, float delay, bool isQuickStop)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Detect the target blank scenario and its stop parameters
        BlankScenario scenario = DetectBlankScenario(targetSymbols);
        float targetSpacing = GetTargetSpacingForScenario(scenario);
        float targetY = GetTargetYForScenario(scenario);

        // Apply stopped spacing IMMEDIATELY while spinning (one cycle before bounce).
        // Since it happens while spinning at top speed, the layout adjustment is completely invisible to the user.
        var layoutGroup = reelLayoutGroups[columnIndex];
        if (layoutGroup != null)
        {
            layoutGroup.spacing = targetSpacing;
        }
        startSpinYPositions[columnIndex] = targetY;

        // Store stopping parameters for the actual stop trigger on cycle complete
        reelTargetSymbols[columnIndex] = targetSymbols;
        reelTargetScenarios[columnIndex] = scenario;
        reelTargetYPositions[columnIndex] = targetY;
        reelIsQuickStop[columnIndex] = isQuickStop;
        isPreparingToStop[columnIndex] = true;
    }

    private void TriggerActualReelStop(int columnIndex)
    {
        if (columnIndex >= reelTransforms.Length) return;

        if (columnIndex < spinTweens.Count && spinTweens[columnIndex] != null)
        {
            spinTweens[columnIndex].Kill();
        }

        Transform slotTransform = reelTransforms[columnIndex];

        var targetSymbols = reelTargetSymbols[columnIndex];
        var scenario = reelTargetScenarios[columnIndex];
        var isQuickStop = reelIsQuickStop[columnIndex];
        float scenarioTargetY = reelTargetYPositions[columnIndex];

        // Load final result symbols (which can include blank symbols)
        SetReelSymbols(columnIndex, targetSymbols, false);

        // Apply blank scenario details (like sprite overrides for special scenarios)
        ApplyBlankScenario(columnIndex, scenario, targetSymbols);

        // Snap to target position (visually identical since cycle completes at -cycleDistance phase)
        slotTransform.localPosition = new Vector3(
            slotTransform.localPosition.x,
            scenarioTargetY,
            0
        );

        // ── Play reel-stop sound immediately when symbols lock in ──────────
        bool isLastReel = (columnIndex == reelTransforms.Length - 1);


        // ──────────────────────────────────────────────────────────────────

        if (isQuickStop)
        {
            Sequence quickStopSequence = DOTween.Sequence();

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY - quickStopOvershoot, quickStopDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY, quickStopDuration * 0.7f)
                    .SetEase(Ease.OutQuad)
            );

            spinTweens[columnIndex] = quickStopSequence;
        }
        else
        {
            Sequence stopSequence = DOTween.Sequence();

            stopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY - stopOvershootDistance, stopOvershootDuration)
                    .SetEase(Ease.OutQuad)
            );

            stopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY, stopBounceBackDuration)
                    .SetEase(Ease.OutQuad)
            );

            spinTweens[columnIndex] = stopSequence;
        }
    }

    #endregion

    #region Quick Spin

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
                    BlankScenario scenario = DetectBlankScenario(resultMatrix[col]);
                    ApplyBlankScenario(col, scenario, resultMatrix[col]);
                    float targetY = GetTargetYForScenario(scenario);
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        targetY,
                        0
                    );
                }
            }
            
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, onComplete, true));
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

        KillWinTweens();
        winAnimationCoroutine = StartCoroutine(PlayWinLinesSequentially(winLines, onComplete));
    }

    private IEnumerator PlayWinLinesSequentially(List<WinLine> winLines, System.Action onComplete)
    {
        bool skipScreen = false;
        List<int> prevPositions = null;

        bool isAuto = (gameManager != null && gameManager.isAutoPlaying);

        if (isAuto)
        {
            // Autoplay: 1 loop per win line, all win lines played exactly once, then call onComplete
            foreach (var winLine in winLines)
            {
                if (winLine.positions == null || winLine.positions.Count == 0) continue;

                if (prevPositions != null)
                {
                    KillWinTweens(false);
                    int cols = gameManager?.gameConfig != null ? gameManager.gameConfig.reelCount : 3;
                    foreach (int flatIdx in prevPositions)
                    {
                        int r = flatIdx / cols;
                        int c = flatIdx % cols;
                        ResetSymbolScale(c, r);
                    }
                }

                float singleDuration = GetWinLineAnimationDuration(winLine);
                float lineDuration = skipScreen ? 0.5f : singleDuration;

                foreach (int flatIndex in winLine.positions)
                {
                    int cols = gameManager?.gameConfig != null ? gameManager.gameConfig.reelCount : 3;
                    int rows = gameManager?.gameConfig != null ? gameManager.gameConfig.rowCount : 3;
                    int row = flatIndex / cols;
                    int col = flatIndex % cols;

                    if (col < 0 || col >= cols || row < 0 || row >= rows) continue;
                    AnimateWinSymbol(col, row);
                }

                prevPositions = new List<int>(winLine.positions);
                yield return new WaitForSeconds(lineDuration);
            }

            KillWinTweens(false);
            onComplete?.Invoke();
        }
        else
        {
            // Normal play
            if (winLines.Count == 1)
            {
                // Only 1 win line: trigger onComplete immediately and play infinitely
                onComplete?.Invoke();

                var winLine = winLines[0];
                if (winLine.positions != null && winLine.positions.Count > 0)
                {
                    foreach (int flatIndex in winLine.positions)
                    {
                        int cols = gameManager?.gameConfig != null ? gameManager.gameConfig.reelCount : 3;
                        int rows = gameManager?.gameConfig != null ? gameManager.gameConfig.rowCount : 3;
                        int row = flatIndex / cols;
                        int col = flatIndex % cols;

                        if (col < 0 || col >= cols || row < 0 || row >= rows) continue;
                        AnimateWinSymbol(col, row);
                    }
                }

                while (true)
                {
                    yield return null;
                }
            }
            else
            {
                // Multiple win lines: play infinitely in a loop, each line playing 3 loops
                bool onCompleteCalled = false;
                while (true)
                {
                    foreach (var winLine in winLines)
                    {
                        if (winLine.positions == null || winLine.positions.Count == 0) continue;

                        if (prevPositions != null)
                        {
                            KillWinTweens(false);
                            int cols = gameManager?.gameConfig != null ? gameManager.gameConfig.reelCount : 3;
                            foreach (int flatIdx in prevPositions)
                            {
                                int r = flatIdx / cols;
                                int c = flatIdx % cols;
                                ResetSymbolScale(c, r);
                            }
                        }

                        float singleDuration = GetWinLineAnimationDuration(winLine);
                        float lineDuration = skipScreen ? 0.5f : singleDuration * 3f;

                        foreach (int flatIndex in winLine.positions)
                        {
                            int cols = gameManager?.gameConfig != null ? gameManager.gameConfig.reelCount : 3;
                            int rows = gameManager?.gameConfig != null ? gameManager.gameConfig.rowCount : 3;
                            int row = flatIndex / cols;
                            int col = flatIndex % cols;

                            if (col < 0 || col >= cols || row < 0 || row >= rows) continue;
                            AnimateWinSymbol(col, row);
                        }

                        prevPositions = new List<int>(winLine.positions);
                        yield return new WaitForSeconds(lineDuration);
                    }

                    // Call onComplete after the first full cycle of win lines is played
                    if (!onCompleteCalled)
                    {
                        onCompleteCalled = true;
                        onComplete?.Invoke();
                    }
                }
            }
        }
    }

    private void ResetSymbolScale(int col, int row)
    {
        if (col >= reelImagesList.Count) return;
        var reel = reelImagesList[col];
        if (reel.images == null) return;
        int visualRow = GetVisualRow(col, row);
        int imageIndex = 2 + visualRow;
        if (imageIndex >= reel.images.Count) return;
        if (reel.images[imageIndex] != null)
        {
            ResetSymbolAnimation(reel.images[imageIndex], col, row);
        }
        // Re-apply blank alpha if this column has an active blank scenario
        ReapplyBlankAlphaForColumn(col);
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

        PlaySymbolAnimation(symbolImage);
    }

    private void KillWinTweens(bool stopCoroutine = true)
    {
        foreach (var tween in winTweens)
        {
            tween?.Kill();
        }
        winTweens.Clear();

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
                    var image = reel.images[imageIndex];
                    if (image != null)
                    {
                        int row = imageIndex - 2;
                        ResetSymbolAnimation(image, col, row);
                    }
                }
            }
        }

        // Re-apply blank scenarios after restoring alphas
        ReapplyCurrentBlankScenarios();
    }

    private int GetVisualRow(int col, int row)
    {
        if (currentBlankScenarios != null && col < currentBlankScenarios.Length)
        {
            if (currentBlankScenarios[col] == BlankScenario.OneBlankMiddle && row == 2)
            {
                return 1;
            }
        }
        return row;
    }

    private void PlaySymbolAnimation(Image symbolImage)
    {
        if (symbolImage == null) return;

        symbolImage.DOKill();
        symbolImage.transform.localScale = Vector3.one;
        Tween tween = symbolImage.transform
            .DOScale(1.12f, 0.35f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        winTweens.Add(tween);
    }

    private void ResetSymbolAnimation(Image symbolImage, int col, int row)
    {
        if (symbolImage == null) return;

        symbolImage.DOKill();
        symbolImage.transform.localScale = Vector3.one;
        Color c = symbolImage.color;
        symbolImage.color = new Color(c.r, c.g, c.b, 1f);
        symbolImage.enabled = true;
    }

    private float GetWinLineAnimationDuration(WinLine winLine)
    {
        return winSymbolLoopDuration;
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



    private void KillAllTweens()
    {
        foreach (var tween in spinTweens)
        {
            tween?.Kill();
        }
        spinTweens.Clear();

        foreach (var tween in spacingTweens)
        {
            tween?.Kill();
        }
        for (int i = 0; i < spacingTweens.Count; i++)
        {
            spacingTweens[i] = null;
        }

        KillWinTweens();
    }

    #endregion

    #region Blank Symbol Handling

    private enum BlankScenario
    {
        NoBlanks,          // Scenario 1: No blanks
        AllBlank,          // Scenario 2: All 3 positions blank
        TwoBlankTop,       // Scenario 3: Top 2 blank (row 0, 1)
        TwoBlankBottom,    // Scenario 4: Bottom 2 blank (row 1, 2)
        TwoBlankTopBottom, // Scenario 5: Top and bottom blank (row 0, 2)
        OneBlankTop,       // Scenario 6: Top blank (row 0)
        OneBlankBottom,    // Scenario 7: Bottom blank (row 2)
        OneBlankMiddle     // Scenario 8: Middle blank (row 1)
    }

    /// <summary>
    /// Analyzes a column's symbol IDs to determine which blank scenario applies.
    /// </summary>
    private BlankScenario DetectBlankScenario(List<int> columnSymbols)
    {
        if (columnSymbols == null || columnSymbols.Count != 3)
            return BlankScenario.NoBlanks;

        bool top = columnSymbols[0] == blankSymbolId;
        bool mid = columnSymbols[1] == blankSymbolId;
        bool bot = columnSymbols[2] == blankSymbolId;

        int blankCount = (top ? 1 : 0) + (mid ? 1 : 0) + (bot ? 1 : 0);

        if (blankCount == 0) return BlankScenario.NoBlanks;
        if (blankCount == 3) return BlankScenario.AllBlank;

        if (blankCount == 2)
        {
            if (top && mid) return BlankScenario.TwoBlankTop;
            if (mid && bot) return BlankScenario.TwoBlankBottom;
            if (top && bot) return BlankScenario.TwoBlankTopBottom;
        }

        // blankCount == 1
        if (top) return BlankScenario.OneBlankTop;
        if (bot) return BlankScenario.OneBlankBottom;
        return BlankScenario.OneBlankMiddle;
    }

    /// <summary>
    /// Universal method that applies spacing, alpha, and sprite overrides based on the blank scenario.
    /// Called after SetReelSymbols to override blank positions.
    /// </summary>
    private void ApplyBlankScenario(int columnIndex, BlankScenario scenario, List<int> targetSymbols)
    {
        if (columnIndex >= reelTransforms.Length || columnIndex >= reelImagesList.Count) return;

        var reel = reelImagesList[columnIndex];
        VerticalLayoutGroup layoutGroup = (reelLayoutGroups != null && columnIndex < reelLayoutGroups.Length)
            ? reelLayoutGroups[columnIndex] : null;

        // Track current scenario for re-application after win animations
        if (currentBlankScenarios != null && columnIndex < currentBlankScenarios.Length)
        {
            currentBlankScenarios[columnIndex] = scenario;
        }

        // Always hide buffer images (indices 0, 1, 5, 6)
        SetImageAlpha(columnIndex, 0, 0f);
        SetImageAlpha(columnIndex, 1, 0f);
        SetImageAlpha(columnIndex, 5, 0f);
        SetImageAlpha(columnIndex, 6, 0f);

        // Reset visible images to full alpha first
        SetImageAlpha(columnIndex, 2, 1f);
        SetImageAlpha(columnIndex, 3, 1f);
        SetImageAlpha(columnIndex, 4, 1f);

        switch (scenario)
        {
            case BlankScenario.NoBlanks: // Scenario 1
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                break;

            case BlankScenario.AllBlank: // Scenario 2
                if (layoutGroup != null) layoutGroup.spacing = defaultSpacing;
                SetImageAlpha(columnIndex, 2, 0f);
                SetImageAlpha(columnIndex, 3, 0f);
                SetImageAlpha(columnIndex, 4, 0f);
                break;

            case BlankScenario.TwoBlankTop: // Scenario 3
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 2, 0f);
                SetImageAlpha(columnIndex, 3, 0f);
                break;

            case BlankScenario.TwoBlankBottom: // Scenario 4
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 3, 0f);
                SetImageAlpha(columnIndex, 4, 0f);
                break;

            case BlankScenario.TwoBlankTopBottom: // Scenario 5
                if (layoutGroup != null) layoutGroup.spacing = blankTopBottomSpacingValue;
                // Show random non-blank sprites at blank positions (index 2 = row 0, index 4 = row 2) if not already set
                Sprite blankSprite = GetSymbolSprite(blankSymbolId);
                if (reel.images[2].sprite == null || reel.images[2].sprite == blankSprite)
                {
                    reel.images[2].sprite = GetRandomNonBlankSprite();
                }
                if (reel.images[4].sprite == null || reel.images[4].sprite == blankSprite)
                {
                    reel.images[4].sprite = GetRandomNonBlankSprite();
                }
                break;

            case BlankScenario.OneBlankTop: // Scenario 6
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 2, 0f);
                break;

            case BlankScenario.OneBlankBottom: // Scenario 7
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 4, 0f);
                break;

            case BlankScenario.OneBlankMiddle: // Scenario 8
                if (layoutGroup != null) layoutGroup.spacing = blankMiddleSpacingValue;
                // Index 3 shows last row result (row 2's symbol)
                if (targetSymbols != null && targetSymbols.Count > 2)
                {
                    reel.images[3].sprite = GetSymbolSprite(targetSymbols[2]);
                }
                break;
        }
    }

    /// <summary>
    /// Applies the blank scenario smoothly using DOTween to transition spacing and alphas over stopDuration.
    /// Called during the reel's deceleration and bounce stop sequence.
    /// </summary>
    private void ApplyBlankScenarioSmooth(int columnIndex, BlankScenario scenario, List<int> targetSymbols, float duration)
    {
        if (columnIndex >= reelTransforms.Length || columnIndex >= reelImagesList.Count) return;

        var reel = reelImagesList[columnIndex];
        VerticalLayoutGroup layoutGroup = (reelLayoutGroups != null && columnIndex < reelLayoutGroups.Length)
            ? reelLayoutGroups[columnIndex] : null;

        // Track current scenario
        if (currentBlankScenarios != null && columnIndex < currentBlankScenarios.Length)
        {
            currentBlankScenarios[columnIndex] = scenario;
        }

        // Apply sprite overrides immediately (Scenario 5 and Scenario 8) so they are visually aligned before stop starts
        if (scenario == BlankScenario.TwoBlankTopBottom)
        {
            Sprite blankSprite = GetSymbolSprite(blankSymbolId);
            if (reel.images[2].sprite == null || reel.images[2].sprite == blankSprite)
            {
                reel.images[2].sprite = GetRandomNonBlankSprite();
            }
            if (reel.images[4].sprite == null || reel.images[4].sprite == blankSprite)
            {
                reel.images[4].sprite = GetRandomNonBlankSprite();
            }
        }
        else if (scenario == BlankScenario.OneBlankMiddle)
        {
            if (targetSymbols != null && targetSymbols.Count > 2)
            {
                reel.images[3].sprite = GetSymbolSprite(targetSymbols[2]);
            }
        }

        // Kill any existing spacing/alpha tweens for this column
        if (spacingTweens[columnIndex] != null)
        {
            spacingTweens[columnIndex].Kill();
        }

        float targetSpacing = GetTargetSpacingForScenario(scenario);

        Sequence stopSeq = DOTween.Sequence();

        // 1. Tween VerticalLayoutGroup spacing to targetSpacing
        if (layoutGroup != null)
        {
            stopSeq.Join(DOTween.To(() => layoutGroup.spacing, x => layoutGroup.spacing = x, targetSpacing, duration));
        }

        stopSeq.SetEase(Ease.OutQuad);
        spacingTweens[columnIndex] = stopSeq;
    }

    private float GetTargetSpacingForScenario(BlankScenario scenario)
    {
        switch (scenario)
        {
            case BlankScenario.NoBlanks:
            case BlankScenario.TwoBlankTop:
            case BlankScenario.TwoBlankBottom:
            case BlankScenario.OneBlankTop:
            case BlankScenario.OneBlankBottom:
                return blankSpacingValue;
            case BlankScenario.AllBlank:
                return defaultSpacing;
            case BlankScenario.TwoBlankTopBottom:
                return blankTopBottomSpacingValue;
            case BlankScenario.OneBlankMiddle:
                return blankMiddleSpacingValue;
            default:
                return defaultSpacing;
        }
    }

    private float[] GetTargetAlphasForScenario(BlankScenario scenario)
    {
        float[] alphas = new float[7];
        // Buffer images (0, 1, 5, 6) always target 0f alpha when stopped
        alphas[0] = 0f;
        alphas[1] = 0f;
        alphas[5] = 0f;
        alphas[6] = 0f;

        // Visible images (2, 3, 4) target 1f default
        alphas[2] = 1f;
        alphas[3] = 1f;
        alphas[4] = 1f;

        switch (scenario)
        {
            case BlankScenario.AllBlank:
                alphas[2] = 0f;
                alphas[3] = 0f;
                alphas[4] = 0f;
                break;
            case BlankScenario.TwoBlankTop:
                alphas[2] = 0f;
                alphas[3] = 0f;
                break;
            case BlankScenario.TwoBlankBottom:
                alphas[3] = 0f;
                alphas[4] = 0f;
                break;
            case BlankScenario.OneBlankTop:
                alphas[2] = 0f;
                break;
            case BlankScenario.OneBlankBottom:
                alphas[4] = 0f;
                break;
        }
        return alphas;
    }

    /// <summary>
    /// Returns the reel Y position target based on the blank scenario.
    /// Scenario 8 (OneBlankMiddle) shifts the reel down so only indices 2 and 3 are visible.
    /// </summary>
    private float GetTargetYForScenario(BlankScenario scenario)
    {
        switch (scenario)
        {
            case BlankScenario.OneBlankMiddle:
                return middlePosition + blankMiddleYOffset;
            default:
                return middlePosition;
        }
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
            img.color = new Color(c.r, c.g, c.b, 1f); // Bypass alpha changes (transparent sprites handle blanks)
        }
    }

    /// <summary>
    /// Returns a random non-blank sprite for filling blank positions in Scenario 5.
    /// </summary>
    private Sprite GetRandomNonBlankSprite()
    {
        List<int> nonBlankIds = new List<int>();
        for (int i = 0; i < symbolSprites.Length; i++)
        {
            if (i != blankSymbolId && symbolSprites[i] != null)
            {
                nonBlankIds.Add(i);
            }
        }

        if (nonBlankIds.Count == 0) return symbolSprites[0];
        return symbolSprites[nonBlankIds[Random.Range(0, nonBlankIds.Count)]];
    }

    /// <summary>
    /// Resets all reels to default state: full alpha, default spacing, middlePosition Y.
    /// Called at the start of each spin.
    /// </summary>
    private void ResetBlankScenarios()
    {
        int cols = reelImagesList.Count;
        for (int col = 0; col < cols; col++)
        {
            if (col >= reelTransforms.Length) continue;

            // Reset Y position
            Transform slotTransform = reelTransforms[col];
            slotTransform.localPosition = new Vector3(
                slotTransform.localPosition.x,
                middlePosition,
                0
            );

            // Reset spacing
            if (reelLayoutGroups != null && col < reelLayoutGroups.Length && reelLayoutGroups[col] != null)
            {
                reelLayoutGroups[col].spacing = defaultSpacing;
            }

            // Reset all image alphas to 1
            var reel = reelImagesList[col];
            if (reel.images != null)
            {
                for (int i = 0; i < reel.images.Count; i++)
                {
                    if (reel.images[i] != null)
                    {
                        Color c = reel.images[i].color;
                        reel.images[i].color = new Color(c.r, c.g, c.b, 1f);
                    }
                }
            }

            // Reset tracked scenario
            if (currentBlankScenarios != null && col < currentBlankScenarios.Length)
            {
                currentBlankScenarios[col] = BlankScenario.NoBlanks;
            }
        }
    }

    /// <summary>
    /// Re-applies blank scenarios for all reels. Called after win animations restore alphas.
    /// </summary>
    private void ReapplyCurrentBlankScenarios()
    {
        if (currentBlankScenarios == null || currentDisplayMatrix == null) return;
        for (int col = 0; col < currentBlankScenarios.Length && col < currentDisplayMatrix.Count; col++)
        {
            if (currentBlankScenarios[col] != BlankScenario.NoBlanks)
            {
                ApplyBlankScenario(col, currentBlankScenarios[col], currentDisplayMatrix[col]);
            }
            else
            {
                // Even for NoBlanks, re-apply buffer alpha 0
                SetImageAlpha(col, 0, 0f);
                SetImageAlpha(col, 1, 0f);
                SetImageAlpha(col, 5, 0f);
                SetImageAlpha(col, 6, 0f);
            }
        }
    }

    /// <summary>
    /// Re-applies blank alpha for a single column. Called after ResetSymbolScale restores alpha.
    /// </summary>
    private void ReapplyBlankAlphaForColumn(int columnIndex)
    {
        if (currentBlankScenarios == null || columnIndex >= currentBlankScenarios.Length) return;
        if (currentDisplayMatrix == null || columnIndex >= currentDisplayMatrix.Count) return;

        ApplyBlankScenario(columnIndex, currentBlankScenarios[columnIndex], currentDisplayMatrix[columnIndex]);
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
}
