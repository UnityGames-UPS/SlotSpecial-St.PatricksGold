using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    internal const int InfiniteAutoPlayRounds = -1;

    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] private SlotView slotView;

    [Header("Ultra Slot")]
    [SerializeField] private UltraSlotView ultraSlotView;
    [Tooltip("The normal 5x3 SlotHolder object.")]
    [SerializeField] private GameObject normalSlotPanel;
    [Tooltip("The new three-reel UltraWheelSlot object.")]
    [SerializeField] private GameObject ultraSlotPanel;
    [SerializeField, Min(0f)] private float ultraSlotSpinDuration = 2f;
    [SerializeField, Min(0f)] private float ultraTriggerAnimationDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float ultraSlotTransitionDuration = 1.2f;
    [SerializeField, Min(0f)] private float ultraSlotTransitionPadding = 100f;

    [Header("Ultra Wheels")]
    [Tooltip("Wheel 1, represented by Ultra result symbol 1.")]
    [SerializeField] private UltraWheelController redUltraWheel;
    [Tooltip("Wheel 2, represented by Ultra result symbol 2.")]
    [SerializeField] private UltraWheelController blueUltraWheel;
    [Tooltip("Wheel 3, represented by Ultra result symbol 3.")]
    [SerializeField] private UltraWheelController thirdUltraWheel;
    [SerializeField, Min(0f)] private float ultraResultAnimationHoldDuration = 1.2f;

    [Header("Spin Settings")]
    [SerializeField] private float normalSpinDuration = 2.0f;
    [UnityEngine.Serialization.FormerlySerializedAs("quickSpinDuration")]
    [SerializeField] private float fastSpinDuration = 0.75f;

    [Header("Auto Play Settings")]
    [Tooltip("How long an autoplay result remains visible before the next spin starts.")]
    [SerializeField, Min(0f)] private float autoPlayResultHoldDuration = 1f;

    internal StPatricksGoldGameConfig stPatricksGoldConfig;
    internal PlayerData playerData;
    internal ServerSpinResponse latestServerSpinResponse { get; private set; }
    internal SpinResult latestSpinResult { get; private set; }
    internal string latestRawSpinResponse { get; private set; }
    internal ServerUltraBonus latestUltraBonus { get; private set; }
    internal IReadOnlyList<int> latestWinningPaylineIndices => latestWinningPaylineIndicesInternal;

    internal event System.Action<bool> SpinActivityChanged;
    internal event System.Action<SpinSpeed> SpinSpeedChanged;
    internal event System.Action GamePresentationChanged;
    internal event System.Action AutoPlayChanged;
    internal event System.Action UltraWheelsCompleted;

    internal GameState currentState;
    internal SpinSpeed currentSpinSpeed;

    internal int currentBetIndex;
    internal double currentBetAmount;

    internal bool isAutoPlaying;
    internal int autoPlayTotalRounds;
    internal int autoPlayRemainingRounds;

    internal bool isInitialized;
    internal bool initializationFailed;

    private Coroutine spinCoroutine;
    private Coroutine autoPlayCoroutine;
    private Coroutine ultraSlotCoroutine;
    private Coroutine ultraWheelsCoroutine;
    private SpinSpeed activeSpinSpeed;
    private List<List<int>> pendingResultMatrix;
    private SpinResult pendingSpinResult;
    private ServerUltraBonus pendingUltraBonus;
    private List<int> pendingUltraSlotResult;
    private bool manualStopRequested;
    private bool isUltraSlotUnlocked;
    private bool isUltraSlotSpinning;
    private bool hasUltraSlotStarted;
    private bool areUltraWheelsSpinning;
    private bool isUltraSlotTransitioning;
    private RectTransform normalSlotRectTransform;
    private RectTransform ultraSlotRectTransform;
    private Vector2 normalSlotRestingPosition;
    private Vector2 ultraSlotRestingPosition;
    private bool hasCachedUltraSlotLayout;
    private Sequence ultraSlotTransitionSequence;
    private double displayedWinAmount;
    private readonly List<int> latestWinningPaylineIndicesInternal = new List<int>();

    #region Initialization

    private void Awake()
    {
        ResolveUltraSlotReferences();
        CacheUltraSlotLayout();
    }

    private void Start()
    {
        currentState = GameState.Initializing;
        currentSpinSpeed = SpinSpeed.Normal;
        isInitialized = false;
        initializationFailed = false;
        ResetUltraSlotState();
    }

    internal void OnStPatricksGoldConfigReceived(StPatricksGoldGameConfig config, PlayerData player, List<List<int>> initialMatrix)
    {
        stPatricksGoldConfig = config;
        playerData = player;
        currentBetIndex = playerData.currentBetIndex;
        UpdateBetAmount();

        if (initialMatrix != null && slotView != null)
        {
            slotView.SetInitialMatrix(initialMatrix);
        }

        isInitialized = true;
        currentState = GameState.Idle;
        displayedWinAmount = 0;
        latestSpinResult = null;
        pendingSpinResult = null;
        latestWinningPaylineIndicesInternal.Clear();

        ResetUltraSlotState();
        if (config != null && config.isSpecial)
        {
            UnlockUltraSlot(null, "parse-sheet isSpecial flag");
        }

        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        Debug.Log("[GameManager] Game initialized.");
    }

    private void ResolveUltraSlotReferences()
    {
        if (ultraSlotView == null)
        {
            ultraSlotView = FindFirstObjectByType<UltraSlotView>(FindObjectsInactive.Include);
        }

        if (normalSlotPanel == null)
        {
            normalSlotPanel = FindSceneObjectByName("SlotHolder");
        }

        if (ultraSlotPanel == null)
        {
            ultraSlotPanel = FindSceneObjectByName("UltraWheelSlot");
        }

        UltraWheelController[] wheelControllers =
            FindObjectsByType<UltraWheelController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        foreach (UltraWheelController wheelController in wheelControllers)
        {
            if (wheelController == null)
            {
                continue;
            }

            switch (wheelController.WheelNumber)
            {
                case 1 when redUltraWheel == null:
                    redUltraWheel = wheelController;
                    break;
                case 2 when blueUltraWheel == null:
                    blueUltraWheel = wheelController;
                    break;
                case 3 when thirdUltraWheel == null:
                    thirdUltraWheel = wheelController;
                    break;
            }
        }
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform != null &&
                sceneTransform.gameObject.scene.IsValid() &&
                string.Equals(sceneTransform.name, objectName, StringComparison.Ordinal))
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }

    #endregion

    #region Bet Management

    internal bool IncreaseBet()
    {
        if (!CanIncreaseBet()) return false;

        int betCount = stPatricksGoldConfig.availableBets.Count;
        SetBetIndex((currentBetIndex + 1) % betCount);
        return true;
    }

    internal bool DecreaseBet()
    {
        if (!CanDecreaseBet()) return false;

        int betCount = stPatricksGoldConfig.availableBets.Count;
        SetBetIndex((currentBetIndex - 1 + betCount) % betCount);
        return true;
    }

    internal void SetBetIndex(int index)
    {
        if (stPatricksGoldConfig?.availableBets == null || stPatricksGoldConfig.availableBets.Count == 0)
        {
            Debug.LogError("[GameManager] Cannot select a bet because the server supplied no bet values.");
            return;
        }

        currentBetIndex = Mathf.Clamp(index, 0, stPatricksGoldConfig.availableBets.Count - 1);
        UpdateBetAmount();
        GamePresentationChanged?.Invoke();
    }

    private void UpdateBetAmount()
    {
        currentBetAmount = stPatricksGoldConfig.availableBets[currentBetIndex];
    }

    internal bool CanIncreaseBet()
    {
        return CanChangeBet();
    }

    internal bool CanDecreaseBet()
    {
        return CanChangeBet();
    }

    private bool CanChangeBet()
    {
        return isInitialized &&
               currentState == GameState.Idle &&
               !isAutoPlaying &&
               !isUltraSlotUnlocked &&
               stPatricksGoldConfig?.availableBets != null &&
               stPatricksGoldConfig.availableBets.Count > 0;
    }

    #endregion

    #region Spin Control
    
    internal bool RequestSpin()
    {
        if (currentState != GameState.Idle)
        {
            Debug.LogWarning($"[GameManager] Spin ignored because the game state is {currentState}.");
            return false;
        }

        if (!isInitialized || stPatricksGoldConfig == null)
        {
            Debug.LogError("[GameManager] Spin rejected because the game is not initialized.");
            return false;
        }

        if (isUltraSlotUnlocked)
        {
            Debug.LogWarning("[GameManager] Normal spin is locked while the Ultra slot is active.");
            return false;
        }

        if (socketManager == null || !socketManager.isConnected)
        {
            Debug.LogError("[GameManager] Spin rejected because the socket is not connected.");
            return false;
        }

        if (slotView == null)
        {
            Debug.LogError("[GameManager] Spin rejected because SlotView is not assigned.");
            return false;
        }

        if (slotView.IsSpinning())
        {
            Debug.LogWarning("[GameManager] Spin ignored because SlotView is already spinning.");
            return false;
        }

        if (!CanAffordBet())
        {
            Debug.LogWarning(
                $"[GameManager] Spin rejected because balance {GetDisplayedBalance():0.00} " +
                $"is lower than total bet {GetDisplayedTotalBetAmount():0.00}.");
            return false;
        }

        return StartSpin();
    }

    internal bool RequestStop()
    {
        if (!CanRequestStop())
        {
            Debug.LogWarning($"[GameManager] Stop ignored because the game state is {currentState} or a stop is already pending.");
            return false;
        }

        manualStopRequested = true;

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        Debug.Log("[GameManager] Manual stop requested. Reels will stop as soon as valid server data is available.");
        return true;
    }

    private bool StartSpin()
    {
        pendingResultMatrix = null;
        pendingSpinResult = null;
        pendingUltraBonus = null;
        manualStopRequested = false;
        displayedWinAmount = 0;
        latestWinningPaylineIndicesInternal.Clear();
        currentState = GameState.Spinning;
        activeSpinSpeed = currentSpinSpeed;

        slotView.StartSpin(activeSpinSpeed);
        SpinActivityChanged?.Invoke(true);
        GamePresentationChanged?.Invoke();

        if (spinCoroutine != null)
            StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(SpinRoutine());

        if (!socketManager.SendSpinRequest(currentBetIndex))
        {
            FailActiveSpin("The spin request could not be sent.");
            return false;
        }

        return true;
    }

    private IEnumerator SpinRoutine()
    {
        float normalStopReadyTime = Time.time + GetSpinDuration(activeSpinSpeed);

        while (true)
        {
            if (currentState != GameState.Spinning)
            {
                yield break;
            }

            bool stopTimeReached = manualStopRequested || Time.time >= normalStopReadyTime;

            if (stopTimeReached && pendingResultMatrix != null)
            {
                break;
            }

            yield return null;
        }

        currentState = GameState.Stopping;
        List<List<int>> resultMatrix = pendingResultMatrix;
        bool showResultImmediately = activeSpinSpeed == SpinSpeed.SkipSpin;
        bool useFastStop = manualStopRequested || activeSpinSpeed == SpinSpeed.FastSpin;
        spinCoroutine = null;

        if (showResultImmediately)
        {
            slotView.ShowServerResultImmediately(resultMatrix, OnReelsStoppedComplete);
        }
        else if (useFastStop)
        {
            slotView.QuickStop(resultMatrix, OnReelsStoppedComplete);
        }
        else
        {
            slotView.StopSpin(resultMatrix, OnReelsStoppedComplete);
        }
    }

    private void OnReelsStoppedComplete()
    {
        pendingResultMatrix = null;
        manualStopRequested = false;

        SpinResult completedResult = pendingSpinResult;
        pendingSpinResult = null;

        if (completedResult == null)
        {
            FailActiveSpin("The reels stopped without a converted SpinResult.");
            return;
        }

        latestSpinResult = completedResult;
        displayedWinAmount = completedResult.winAmount;
        if (completedResult.playerData != null)
        {
            playerData = completedResult.playerData;
            currentBetIndex = playerData.currentBetIndex;
            UpdateBetAmount();
        }

        latestWinningPaylineIndicesInternal.Clear();
        latestWinningPaylineIndicesInternal.AddRange(GetConfiguredWinningPaylineIndices(completedResult));

        currentState = GameState.Idle;

        ServerUltraBonus completedUltraBonus = pendingUltraBonus;
        pendingUltraBonus = null;
        bool ultraTriggered = IsUltraBonusTriggered(completedUltraBonus);
        if (ultraTriggered && isAutoPlaying)
        {
            StopAutoPlay();
        }

        bool shouldContinueAutoPlay = false;
        if (!ultraTriggered && isAutoPlaying)
        {
            if (autoPlayRemainingRounds != InfiniteAutoPlayRounds)
            {
                autoPlayRemainingRounds = Mathf.Max(0, autoPlayRemainingRounds - 1);
            }

            if (autoPlayRemainingRounds == 0)
            {
                StopAutoPlay();
            }
            else if (!CanAffordBet())
            {
                Debug.LogWarning("[GameManager] Autoplay stopped because the balance is insufficient for another spin.");
                StopAutoPlay();
            }
            else
            {
                shouldContinueAutoPlay = true;
                AutoPlayChanged?.Invoke();
            }
        }

        if (ultraTriggered)
        {
            UnlockUltraSlot(completedUltraBonus, "server ultraBonus/features flag");
        }

        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        // The server win lines have already been converted to flat positions
        // for the visible 5x3 grid. Start their symbol animation only after all
        // reels have settled so the pulse is applied to the displayed result.
        bool hasWinLines = !ultraTriggered &&
                           slotView != null &&
                           completedResult.winLines != null &&
                           completedResult.winLines.Count > 0;
        if (hasWinLines)
        {
            slotView.ShowWinLineAnimation(
                completedResult.winLines,
                shouldContinueAutoPlay ? QueueNextAutoPlaySpin : null
            );
        }

        Debug.Log("[GameManager] SpinResult applied. Round returned to Idle.");

        if (!hasWinLines && shouldContinueAutoPlay)
        {
            QueueNextAutoPlaySpin();
        }
    }

    private float GetSpinDuration(SpinSpeed spinSpeed)
    {
        switch (spinSpeed)
        {
            case SpinSpeed.FastSpin:
                return Mathf.Max(0f, fastSpinDuration);
            case SpinSpeed.SkipSpin:
                return 0f;
            default:
                return Mathf.Max(0f, normalSpinDuration);
        }
    }

    internal bool CanRequestSpin()
    {
        return isInitialized &&
               currentState == GameState.Idle &&
               !isAutoPlaying &&
               !isUltraSlotUnlocked &&
               socketManager != null &&
               socketManager.isConnected &&
               slotView != null &&
               !slotView.IsSpinning() &&
               CanAffordBet();
    }

    internal bool CanRequestStop()
    {
        return currentState == GameState.Spinning &&
               !manualStopRequested &&
               slotView != null &&
               slotView.IsSpinning();
    }

    internal bool IsSpinRoundActive()
    {
        return IsSpinning();
    }

    internal void OnSpinResponseReceived(
        ServerSpinResponse serverResponse,
        string rawJson,
        List<List<int>> resultMatrix)
    {
        StoreLatestSpinResponse(serverResponse, rawJson);

        if (currentState != GameState.Spinning)
        {
            Debug.LogWarning($"[GameManager] Stored a spin response while the game state was {currentState}; it will not affect the reels.");
            return;
        }

        if (resultMatrix == null)
        {
            FailActiveSpin("The server result matrix is null.");
            return;
        }

        if (slotView == null)
        {
            FailActiveSpin("The server result cannot be displayed because SlotView is not assigned.");
            return;
        }

        if (!slotView.TryValidateResultMatrix(resultMatrix, out string validationError))
        {
            FailActiveSpin($"The server result cannot be displayed: {validationError}");
            return;
        }

        double currentBalance = playerData != null ? playerData.balance : 0;
        SpinResult spinResult = GameDataConverter.ConvertServerResponseToSpinResult(
            serverResponse,
            currentBalance,
            GetDisplayedTotalBetAmount(),
            currentBetIndex,
            stPatricksGoldConfig);

        // Reuse the already validated matrix produced by SocketIOManager.
        spinResult.resultMatrix = resultMatrix;
        pendingSpinResult = spinResult;
        pendingResultMatrix = spinResult.resultMatrix;

        if (IsUltraBonusTriggered(serverResponse.payload?.ultraBonus) ||
            serverResponse.payload?.features?.ultraWheel?.triggered == true)
        {
            pendingUltraBonus = serverResponse.payload?.ultraBonus ??
                                new ServerUltraBonus { isTriggered = true };
            pendingUltraBonus.isTriggered = true;
        }
    }

    private List<int> GetConfiguredWinningPaylineIndices(SpinResult spinResult)
    {
        var result = new List<int>();
        if (spinResult?.winLines == null || stPatricksGoldConfig?.paylines == null)
        {
            return result;
        }

        var uniqueIndices = new HashSet<int>();
        foreach (WinLine winLine in spinResult.winLines)
        {
            if (winLine == null) continue;

            int lineIndex = winLine.lineId;
            if (lineIndex < 0 || lineIndex >= stPatricksGoldConfig.paylines.Count)
            {
                Debug.LogWarning(
                    $"[GameManager] Ignoring server winning line index {lineIndex}; " +
                    $"configured range is 0-{stPatricksGoldConfig.paylines.Count - 1}.");
                continue;
            }

            if (uniqueIndices.Add(lineIndex))
            {
                result.Add(lineIndex);
            }
        }

        return result;
    }

    internal void OnSpinResponseInvalid(ServerSpinResponse serverResponse, string rawJson, string error)
    {
        StoreLatestSpinResponse(serverResponse, rawJson);

        if (currentState == GameState.Spinning || currentState == GameState.Stopping)
        {
            FailActiveSpin(error);
        }
        else
        {
            Debug.LogError($"[GameManager] Invalid spin response received while no round was active: {error}");
        }
    }

    private void StoreLatestSpinResponse(ServerSpinResponse serverResponse, string rawJson)
    {
        latestServerSpinResponse = serverResponse;
        latestRawSpinResponse = rawJson;
    }

    private void FailActiveSpin(string error)
    {
        Debug.LogError($"[GameManager] Spin cancelled: {error}");

        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        pendingResultMatrix = null;
        pendingSpinResult = null;
        pendingUltraBonus = null;
        manualStopRequested = false;
        slotView?.CancelSpin();

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        currentState = GameState.Idle;
        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();
    }

    #endregion

    #region Ultra Slot

    internal bool IsUltraSlotUnlocked()
    {
        return isUltraSlotUnlocked;
    }

    internal bool IsUltraSlotSpinning()
    {
        return isUltraSlotSpinning;
    }

    internal bool AreUltraWheelsSpinning()
    {
        return areUltraWheelsSpinning;
    }

    internal bool ShouldShowUltraStartButton()
    {
        return isUltraSlotUnlocked &&
               !isUltraSlotTransitioning &&
               !hasUltraSlotStarted;
    }

    internal bool CanStartUltraSlot()
    {
        return isInitialized &&
               isUltraSlotUnlocked &&
               !isUltraSlotTransitioning &&
               !hasUltraSlotStarted &&
               !isUltraSlotSpinning &&
               pendingUltraSlotResult != null &&
               pendingUltraSlotResult.Count == UltraSlotView.ResultCellCount &&
               ultraSlotView != null &&
               !ultraSlotView.IsSpinning;
    }

    internal bool RequestUltraSlotStart()
    {
        if (!CanStartUltraSlot())
        {
            Debug.LogWarning(
                "[GameManager] Ultra Start ignored. The feature is not ready, " +
                "its server reel result is missing, or it has already started.");
            return false;
        }

        if (!ultraSlotView.TryValidateResult(pendingUltraSlotResult, out string validationError))
        {
            Debug.LogError($"[GameManager] Ultra slot result is invalid: {validationError}");
            return false;
        }

        if (!ultraSlotView.StartSpin(SpinSpeed.Normal))
        {
            Debug.LogError("[GameManager] Ultra reels could not start.");
            return false;
        }

        hasUltraSlotStarted = true;
        isUltraSlotSpinning = true;
        GamePresentationChanged?.Invoke();

        if (ultraSlotCoroutine != null)
        {
            StopCoroutine(ultraSlotCoroutine);
        }
        ultraSlotCoroutine = StartCoroutine(StopUltraSlotAfterDelay());

        Debug.Log("[GameManager] Ultra Start clicked. The three ultra reels are now spinning.");
        return true;
    }

    private IEnumerator StopUltraSlotAfterDelay()
    {
        if (ultraSlotSpinDuration > 0f)
        {
            yield return new WaitForSeconds(ultraSlotSpinDuration);
        }
        else
        {
            yield return null;
        }

        ultraSlotCoroutine = null;
        if (!isUltraSlotSpinning || ultraSlotView == null || pendingUltraSlotResult == null)
        {
            yield break;
        }

        ultraSlotView.StopSpin(pendingUltraSlotResult, OnUltraSlotStopped);
    }

    private void OnUltraSlotStopped()
    {
        isUltraSlotSpinning = false;

        if (ultraWheelsCoroutine != null)
        {
            StopCoroutine(ultraWheelsCoroutine);
        }
        ultraWheelsCoroutine = StartCoroutine(
            SpinActiveUltraWheelsAfterResultAnimation());

        GamePresentationChanged?.Invoke();
        Debug.Log(
            "[GameManager] Ultra slot stopped on the server-provided reel states. " +
            "Waiting for the result animation before spinning active wheels.");
    }

    private IEnumerator SpinActiveUltraWheelsAfterResultAnimation()
    {
        if (ultraResultAnimationHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                ultraResultAnimationHoldDuration);
        }
        else
        {
            yield return null;
        }

        if (!isUltraSlotUnlocked)
        {
            ultraWheelsCoroutine = null;
            yield break;
        }

        HashSet<int> activeWheelNumbers = GetActiveUltraWheelNumbersFromResult();
        var serverResultsByWheel = new Dictionary<int, ServerUltraActiveWheel>();
        if (latestUltraBonus?.activeWheels != null)
        {
            foreach (ServerUltraActiveWheel serverWheel in latestUltraBonus.activeWheels)
            {
                if (serverWheel != null &&
                    serverWheel.wheelIndex >= 1 &&
                    serverWheel.wheelIndex <= 3 &&
                    !serverResultsByWheel.ContainsKey(serverWheel.wheelIndex))
                {
                    serverResultsByWheel.Add(serverWheel.wheelIndex, serverWheel);
                }
            }
        }

        int startedWheelCount = 0;
        int completedWheelCount = 0;
        foreach (int wheelNumber in activeWheelNumbers)
        {
            if (!serverResultsByWheel.TryGetValue(
                    wheelNumber,
                    out ServerUltraActiveWheel serverWheel))
            {
                Debug.LogWarning(
                    $"[GameManager] Ultra result activated wheel {wheelNumber}, " +
                    "but the server supplied no activeWheels stop result for it.");
                continue;
            }

            UltraWheelController wheelController =
                GetUltraWheelController(wheelNumber);
            if (wheelController == null)
            {
                Debug.LogError(
                    $"[GameManager] Ultra wheel {wheelNumber} is active, " +
                    "but its controller is not assigned.");
                continue;
            }

            bool started = wheelController.SpinToServerStopIndex(
                serverWheel.stopIndex,
                () => completedWheelCount++);
            if (started)
            {
                startedWheelCount++;
            }
        }

        if (startedWheelCount == 0)
        {
            areUltraWheelsSpinning = false;
            ultraWheelsCoroutine = null;
            UltraWheelsCompleted?.Invoke();
            GamePresentationChanged?.Invoke();
            yield break;
        }

        areUltraWheelsSpinning = true;
        GamePresentationChanged?.Invoke();

        while (isUltraSlotUnlocked &&
               completedWheelCount < startedWheelCount)
        {
            yield return null;
        }

        areUltraWheelsSpinning = false;
        ultraWheelsCoroutine = null;
        if (!isUltraSlotUnlocked)
        {
            yield break;
        }

        UltraWheelsCompleted?.Invoke();
        GamePresentationChanged?.Invoke();
        Debug.Log(
            $"[GameManager] All {startedWheelCount} active Ultra wheel(s) finished.");
    }

    private HashSet<int> GetActiveUltraWheelNumbersFromResult()
    {
        var activeWheelNumbers = new HashSet<int>();
        if (pendingUltraSlotResult == null)
        {
            return activeWheelNumbers;
        }

        foreach (int resultSymbol in pendingUltraSlotResult)
        {
            if (resultSymbol >= UltraSlotView.CoinOneSymbolId &&
                resultSymbol <= UltraSlotView.CoinThreeSymbolId)
            {
                activeWheelNumbers.Add(resultSymbol);
            }
        }

        return activeWheelNumbers;
    }

    private UltraWheelController GetUltraWheelController(int wheelNumber)
    {
        switch (wheelNumber)
        {
            case 1:
                return redUltraWheel;
            case 2:
                return blueUltraWheel;
            case 3:
                return thirdUltraWheel;
            default:
                return null;
        }
    }

    /// <summary>
    /// Called by the later Ultra Wheel presentation after all awarded wheels finish.
    /// </summary>
    internal void CompleteUltraSlotFeature()
    {
        if (isUltraSlotSpinning ||
            ultraWheelsCoroutine != null ||
            areUltraWheelsSpinning)
        {
            Debug.LogWarning(
                "[GameManager] Ultra feature cannot close until the slot and all active wheels finish.");
            return;
        }

        PlayUltraSlotExitTransition();
    }

    private void UnlockUltraSlot(ServerUltraBonus ultraBonus, string source)
    {
        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        latestUltraBonus = ultraBonus;
        pendingUltraSlotResult = null;
        hasUltraSlotStarted = false;
        isUltraSlotSpinning = false;
        isUltraSlotUnlocked = true;

        if (TryBuildUltraSlotResult(ultraBonus, out List<int> serverResult))
        {
            pendingUltraSlotResult = serverResult;
        }

        if (ultraSlotView != null)
        {
            ultraSlotView.SetInitialResult(UltraSlotView.CreateDefaultInitialResult());
        }

        PlayUltraTriggerAnimationThenEnter(ultraBonus);

        Debug.Log(
            $"[GameManager] Ultra slot unlocked by {source}. " +
            (pendingUltraSlotResult != null
                ? "Waiting for the Ultra Start button."
                : "Waiting for a server reel result before Start can be used."));
    }

    private void ResetUltraSlotState()
    {
        KillUltraSlotTransition();
        slotView?.CancelWinAnimation();
        StopActiveUltraWheels();

        if (ultraSlotCoroutine != null)
        {
            StopCoroutine(ultraSlotCoroutine);
            ultraSlotCoroutine = null;
        }

        ultraSlotView?.CancelSpin();
        pendingUltraBonus = null;
        pendingUltraSlotResult = null;
        latestUltraBonus = null;
        isUltraSlotUnlocked = false;
        isUltraSlotSpinning = false;
        hasUltraSlotStarted = false;
        areUltraWheelsSpinning = false;
        RestoreUltraSlotLayout();

        if (ultraSlotPanel != null)
        {
            ultraSlotPanel.SetActive(false);
        }
        if (normalSlotPanel != null)
        {
            normalSlotPanel.SetActive(true);
        }
    }

    private void StopActiveUltraWheels()
    {
        if (ultraWheelsCoroutine != null)
        {
            StopCoroutine(ultraWheelsCoroutine);
            ultraWheelsCoroutine = null;
        }

        var stoppedControllers = new HashSet<UltraWheelController>();
        UltraWheelController[] wheelControllers =
        {
            redUltraWheel,
            blueUltraWheel,
            thirdUltraWheel
        };

        foreach (UltraWheelController wheelController in wheelControllers)
        {
            if (wheelController != null &&
                stoppedControllers.Add(wheelController))
            {
                wheelController.KillSpin();
            }
        }

        areUltraWheelsSpinning = false;
    }

    private void PlayUltraTriggerAnimationThenEnter(ServerUltraBonus ultraBonus)
    {
        KillUltraSlotTransition();
        isUltraSlotTransitioning = true;

        List<int> ultraWheelPositions = GetUltraWheelTriggerPositions(ultraBonus);
        bool startedAnimation =
            ultraTriggerAnimationDuration > 0f &&
            slotView != null &&
            slotView.ShowPrioritySymbolAnimation(
                ultraWheelPositions,
                ultraTriggerAnimationDuration,
                () =>
                {
                    if (isUltraSlotUnlocked)
                    {
                        PlayUltraSlotEnterTransition();
                    }
                });

        if (!startedAnimation)
        {
            PlayUltraSlotEnterTransition();
            return;
        }

        GamePresentationChanged?.Invoke();
    }

    private List<int> GetUltraWheelTriggerPositions(ServerUltraBonus ultraBonus)
    {
        var positions = new List<int>();
        var uniquePositions = new HashSet<int>();
        List<List<int>> displayMatrix = slotView?.GetCurrentDisplayMatrix();

        if (displayMatrix != null)
        {
            for (int column = 0; column < displayMatrix.Count; column++)
            {
                List<int> columnSymbols = displayMatrix[column];
                if (columnSymbols == null)
                {
                    continue;
                }

                for (int row = 0; row < columnSymbols.Count; row++)
                {
                    if (columnSymbols[row] != StPatricksGoldSymbolIds.UltraWheel)
                    {
                        continue;
                    }

                    int flatIndex = row * displayMatrix.Count + column;
                    if (uniquePositions.Add(flatIndex))
                    {
                        positions.Add(flatIndex);
                    }
                }
            }
        }

        // Prefer the displayed matrix because it guarantees the highlighted
        // images are Ultra Wheel symbols. Trigger positions are a fallback for
        // compact server responses that omit the full display matrix.
        if (positions.Count > 0 || ultraBonus?.triggerPositions == null)
        {
            return positions;
        }

        int columnCount = stPatricksGoldConfig?.reelCount > 0
            ? stPatricksGoldConfig.reelCount
            : StPatricksGoldDefinition.ReelCount;
        int rowCount = stPatricksGoldConfig?.rowCount > 0
            ? stPatricksGoldConfig.rowCount
            : StPatricksGoldDefinition.RowCount;

        foreach (ServerGridPosition triggerPosition in ultraBonus.triggerPositions)
        {
            if (triggerPosition == null)
            {
                continue;
            }

            int row = triggerPosition.row;
            int column = triggerPosition.col;
            if (row < 0 || row >= rowCount || column < 0 || column >= columnCount)
            {
                continue;
            }

            int flatIndex = row * columnCount + column;
            if (uniquePositions.Add(flatIndex))
            {
                positions.Add(flatIndex);
            }
        }

        return positions;
    }

    private void CacheUltraSlotLayout()
    {
        if (hasCachedUltraSlotLayout)
        {
            return;
        }

        normalSlotRectTransform = normalSlotPanel != null
            ? normalSlotPanel.GetComponent<RectTransform>()
            : null;
        ultraSlotRectTransform = ultraSlotPanel != null
            ? ultraSlotPanel.GetComponent<RectTransform>()
            : null;

        if (normalSlotRectTransform == null || ultraSlotRectTransform == null)
        {
            return;
        }

        normalSlotRestingPosition = normalSlotRectTransform.anchoredPosition;
        ultraSlotRestingPosition = ultraSlotRectTransform.anchoredPosition;
        hasCachedUltraSlotLayout = true;
    }

    private void PlayUltraSlotEnterTransition()
    {
        KillUltraSlotTransition();
        CacheUltraSlotLayout();

        if (!hasCachedUltraSlotLayout)
        {
            normalSlotPanel?.SetActive(false);
            ultraSlotPanel?.SetActive(true);
            GamePresentationChanged?.Invoke();
            Debug.LogWarning(
                "[GameManager] Ultra slot transition requires RectTransforms; used an instant swap instead.");
            return;
        }

        isUltraSlotTransitioning = true;
        normalSlotPanel.SetActive(true);
        ultraSlotPanel.SetActive(true);

        normalSlotRectTransform.anchoredPosition = normalSlotRestingPosition;
        ultraSlotRectTransform.anchoredPosition =
            ultraSlotRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(ultraSlotRectTransform);

        float duration = Mathf.Max(0.01f, ultraSlotTransitionDuration);
        ultraSlotTransitionSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                normalSlotRectTransform
                    .DOAnchorPos(
                        normalSlotRestingPosition +
                        Vector2.down * GetUltraSlotSlideDistance(normalSlotRectTransform),
                        duration)
                    .SetEase(Ease.InCubic))
            .AppendCallback(() => normalSlotPanel.SetActive(false))
            .Append(
                ultraSlotRectTransform
                    .DOAnchorPos(ultraSlotRestingPosition, duration)
                    .SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                ultraSlotTransitionSequence = null;
                isUltraSlotTransitioning = false;
                GamePresentationChanged?.Invoke();
            });

        GamePresentationChanged?.Invoke();
    }

    private void PlayUltraSlotExitTransition()
    {
        if (!isUltraSlotUnlocked)
        {
            ResetUltraSlotState();
            GamePresentationChanged?.Invoke();
            return;
        }

        KillUltraSlotTransition();
        slotView?.CancelWinAnimation();
        StopActiveUltraWheels();
        CacheUltraSlotLayout();

        if (!hasCachedUltraSlotLayout)
        {
            ResetUltraSlotState();
            GamePresentationChanged?.Invoke();
            return;
        }

        if (ultraSlotCoroutine != null)
        {
            StopCoroutine(ultraSlotCoroutine);
            ultraSlotCoroutine = null;
        }

        ultraSlotView?.CancelSpin();
        isUltraSlotSpinning = false;
        isUltraSlotTransitioning = true;

        ultraSlotPanel.SetActive(true);
        normalSlotPanel.SetActive(true);
        ultraSlotRectTransform.anchoredPosition = ultraSlotRestingPosition;
        normalSlotRectTransform.anchoredPosition =
            normalSlotRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(normalSlotRectTransform);

        float duration = Mathf.Max(0.01f, ultraSlotTransitionDuration);
        ultraSlotTransitionSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                ultraSlotRectTransform
                    .DOAnchorPos(
                        ultraSlotRestingPosition +
                        Vector2.down * GetUltraSlotSlideDistance(ultraSlotRectTransform),
                        duration)
                    .SetEase(Ease.InCubic))
            .AppendCallback(() => ultraSlotPanel.SetActive(false))
            .Append(
                normalSlotRectTransform
                    .DOAnchorPos(normalSlotRestingPosition, duration)
                    .SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                ultraSlotTransitionSequence = null;
                ResetUltraSlotState();
                GamePresentationChanged?.Invoke();
            });

        GamePresentationChanged?.Invoke();
    }

    private float GetUltraSlotSlideDistance(RectTransform panel)
    {
        float containerHeight = 0f;
        if (panel.parent is RectTransform parentRectTransform)
        {
            containerHeight = Mathf.Abs(parentRectTransform.rect.height);
        }

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null &&
            canvas.rootCanvas != null &&
            canvas.rootCanvas.transform is RectTransform canvasRectTransform)
        {
            containerHeight = Mathf.Max(
                containerHeight,
                Mathf.Abs(canvasRectTransform.rect.height));
        }

        float panelHeight = Mathf.Abs(panel.rect.height);
        return Mathf.Max(1f, containerHeight) +
               panelHeight +
               Mathf.Max(0f, ultraSlotTransitionPadding);
    }

    private void RestoreUltraSlotLayout()
    {
        if (!hasCachedUltraSlotLayout)
        {
            return;
        }

        normalSlotRectTransform.anchoredPosition = normalSlotRestingPosition;
        ultraSlotRectTransform.anchoredPosition = ultraSlotRestingPosition;
    }

    private void KillUltraSlotTransition()
    {
        if (ultraSlotTransitionSequence != null)
        {
            ultraSlotTransitionSequence.Kill();
            ultraSlotTransitionSequence = null;
        }

        isUltraSlotTransitioning = false;
    }

    private static bool IsUltraBonusTriggered(ServerUltraBonus ultraBonus)
    {
        return ultraBonus != null && ultraBonus.isTriggered;
    }

    private static bool TryBuildUltraSlotResult(
        ServerUltraBonus ultraBonus,
        out List<int> result)
    {
        result = null;
        if (ultraBonus?.reelResults == null || ultraBonus.reelResults.Count == 0)
        {
            return false;
        }

        List<int> converted = UltraSlotView.CreateEmptyResult();
        var assignedReels = new HashSet<int>();

        foreach (ServerUltraReelResult reelResult in ultraBonus.reelResults)
        {
            if (reelResult == null)
            {
                continue;
            }

            // The live SL-SPG response uses one-based reelIndex values 1, 2, 3.
            int resultIndex = reelResult.reelIndex >= 1 &&
                              reelResult.reelIndex <= UltraSlotView.ReelCount
                ? reelResult.reelIndex - 1
                : reelResult.reelIndex;
            if (resultIndex < 0 ||
                resultIndex >= UltraSlotView.ReelCount ||
                !assignedReels.Add(resultIndex))
            {
                Debug.LogWarning(
                    $"[GameManager] Ignoring invalid or duplicate ultra reel index {reelResult.reelIndex}.");
                continue;
            }

            bool explicitlyInactive = string.Equals(
                reelResult.wheelState,
                "inactive",
                StringComparison.OrdinalIgnoreCase);
            bool isActive = !explicitlyInactive &&
                            (string.Equals(
                                 reelResult.wheelState,
                                 "active",
                                 StringComparison.OrdinalIgnoreCase) ||
                             reelResult.bonusWheelStopIndex > 0);
            if (!isActive)
            {
                converted[UltraSlotView.GetResultIndex(
                    UltraSlotView.CenterRowIndex,
                    resultIndex)] = UltraSlotView.EmptySymbolId;
                continue;
            }

            int wheelSymbol = reelResult.assignedWheelIndex;
            if (wheelSymbol < UltraSlotView.CoinOneSymbolId ||
                wheelSymbol > UltraSlotView.CoinThreeSymbolId)
            {
                wheelSymbol = resultIndex + UltraSlotView.CoinOneSymbolId;
            }

            converted[UltraSlotView.GetResultIndex(
                UltraSlotView.CenterRowIndex,
                resultIndex)] = wheelSymbol;
        }

        result = converted;
        return true;
    }

    #endregion

    #region Spin Speed Control

    internal bool SetSpinSpeed(SpinSpeed speed)
    {
        if (speed != SpinSpeed.Normal &&
            speed != SpinSpeed.FastSpin &&
            speed != SpinSpeed.SkipSpin)
        {
            Debug.LogWarning($"[GameManager] Unsupported spin mode requested: {speed}.");
            return false;
        }

        currentSpinSpeed = speed;
        SpinSpeedChanged?.Invoke(currentSpinSpeed);
        Debug.Log($"[GameManager] Spin mode selected: {currentSpinSpeed}.");
        return true;
    }

    internal SpinSpeed GetSpinSpeed()
    {
        return currentSpinSpeed;
    }

    internal int GetDisplayedPaylineCount()
    {
        return stPatricksGoldConfig != null
            ? stPatricksGoldConfig.paylineCount
            : 0;
    }

    internal double GetDisplayedWinAmount()
    {
        return displayedWinAmount;
    }

    internal double GetDisplayedBalance()
    {
        return playerData != null ? playerData.balance : 0;
    }

    internal double GetDisplayedBetAmount()
    {
        return currentBetAmount;
    }

    internal double GetDisplayedTotalBetAmount()
    {
        double multiplier = stPatricksGoldConfig != null
            ? stPatricksGoldConfig.betMultiplier
            : 1;
        return currentBetAmount * multiplier;
    }

    #endregion

    #region Auto Play

    internal bool StartAutoPlay(int rounds)
    {
        bool isInfinite = rounds == InfiniteAutoPlayRounds;
        if (!isInfinite && rounds <= 0)
        {
            Debug.LogWarning($"[GameManager] Invalid autoplay round count: {rounds}.");
            return false;
        }

        if (!CanStartAutoPlay())
        {
            Debug.LogWarning("[GameManager] Autoplay cannot start in the current game state.");
            return false;
        }

        // Check balance BEFORE locking any UI — if insufficient, show popup and bail.
        if (!CanAffordBet())
        {
            Debug.LogWarning("[GameManager] Insufficient funds.");
            return false;
        }

        isAutoPlaying = true;
        autoPlayTotalRounds = rounds;
        autoPlayRemainingRounds = rounds;
        AutoPlayChanged?.Invoke();

        if (!RequestSpin())
        {
            StopAutoPlay();
            return false;
        }

        return true;
    }

    internal void StopAutoPlay()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }

        bool stateChanged = isAutoPlaying || autoPlayRemainingRounds != 0;
        isAutoPlaying = false;
        autoPlayRemainingRounds = 0;

        if (stateChanged)
        {
            AutoPlayChanged?.Invoke();
        }
    }

    internal bool CanStartAutoPlay()
    {
        return !isAutoPlaying && CanRequestSpin();
    }

    private void QueueNextAutoPlaySpin()
    {
        if (!isAutoPlaying || autoPlayCoroutine != null) return;

        autoPlayCoroutine = StartCoroutine(StartNextAutoPlaySpin());
    }

    private IEnumerator StartNextAutoPlaySpin()
    {
        // Keep the completed server result visible before starting another round.
        if (autoPlayResultHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(autoPlayResultHoldDuration);
        }
        else
        {
            yield return null;
        }

        autoPlayCoroutine = null;

        if (!isAutoPlaying)
        {
            yield break;
        }

        if (!RequestSpin())
        {
            StopAutoPlay();
        }
    }

    #endregion

    #region Connection Events

    internal void OnDisconnected()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        pendingResultMatrix = null;
        pendingSpinResult = null;
        manualStopRequested = false;
        slotView?.CancelSpin();
        ResetUltraSlotState();
        currentState = GameState.Idle;
        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();
        // Note: The disconnection popup is shown by SocketIOManager.OnSocketDisconnected()
        // to avoid duplicates. GameManager only cleans up state here.
    }

    internal void ExitGame()
    {
        socketManager.CloseGame();
    }

    #endregion

    #region Helper Methods

    internal bool CanAffordBet()
    {
        return playerData != null && playerData.balance >= GetDisplayedTotalBetAmount();
    }

    internal bool IsSpinning()
    {
        return currentState == GameState.Spinning || currentState == GameState.Stopping;
    }

    private void OnDestroy()
    {
        KillUltraSlotTransition();
        StopActiveUltraWheels();
    }

    #endregion
}
