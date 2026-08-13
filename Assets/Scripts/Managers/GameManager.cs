using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Best.HTTP.JSON;
using DG.Tweening;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    internal const int InfiniteAutoPlayRounds = -1;

    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] private SlotView slotView;
    [SerializeField] private SlotSymbolAnimationManager symbolAnimationManager;
    [Tooltip(
        "Controls result popups. If empty, the scene PopupManager is found automatically.")]
    [SerializeField] private PopupManager popupManager;

    [Header("Ultra Slot")]
    [SerializeField] private UltraSlotView ultraSlotView;
    [Tooltip(
        "The normal 5x3 panel. This assigned object is deactivated halfway " +
        "through the Ultra entry frame transition.")]
    [SerializeField] private GameObject normalSlotPanel;
    [Tooltip(
        "The three-reel Ultra panel. This assigned object is activated when " +
        "the normal panel is deactivated.")]
    [SerializeField] private GameObject ultraSlotPanel;
    [Tooltip("The panel containing the three Ultra prize wheels.")]
    [SerializeField] private GameObject ultraWheelPanel;
    [SerializeField, Min(0f)] private float ultraSlotSpinDuration = 2f;
    [SerializeField, Min(0.01f)] private float ultraSlotTransitionDuration = 1.2f;
    [SerializeField, Min(0f)] private float ultraSlotTransitionPadding = 100f;
    [Tooltip(
        "Duration of the small left/right shake before the Ultra slot slides down.")]
    [SerializeField, Min(0f)] private float ultraSlotExitShakeDuration = 0.35f;
    [Tooltip(
        "Horizontal distance used by the Ultra slot shake before the wheels appear.")]
    [SerializeField, Min(0f)] private float ultraSlotExitShakeStrength = 12f;

    [Header("Ultra Wheels")]
    [Tooltip("Server wheel 1: Green wheel, represented by Ultra result symbol 1.")]
    [UnityEngine.Serialization.FormerlySerializedAs("thirdUltraWheel")]
    [SerializeField] private UltraWheelController greenUltraWheel;
    [Tooltip("Server wheel 2: Blue wheel, represented by Ultra result symbol 2.")]
    [SerializeField] private UltraWheelController blueUltraWheel;
    [Tooltip("Server wheel 3: Red wheel, represented by Ultra result symbol 3.")]
    [SerializeField] private UltraWheelController redUltraWheel;
    [Tooltip(
        "Delay after the Ultra slot leaves before the green wheel begins rising.")]
    [SerializeField, Min(0f)] private float ultraFirstWheelRevealDelay = 1f;
    [Tooltip(
        "Delay between the green, blue, and red wheel rise start times.")]
    [SerializeField, Min(0f)] private float ultraWheelRevealStagger = 0.06f;
    [SerializeField, Min(0f)] private float ultraResultAnimationHoldDuration = 1.2f;
    [Tooltip(
        "Fallback hold time used when the Ultra Wheel Reward popup is not assigned.")]
    [SerializeField, Min(0f)] private float ultraWheelResultHoldDuration = 1.2f;
    [Tooltip(
        "Duration of the small left/right shake before the completed Ultra wheel panel exits.")]
    [SerializeField, Min(0f)] private float ultraRewardShakeDuration = 0.35f;
    [Tooltip(
        "Horizontal distance used by the completed Ultra wheel panel shake.")]
    [SerializeField, Min(0f)] private float ultraRewardShakeStrength = 12f;
    [Tooltip(
        "Time used for each slow panel slide before the Take button becomes available.")]
    [SerializeField, Min(0.01f)] private float ultraRewardExitDuration = 1.2f;

    [Header("Spin Settings")]
    [SerializeField] private float normalSpinDuration = 2.0f;
    [UnityEngine.Serialization.FormerlySerializedAs("quickSpinDuration")]
    [SerializeField] private float fastSpinDuration = 0.75f;
    [SerializeField, Min(0f)] private float skipSpinDuration = 0.5f;

    [Header("Auto Play Settings")]
    [Tooltip("How long an autoplay result remains visible before the next spin starts.")]
    [SerializeField, Min(0f)] private float autoPlayResultHoldDuration = 1f;
    [Tooltip(
        "Delay after the Scatter output texts finish moving before autoplay " +
        "starts the next spin.")]
    [SerializeField, Min(0f)]
    private float scatterAutoPlayResultHoldDuration = 2f;

    internal StPatricksGoldGameConfig stPatricksGoldConfig;
    internal PlayerData playerData;
    internal ServerSpinResponse latestServerSpinResponse { get; private set; }
    internal SpinResult latestSpinResult { get; private set; }
    internal string latestRawSpinResponse { get; private set; }
    internal ServerScatterBonus latestScatterBonus { get; private set; }
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
    private ServerScatterBonus pendingScatterBonus;
    private ServerUltraBonus pendingUltraBonus;
    private List<int> pendingUltraSlotResult;
    private bool manualStopRequested;
    private bool isUltraSlotUnlocked;
    private bool isUltraSlotSpinning;
    private bool hasUltraSlotStarted;
    private bool areUltraWheelsReady;
    private bool haveUltraWheelsStarted;
    private bool areUltraWheelsSpinning;
    private bool isUltraStartButtonReady;
    private bool isUltraTakeReady;
    private bool isUltraSlotTransitioning;
    private RectTransform normalSlotRectTransform;
    private RectTransform ultraSlotRectTransform;
    private RectTransform ultraWheelRectTransform;
    private RectTransform greenUltraWheelRectTransform;
    private RectTransform blueUltraWheelRectTransform;
    private RectTransform redUltraWheelRectTransform;
    private Vector2 normalSlotRestingPosition;
    private Vector2 ultraSlotRestingPosition;
    private Vector2 ultraWheelRestingPosition;
    private Vector2 greenUltraWheelRestingPosition;
    private Vector2 blueUltraWheelRestingPosition;
    private Vector2 redUltraWheelRestingPosition;
    private bool hasCachedUltraSlotLayout;
    private bool hasCachedUltraWheelLayout;
    private bool hasCachedUltraWheelItemLayout;
    private Sequence ultraSlotTransitionSequence;
    private double displayedWinAmount;
    private bool hasOptimisticBalanceTransaction;
    private double balanceBeforeActiveSpin;
    private double activeSpinBetAmount;
    private double? pendingAuthoritativeBalance;
    private readonly List<int> latestWinningPaylineIndicesInternal = new List<int>();

    #region Initialization

    private void Awake()
    {
        if (popupManager == null)
        {
            popupManager = FindFirstObjectByType<PopupManager>(
                FindObjectsInactive.Include);
        }

        ResolveUltraSlotReferences();
        CacheUltraSlotLayout();
        symbolAnimationManager?.StopUltraEntryTransition();
        currentState = GameState.Initializing;
        currentSpinSpeed = SpinSpeed.Normal;
        isInitialized = false;
        initializationFailed = false;
    }

    private void Start()
    {
        ResetUltraSlotState();
    }

    internal void OnStPatricksGoldConfigReceived(StPatricksGoldGameConfig config, PlayerData player, List<List<int>> initialMatrix)
    {
        initializationFailed = false;
        stPatricksGoldConfig = config;
        playerData = player;
        currentBetIndex = playerData.currentBetIndex;
        UpdateBetAmount();

        if (initialMatrix != null && slotView != null)
        {
            slotView.SetInitialMatrix(initialMatrix);
        }

        ApplyUltraWheelServerValues();

        isInitialized = true;
        currentState = GameState.Idle;
        displayedWinAmount = 0;
        ClearOptimisticBalanceTransaction();
        latestSpinResult = null;
        pendingSpinResult = null;
        latestScatterBonus = null;
        pendingScatterBonus = null;
        latestWinningPaylineIndicesInternal.Clear();

        ResetUltraSlotState();

        if (slotView != null)
        {
            slotView.RefreshMaskAfterSuccessfulInitialization();
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] The slot mask was not refreshed after " +
                "initialization because SlotView is not assigned.");
        }

        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        Debug.Log("[GameManager] Game initialized.");
    }

    internal void OnInitializationFailed(string message = null)
    {
        isInitialized = false;
        initializationFailed = true;
        currentState = GameState.Initializing;

        slotView?.ShowRandomFallbackSymbols();
        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        if (popupManager == null)
        {
            popupManager = FindFirstObjectByType<PopupManager>(
                FindObjectsInactive.Include);
        }

        if (popupManager == null ||
            !popupManager.ShowInitializationFailurePopup(message))
        {
            Debug.LogError(
                "[GameManager] Initialization failed, but the error popup " +
                "could not be shown.");
        }
    }

    private void ApplyUltraWheelServerValues()
    {
        greenUltraWheel?.ClearServerValues();
        blueUltraWheel?.ClearServerValues();
        redUltraWheel?.ClearServerValues();

        UltraWheelConfig ultraWheelConfig = stPatricksGoldConfig?.ultraWheel;
        if (ultraWheelConfig == null)
        {
            Debug.LogError(
                "[GameManager] The server configuration contains no Ultra " +
                "wheel value tables. Ultra labels will remain blank.");
            return;
        }

        bool greenApplied =
            ApplyUltraWheelServerValues(
                greenUltraWheel,
                ultraWheelConfig.wheel1Awards,
                1);
        bool blueApplied =
            ApplyUltraWheelServerValues(
                blueUltraWheel,
                ultraWheelConfig.wheel2Awards,
                2);
        bool redApplied =
            ApplyUltraWheelServerValues(
                redUltraWheel,
                ultraWheelConfig.wheel3Awards,
                3);

        if (!greenApplied || !blueApplied || !redApplied)
        {
            Debug.LogError(
                $"[GameManager] The server must supply a complete " +
                $"{UltraWheelController.ServerValueCount}-value table for " +
                "each Ultra wheel. Missing wheel labels remain blank.");
        }
    }

    private static bool ApplyUltraWheelServerValues(
        UltraWheelController wheelController,
        IReadOnlyList<int> serverValues,
        int wheelNumber)
    {
        if (serverValues == null ||
            serverValues.Count != UltraWheelController.ServerValueCount)
        {
            return false;
        }

        if (wheelController == null)
        {
            Debug.LogError(
                $"[GameManager] Cannot display values for Ultra wheel {wheelNumber}; " +
                "its controller is not assigned.");
            return false;
        }

        if (wheelController.WheelNumber != wheelNumber)
        {
            Debug.LogError(
                $"[GameManager] Ultra wheel {wheelNumber} is assigned to a " +
                $"controller configured as wheel {wheelController.WheelNumber}.");
            return false;
        }

        return wheelController.SetServerValues(serverValues);
    }

    private void ApplyUltraWheelResultValues(ServerUltraBonus ultraBonus)
    {
        if (ultraBonus?.activeWheels == null)
        {
            return;
        }

        foreach (ServerUltraActiveWheel serverWheel in ultraBonus.activeWheels)
        {
            if (serverWheel == null)
            {
                continue;
            }

            Debug.Log(
                $"[GameManager] SERVER ULTRA RESULT | " +
                $"Wheel: {serverWheel.wheelIndex} | " +
                $"Stop Index: {serverWheel.stopIndex} | " +
                $"Base Award: {serverWheel.baseAward:0.##} | " +
                $"Final Award: {serverWheel.finalAward:0.##}");

            UltraWheelController wheelController =
                GetUltraWheelController(serverWheel.wheelIndex);
            if (wheelController == null)
            {
                Debug.LogError(
                    $"[GameManager] Cannot update the selected value for Ultra wheel " +
                    $"{serverWheel.wheelIndex}; its controller is not assigned.");
                continue;
            }

            if (serverWheel.awards != null &&
                serverWheel.awards.Count ==
                    UltraWheelController.ServerValueCount)
            {
                wheelController.SetServerValues(serverWheel.awards);
            }
        }

        Debug.Log(
            $"[GameManager] SERVER ULTRA TOTAL AWARD: {ultraBonus.totalAward:0.##}");
    }

    private void ResolveUltraSlotReferences()
    {
        if (symbolAnimationManager == null)
        {
            symbolAnimationManager =
                FindFirstObjectByType<SlotSymbolAnimationManager>(
                    FindObjectsInactive.Include);
        }

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

        if (ultraWheelPanel == null)
        {
            ultraWheelPanel = FindSceneObjectByName("UltraWheel Panel");
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
                case 1 when greenUltraWheel == null:
                    greenUltraWheel = wheelController;
                    break;
                case 2 when blueUltraWheel == null:
                    blueUltraWheel = wheelController;
                    break;
                case 3 when redUltraWheel == null:
                    redUltraWheel = wheelController;
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

    internal bool IsCurrentBetMaximum()
    {
        return stPatricksGoldConfig?.availableBets != null &&
               stPatricksGoldConfig.availableBets.Count > 0 &&
               currentBetIndex ==
                   stPatricksGoldConfig.availableBets.Count - 1;
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
            popupManager?.ShowInsufficientFundsError();
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
        popupManager?.HideScatterWinImmediate();
        popupManager?.HideUltraStartImmediate();
        popupManager?.HideUltraWinImmediate();
        pendingResultMatrix = null;
        pendingSpinResult = null;
        pendingScatterBonus = null;
        latestScatterBonus = null;
        pendingUltraBonus = null;
        manualStopRequested = false;
        displayedWinAmount = 0;
        BeginOptimisticBalanceTransaction();
        latestWinningPaylineIndicesInternal.Clear();
        currentState = GameState.Spinning;
        activeSpinSpeed = currentSpinSpeed;

        slotView.StartSpin(activeSpinSpeed);
        AudioController.Instance?.PlaySpinButton();
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

    private void BeginOptimisticBalanceTransaction()
    {
        balanceBeforeActiveSpin = playerData != null ? playerData.balance : 0;
        activeSpinBetAmount = GetDisplayedTotalBetAmount();
        pendingAuthoritativeBalance = null;
        hasOptimisticBalanceTransaction = playerData != null;

        if (playerData == null)
        {
            return;
        }

        playerData.balance =
            Math.Max(0, balanceBeforeActiveSpin - activeSpinBetAmount);

        Debug.Log(
            $"[GameManager] LOCAL BALANCE | Spin started | " +
            $"Before: {balanceBeforeActiveSpin:0.00} | " +
            $"Bet: -{activeSpinBetAmount:0.00} | " +
            $"Displayed: {playerData.balance:0.00}");
    }

    private void CaptureAuthoritativeBalance(ServerSpinResponse serverResponse)
    {
        if (serverResponse?.player?.balance.HasValue == true)
        {
            pendingAuthoritativeBalance = serverResponse.player.balance.Value;
            Debug.Log(
                $"[GameManager] SERVER BALANCE CAPTURED: " +
                $"{pendingAuthoritativeBalance.Value:0.00}. " +
                "It will be applied after the round presentation finishes.");
        }
    }

    private void CompleteOptimisticBalanceTransaction(double totalWinAmount)
    {
        if (!hasOptimisticBalanceTransaction || playerData == null)
        {
            ClearOptimisticBalanceTransaction();
            return;
        }

        double sanitizedWinAmount = Math.Max(0, totalWinAmount);
        playerData.balance += sanitizedWinAmount;
        double locallyCalculatedBalance = playerData.balance;

        Debug.Log(
            $"[GameManager] LOCAL BALANCE | Round completed | " +
            $"Win: +{sanitizedWinAmount:0.00} | " +
            $"Calculated: {locallyCalculatedBalance:0.00}");

        if (pendingAuthoritativeBalance.HasValue)
        {
            double serverBalance = pendingAuthoritativeBalance.Value;
            if (Math.Abs(locallyCalculatedBalance - serverBalance) > 0.0001d)
            {
                Debug.LogWarning(
                    $"[GameManager] BALANCE RECONCILED | " +
                    $"Local: {locallyCalculatedBalance:0.00} | " +
                    $"Server: {serverBalance:0.00}. Using the server balance.");
            }
            else
            {
                Debug.Log(
                    $"[GameManager] BALANCE VERIFIED | " +
                    $"Local and server: {serverBalance:0.00}");
            }

            playerData.balance = serverBalance;
        }

        ClearOptimisticBalanceTransaction();
    }

    private void CancelOptimisticBalanceTransaction()
    {
        if (!hasOptimisticBalanceTransaction || playerData == null)
        {
            ClearOptimisticBalanceTransaction();
            return;
        }

        double restoredBalance = pendingAuthoritativeBalance ??
                                 balanceBeforeActiveSpin;
        playerData.balance = restoredBalance;

        Debug.LogWarning(
            $"[GameManager] LOCAL BALANCE | Spin cancelled | " +
            $"Restored: {restoredBalance:0.00}");

        ClearOptimisticBalanceTransaction();
    }

    private void ClearOptimisticBalanceTransaction()
    {
        hasOptimisticBalanceTransaction = false;
        balanceBeforeActiveSpin = 0;
        activeSpinBetAmount = 0;
        pendingAuthoritativeBalance = null;
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
        bool useTurboStop = activeSpinSpeed == SpinSpeed.SkipSpin;
        bool useFastStop = manualStopRequested || activeSpinSpeed == SpinSpeed.FastSpin;
        spinCoroutine = null;

        if (useTurboStop)
        {
            slotView.TurboStop(resultMatrix, OnReelsStoppedComplete);
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
        AudioController.Instance?.PlayAllReelsStopped();
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
        if (completedResult.playerData != null)
        {
            currentBetIndex = completedResult.playerData.currentBetIndex;
            UpdateBetAmount();
        }

        latestWinningPaylineIndicesInternal.Clear();
        latestWinningPaylineIndicesInternal.AddRange(GetConfiguredWinningPaylineIndices(completedResult));

        ServerScatterBonus completedScatterBonus =
            pendingScatterBonus;
        pendingScatterBonus = null;
        ServerUltraBonus completedUltraBonus = pendingUltraBonus;
        pendingUltraBonus = null;
        bool ultraTriggered = IsUltraBonusTriggered(completedUltraBonus);
        bool scatterTriggered =
            !ultraTriggered &&
            IsScatterBonusTriggered(completedScatterBonus);
        latestScatterBonus = scatterTriggered
            ? completedScatterBonus
            : null;
        currentState = scatterTriggered
            ? GameState.ShowingWin
            : GameState.Idle;
        displayedWinAmount = ultraTriggered || scatterTriggered
            ? 0
            : completedResult.winAmount;

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

        if (!ultraTriggered && !scatterTriggered)
        {
            CompleteOptimisticBalanceTransaction(completedResult.winAmount);
            GamePresentationChanged?.Invoke();
        }

        if (scatterTriggered)
        {
            bool presentationStarted =
                slotView != null &&
                slotView.ShowScatterWheelFeature(
                    completedScatterBonus,
                    () =>
                        OnScatterWheelPresentationComplete(
                            shouldContinueAutoPlay,
                            completedScatterBonus.totalAward));

            if (presentationStarted)
            {
                AudioController.Instance?.PlayScatterWheel();
            }

            if (!presentationStarted)
            {
                OnScatterWheelPresentationComplete(
                    shouldContinueAutoPlay,
                    completedScatterBonus.totalAward);
            }

            Debug.Log(
                "[GameManager] SpinResult applied. " +
                "Showing the server Scatter Wheel results.");
            return;
        }

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
                completedResult.winAmount,
                shouldContinueAutoPlay ? QueueNextAutoPlaySpin : null
            );
        }

        Debug.Log("[GameManager] SpinResult applied. Round returned to Idle.");

        if (!hasWinLines && shouldContinueAutoPlay)
        {
            QueueNextAutoPlaySpin();
        }
    }

    private void OnScatterWheelPresentationComplete(
        bool shouldContinueAutoPlay,
        double totalWinAmount)
    {
        if (popupManager != null &&
            popupManager.ShowScatterWin(
                totalWinAmount,
                () =>
                    FinishScatterWinPresentation(
                        shouldContinueAutoPlay,
                        totalWinAmount,
                        0f)))
        {
            Debug.Log(
                "[GameManager] Scatter wheels completed. " +
                "Showing the Scatter total-win popup.");
            return;
        }

        FinishScatterWinPresentation(
            shouldContinueAutoPlay,
            totalWinAmount,
            scatterAutoPlayResultHoldDuration);
    }

    private void FinishScatterWinPresentation(
        bool shouldContinueAutoPlay,
        double totalWinAmount,
        float autoPlayDelay)
    {
        if (currentState == GameState.ShowingWin)
        {
            currentState = GameState.Idle;
        }

        displayedWinAmount = Math.Max(0d, totalWinAmount);

        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        CompleteOptimisticBalanceTransaction(totalWinAmount);
        GamePresentationChanged?.Invoke();

        if (shouldContinueAutoPlay)
        {
            QueueNextAutoPlaySpin(
                autoPlayDelay);
        }

        Debug.Log(
            "[GameManager] Scatter Wheel presentation completed. " +
            "Round returned to Idle.");
    }

    private float GetSpinDuration(SpinSpeed spinSpeed)
    {
        switch (spinSpeed)
        {
            case SpinSpeed.FastSpin:
                return Mathf.Max(0f, fastSpinDuration);
            case SpinSpeed.SkipSpin:
                return Mathf.Max(0f, skipSpinDuration);
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
               !slotView.IsSpinning();
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
        return IsSpinning() ||
               currentState == GameState.ShowingWin;
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

        CaptureAuthoritativeBalance(serverResponse);

        if (resultMatrix == null)
        {
            FailActiveSpin("The server result matrix is null.");
            return;
        }

        if (slotView == null)
        {
            FailActiveSpin(
                "The server result cannot be displayed because SlotView is not assigned.");
            return;
        }

        if (!slotView.TryValidateResultMatrix(resultMatrix, out string validationError))
        {
            FailActiveSpin(
                $"The server result cannot be displayed: {validationError}");
            return;
        }

        if (!TryCaptureServerBonusData(
                rawJson,
                serverResponse,
                out string bonusCaptureError))
        {
            FailActiveSpin(bonusCaptureError);
            return;
        }

        if (!TryValidateServerFeatureResults(
                serverResponse,
                resultMatrix,
                out string featureValidationError))
        {
            FailActiveSpin(featureValidationError);
            return;
        }

        double currentBalance = hasOptimisticBalanceTransaction
            ? balanceBeforeActiveSpin
            : (playerData != null ? playerData.balance : 0);
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

        if (IsUltraBonusTriggered(serverResponse.payload?.ultraBonus))
        {
            pendingUltraBonus = serverResponse.payload.ultraBonus;
        }

        if (IsScatterBonusTriggered(
                serverResponse.payload?.scatterBonus))
        {
            pendingScatterBonus = serverResponse.payload.scatterBonus;
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
        CaptureAuthoritativeBalance(serverResponse);

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
        pendingScatterBonus = null;
        pendingUltraBonus = null;
        manualStopRequested = false;
        slotView?.CancelSpin();
        CancelOptimisticBalanceTransaction();

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
               isUltraStartButtonReady &&
               !isUltraTakeReady;
    }

    internal bool ShouldShowUltraTakeButton()
    {
        return isUltraSlotUnlocked &&
               isUltraTakeReady;
    }

    internal bool CanTakeUltraWin()
    {
        return isInitialized &&
               isUltraSlotUnlocked &&
               isUltraTakeReady &&
               !isUltraSlotTransitioning &&
               !isUltraSlotSpinning &&
               !areUltraWheelsSpinning &&
               ultraWheelsCoroutine == null;
    }

    internal bool CanUseUltraStartButton()
    {
        if (!isInitialized ||
            !isUltraSlotUnlocked ||
            !isUltraStartButtonReady ||
            isUltraSlotTransitioning)
        {
            return false;
        }

        return !hasUltraSlotStarted
            ? CanStartUltraSlot()
            : CanStartUltraWheels();
    }

    internal bool RequestUltraStart()
    {
        return !hasUltraSlotStarted
            ? RequestUltraSlotStart()
            : RequestUltraWheelsStart();
    }

    internal bool RequestUltraTake()
    {
        if (!CanTakeUltraWin())
        {
            Debug.LogWarning(
                "[GameManager] Ultra Take ignored. The reward is not ready to be collected.");
            return false;
        }

        isUltraTakeReady = false;

        double totalRoundWin = latestSpinResult != null
            ? latestSpinResult.winAmount
            : displayedWinAmount;
        CompleteOptimisticBalanceTransaction(totalRoundWin);

        UltraWheelsCompleted?.Invoke();
        popupManager?.HideUltraWinImmediate();
        ResetUltraSlotState();
        GamePresentationChanged?.Invoke();

        Debug.Log(
            $"[GameManager] Ultra reward taken. Credited {Math.Max(0d, totalRoundWin):0.00}; " +
            "the normal Spin button is available again.");
        return true;
    }

    private bool CanStartUltraSlot()
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

    private bool RequestUltraSlotStart()
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

        popupManager?.HideUltraStartImmediate();
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
        AudioController.Instance?.PlayBonusReelThreeNumberIcon();
        isUltraSlotSpinning = false;

        if (ultraWheelsCoroutine != null)
        {
            StopCoroutine(ultraWheelsCoroutine);
        }
        ultraWheelsCoroutine = StartCoroutine(
            ShowUltraWheelsAfterResultAnimation());

        GamePresentationChanged?.Invoke();
        Debug.Log(
            "[GameManager] Ultra slot stopped on the server-provided reel states. " +
            "Waiting for the result animation before showing the Ultra wheels.");
    }

    private IEnumerator ShowUltraWheelsAfterResultAnimation()
    {
        bool animationCompleted = false;
        bool animationStarted =
            symbolAnimationManager != null &&
            symbolAnimationManager.PlayUltraWinningSymbolAnimations(
                ultraSlotView,
                () => animationCompleted = true);

        if (animationStarted)
        {
            while (isUltraSlotUnlocked &&
                   !animationCompleted)
            {
                yield return null;
            }
        }
        else
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
        }

        ultraWheelsCoroutine = null;
        if (!isUltraSlotUnlocked)
        {
            yield break;
        }

        PlayUltraWheelsEnterTransition();
    }

    private bool CanStartUltraWheels()
    {
        return isInitialized &&
               isUltraSlotUnlocked &&
               hasUltraSlotStarted &&
               !isUltraSlotSpinning &&
               areUltraWheelsReady &&
               !haveUltraWheelsStarted &&
               !areUltraWheelsSpinning &&
               ultraWheelsCoroutine == null;
    }

    private bool RequestUltraWheelsStart()
    {
        if (!CanStartUltraWheels())
        {
            Debug.LogWarning(
                "[GameManager] Ultra wheel Start ignored. The wheel panel is not ready " +
                "or the wheels have already started.");
            return false;
        }

        areUltraWheelsReady = false;
        haveUltraWheelsStarted = true;
        ultraWheelsCoroutine = StartCoroutine(SpinActiveUltraWheels());
        GamePresentationChanged?.Invoke();

        Debug.Log(
            "[GameManager] Ultra Start clicked again. Spinning the active Ultra wheels.");
        return true;
    }

    private IEnumerator SpinActiveUltraWheels()
    {
        // Let StartCoroutine assign its handle before this routine can complete
        // immediately because no valid active wheel was supplied.
        yield return null;

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

            int completedWheelNumber = wheelNumber;
            bool started = wheelController.SpinToServerStopIndex(
                serverWheel.stopIndex,
                () => CompleteUltraWheelStopPresentation(
                    completedWheelNumber,
                    () => completedWheelCount++));
            if (started)
            {
                startedWheelCount++;
            }
        }

        if (startedWheelCount == 0)
        {
            areUltraWheelsSpinning = false;
            yield return ShowUltraWinThenReturnToNormal();
            yield break;
        }

        // This clip belongs to the green, blue, and red Ultra prize wheels,
        // not the three-reel Ultra slot or the Scatter Wheel feature.
        AudioController.Instance?.PlayBonusReelSpinning();
        areUltraWheelsSpinning = true;
        GamePresentationChanged?.Invoke();

        while (isUltraSlotUnlocked &&
               completedWheelCount < startedWheelCount)
        {
            yield return null;
        }

        areUltraWheelsSpinning = false;
        if (!isUltraSlotUnlocked)
        {
            ultraWheelsCoroutine = null;
            yield break;
        }

        yield return ShowUltraWinThenReturnToNormal();
        Debug.Log(
            $"[GameManager] All {startedWheelCount} active Ultra wheel(s) finished.");
    }

    private void CompleteUltraWheelStopPresentation(
        int wheelNumber,
        Action onComplete)
    {
        if (!IsAnyUltraPrizeWheelSpinning())
        {
            AudioController.Instance?.StopBonusReelSpinning();
        }

        bool usesStopResultAnimation =
            wheelNumber == UltraSlotView.BlueWheelSymbolId ||
            wheelNumber == UltraSlotView.RedWheelSymbolId;
        bool animationStarted =
            usesStopResultAnimation &&
            symbolAnimationManager != null &&
            symbolAnimationManager.PlayUltraWheelStopResultAnimation(
                wheelNumber,
                onComplete);

        if (!animationStarted)
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator ShowUltraWinThenReturnToNormal()
    {
        bool totalWinCountCompleted = false;
        bool popupStarted =
            popupManager != null &&
            popupManager.ShowUltraWin(
                GetUltraWheelBaseAward(latestUltraBonus, 1),
                GetUltraWheelBaseAward(latestUltraBonus, 2),
                GetUltraWheelBaseAward(latestUltraBonus, 3),
                latestUltraBonus.totalAward,
                () => totalWinCountCompleted = true);

        if (popupStarted)
        {
            while (isUltraSlotUnlocked && !totalWinCountCompleted)
            {
                yield return null;
            }
        }
        else if (ultraWheelResultHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                ultraWheelResultHoldDuration);
        }
        else
        {
            yield return null;
        }

        ultraWheelsCoroutine = null;
        if (isUltraSlotUnlocked)
        {
            PrepareCompletedUltraWin();
            PlayUltraRewardReturnTransition();
        }
    }

    private static double? GetUltraWheelBaseAward(
        ServerUltraBonus ultraBonus,
        int wheelNumber)
    {
        if (ultraBonus?.activeWheels == null)
        {
            return null;
        }

        double? wheelAward = null;
        foreach (ServerUltraActiveWheel activeWheel in ultraBonus.activeWheels)
        {
            if (activeWheel != null &&
                activeWheel.wheelIndex == wheelNumber)
            {
                wheelAward = activeWheel.baseAward;
                break;
            }
        }

        return wheelAward;
    }

    private void PrepareCompletedUltraWin()
    {
        displayedWinAmount = latestUltraBonus.totalAward;

        GamePresentationChanged?.Invoke();
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
            if (resultSymbol >= UltraSlotView.GreenWheelSymbolId &&
                resultSymbol <= UltraSlotView.RedWheelSymbolId)
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
                return greenUltraWheel;
            case 2:
                return blueUltraWheel;
            case 3:
                return redUltraWheel;
            default:
                return null;
        }
    }

    /// <summary>
    /// Called by the later Ultra Wheel presentation after all awarded wheels finish.
    /// </summary>
    internal void CompleteUltraSlotFeature()
    {
        if (haveUltraWheelsStarted)
        {
            if (!RequestUltraTake())
            {
                Debug.LogWarning(
                    "[GameManager] Ultra feature cannot close until the reward " +
                    "transition finishes and Take is available.");
            }
            return;
        }

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
        if (!TryBuildUltraSlotResult(
                ultraBonus,
                out List<int> serverResult))
        {
            Debug.LogError(
                "[GameManager] Ultra was not opened because its complete " +
                "server reel result is unavailable or invalid.");
            return;
        }

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        AudioController.Instance?.PlayUltraWheelAllThree();
        popupManager?.HideUltraStartImmediate();
        popupManager?.HideUltraWinImmediate();
        latestUltraBonus = ultraBonus;
        ApplyUltraWheelResultValues(ultraBonus);
        pendingUltraSlotResult = serverResult;
        hasUltraSlotStarted = false;
        isUltraSlotSpinning = false;
        areUltraWheelsReady = false;
        haveUltraWheelsStarted = false;
        areUltraWheelsSpinning = false;
        isUltraStartButtonReady = false;
        isUltraTakeReady = false;
        isUltraSlotUnlocked = true;

        if (ultraWheelPanel != null)
        {
            ultraWheelPanel.SetActive(false);
        }

        if (ultraSlotView != null)
        {
            ultraSlotView.ShowConfiguredInitialResult();
        }

        PlayUltraTriggerAnimationThenEnter(ultraBonus);

        Debug.Log(
            $"[GameManager] Ultra slot unlocked by {source}. " +
            "Waiting for the Ultra Start button.");
    }

    private void ResetUltraSlotState()
    {
        KillUltraSlotTransition();
        AudioController.Instance?.StopUltraWheelSlotBackgroundMusic();
        popupManager?.HideUltraStartImmediate();
        popupManager?.HideUltraWinImmediate();
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
        areUltraWheelsReady = false;
        haveUltraWheelsStarted = false;
        areUltraWheelsSpinning = false;
        isUltraStartButtonReady = false;
        isUltraTakeReady = false;
        RestoreUltraSlotLayout();

        if (ultraSlotPanel != null)
        {
            ultraSlotPanel.SetActive(false);
        }
        if (ultraWheelPanel != null)
        {
            ultraWheelPanel.SetActive(false);
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

        symbolAnimationManager?.StopUltraWheelStopResultAnimations();

        var stoppedControllers = new HashSet<UltraWheelController>();
        UltraWheelController[] wheelControllers =
        {
            greenUltraWheel,
            blueUltraWheel,
            redUltraWheel
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
        AudioController.Instance?.StopBonusReelSpinning();
    }

    private bool IsAnyUltraPrizeWheelSpinning()
    {
        return (greenUltraWheel != null && greenUltraWheel.IsSpinning) ||
               (blueUltraWheel != null && blueUltraWheel.IsSpinning) ||
               (redUltraWheel != null && redUltraWheel.IsSpinning);
    }

    private void PlayUltraTriggerAnimationThenEnter(ServerUltraBonus ultraBonus)
    {
        KillUltraSlotTransition();
        isUltraSlotTransitioning = true;

        List<int> ultraWheelPositions = GetUltraWheelTriggerPositions(ultraBonus);
        bool startedAnimation =
            slotView != null &&
            slotView.ShowPrioritySymbolAnimationLoops(
                ultraWheelPositions,
                StPatricksGoldSymbolIds.UltraWheel,
                2,
                () =>
                {
                    if (isUltraSlotUnlocked)
                    {
                        PlayUltraEntryFrameTransition();
                    }
                });

        if (!startedAnimation)
        {
            PlayUltraEntryFrameTransition();
            return;
        }

        GamePresentationChanged?.Invoke();
    }

    private void PlayUltraEntryFrameTransition()
    {
        KillUltraSlotTransition();

        if (!isUltraSlotUnlocked)
        {
            return;
        }

        CacheUltraSlotLayout();
        RestoreUltraSlotLayout();
        popupManager?.HideUltraStartImmediate();
        isUltraStartButtonReady = false;
        isUltraSlotTransitioning = true;
        normalSlotPanel?.SetActive(true);
        ultraSlotPanel?.SetActive(false);
        ultraWheelPanel?.SetActive(false);

        bool animationStarted =
            symbolAnimationManager != null &&
            symbolAnimationManager.PlayUltraEntryTransition(
                OnUltraEntryTransitionMidpoint,
                OnUltraEntryTransitionComplete);
        if (!animationStarted)
        {
            Debug.LogError(
                "[GameManager] The animation manager could not start the " +
                "Ultra entry transition. The panels will swap immediately.");
            OnUltraEntryTransitionMidpoint();
            OnUltraEntryTransitionComplete();
        }
        else
        {
            AudioController.Instance?.PlayLeavesFalling();
        }

        GamePresentationChanged?.Invoke();
    }

    private void OnUltraEntryTransitionMidpoint()
    {
        if (isUltraSlotUnlocked)
        {
            SwapToUltraSlotPanels();
        }
    }

    private void OnUltraEntryTransitionComplete()
    {
        isUltraSlotTransitioning = false;

        if (!isUltraSlotUnlocked)
        {
            return;
        }

        AudioController.Instance?.PlayUltraWheelSlotBackgroundMusic();

        bool popupStarted =
            popupManager != null &&
            popupManager.ShowUltraStart(() =>
            {
                if (!isUltraSlotUnlocked)
                {
                    return;
                }

                isUltraStartButtonReady = true;
                GamePresentationChanged?.Invoke();
            });

        if (!popupStarted)
        {
            isUltraStartButtonReady = true;
        }

        GamePresentationChanged?.Invoke();
    }

    private void SwapToUltraSlotPanels()
    {
        normalSlotPanel?.SetActive(false);
        ultraSlotPanel?.SetActive(true);
        ultraWheelPanel?.SetActive(false);
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

        return positions;
    }

    private void CacheUltraSlotLayout()
    {
        if (!hasCachedUltraSlotLayout)
        {
            normalSlotRectTransform = normalSlotPanel != null
                ? normalSlotPanel.GetComponent<RectTransform>()
                : null;
            ultraSlotRectTransform = ultraSlotPanel != null
                ? ultraSlotPanel.GetComponent<RectTransform>()
                : null;

            if (normalSlotRectTransform != null && ultraSlotRectTransform != null)
            {
                normalSlotRestingPosition = normalSlotRectTransform.anchoredPosition;
                ultraSlotRestingPosition = ultraSlotRectTransform.anchoredPosition;
                hasCachedUltraSlotLayout = true;
            }
        }

        if (!hasCachedUltraWheelLayout)
        {
            ultraWheelRectTransform = ultraWheelPanel != null
                ? ultraWheelPanel.GetComponent<RectTransform>()
                : null;

            if (ultraWheelRectTransform != null)
            {
                ultraWheelRestingPosition = ultraWheelRectTransform.anchoredPosition;
                hasCachedUltraWheelLayout = true;
            }
        }

        if (!hasCachedUltraWheelItemLayout)
        {
            greenUltraWheelRectTransform = greenUltraWheel != null
                ? greenUltraWheel.GetComponent<RectTransform>()
                : null;
            blueUltraWheelRectTransform = blueUltraWheel != null
                ? blueUltraWheel.GetComponent<RectTransform>()
                : null;
            redUltraWheelRectTransform = redUltraWheel != null
                ? redUltraWheel.GetComponent<RectTransform>()
                : null;

            if (greenUltraWheelRectTransform != null &&
                blueUltraWheelRectTransform != null &&
                redUltraWheelRectTransform != null)
            {
                greenUltraWheelRestingPosition =
                    greenUltraWheelRectTransform.anchoredPosition;
                blueUltraWheelRestingPosition =
                    blueUltraWheelRectTransform.anchoredPosition;
                redUltraWheelRestingPosition =
                    redUltraWheelRectTransform.anchoredPosition;
                hasCachedUltraWheelItemLayout = true;
            }
        }
    }

    private void PlayUltraWheelsEnterTransition()
    {
        KillUltraSlotTransition();
        CacheUltraSlotLayout();

        if (!isUltraSlotUnlocked || ultraWheelPanel == null)
        {
            Debug.LogError(
                "[GameManager] Cannot show the Ultra wheels because their panel is not assigned.");
            GamePresentationChanged?.Invoke();
            return;
        }

        AudioController.Instance?.StopUltraWheelSlotBackgroundMusic();

        areUltraWheelsReady = false;

        if (!hasCachedUltraSlotLayout ||
            !hasCachedUltraWheelLayout ||
            !hasCachedUltraWheelItemLayout)
        {
            ultraSlotPanel?.SetActive(false);
            ultraWheelPanel.SetActive(true);
            areUltraWheelsReady = true;
            GamePresentationChanged?.Invoke();
            Debug.LogWarning(
                "[GameManager] Ultra wheel transition requires RectTransforms; used an instant swap instead.");
            return;
        }

        isUltraSlotTransitioning = true;
        ultraSlotPanel.SetActive(true);
        ultraWheelPanel.SetActive(true);

        ultraSlotRectTransform.anchoredPosition = ultraSlotRestingPosition;
        ultraWheelRectTransform.anchoredPosition = ultraWheelRestingPosition;

        greenUltraWheelRectTransform.anchoredPosition =
            greenUltraWheelRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(
                greenUltraWheelRectTransform);
        blueUltraWheelRectTransform.anchoredPosition =
            blueUltraWheelRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(
                blueUltraWheelRectTransform);
        redUltraWheelRectTransform.anchoredPosition =
            redUltraWheelRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(
                redUltraWheelRectTransform);

        float duration = Mathf.Max(0.01f, ultraSlotTransitionDuration);
        float shakeDuration = Mathf.Max(0f, ultraSlotExitShakeDuration);
        float shakeStrength = Mathf.Max(0f, ultraSlotExitShakeStrength);
        float effectiveShakeDuration = 0f;
        Vector2 ultraSlotExitPosition =
            ultraSlotRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(
                ultraSlotRectTransform);

        ultraSlotTransitionSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (shakeDuration > 0f && shakeStrength > 0f)
        {
            float shakeStepDuration =
                Mathf.Max(0.01f, shakeDuration / 4f);
            effectiveShakeDuration = shakeStepDuration * 4f;
            ultraSlotTransitionSequence
                .Append(
                    ultraSlotRectTransform
                        .DOAnchorPos(
                            ultraSlotRestingPosition +
                            Vector2.right * shakeStrength,
                            shakeStepDuration)
                        .SetEase(Ease.Linear))
                .Append(
                    ultraSlotRectTransform
                        .DOAnchorPos(
                            ultraSlotRestingPosition +
                            Vector2.left * shakeStrength,
                            shakeStepDuration)
                        .SetEase(Ease.Linear))
                .Append(
                    ultraSlotRectTransform
                        .DOAnchorPos(
                            ultraSlotRestingPosition +
                            Vector2.right * (shakeStrength * 0.5f),
                            shakeStepDuration)
                        .SetEase(Ease.Linear))
                .Append(
                    ultraSlotRectTransform
                        .DOAnchorPos(
                            ultraSlotRestingPosition,
                            shakeStepDuration)
                        .SetEase(Ease.Linear));
        }

        ultraSlotTransitionSequence
            .Append(
                ultraSlotRectTransform
                    .DOAnchorPos(
                        ultraSlotExitPosition,
                        duration)
                    .SetEase(Ease.InCubic))
            .AppendCallback(() => ultraSlotPanel.SetActive(false));

        float greenRevealTime =
            effectiveShakeDuration +
            duration +
            Mathf.Max(0f, ultraFirstWheelRevealDelay);
        float revealStagger =
            Mathf.Max(0f, ultraWheelRevealStagger);

        ultraSlotTransitionSequence
            .Insert(
                greenRevealTime,
                greenUltraWheelRectTransform
                    .DOAnchorPos(
                        greenUltraWheelRestingPosition,
                        duration)
                    .SetEase(Ease.OutCubic))
            .Insert(
                greenRevealTime + revealStagger,
                blueUltraWheelRectTransform
                    .DOAnchorPos(
                        blueUltraWheelRestingPosition,
                        duration)
                    .SetEase(Ease.OutCubic))
            .Insert(
                greenRevealTime + (revealStagger * 2f),
                redUltraWheelRectTransform
                    .DOAnchorPos(
                        redUltraWheelRestingPosition,
                        duration)
                    .SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                ultraSlotTransitionSequence = null;
                isUltraSlotTransitioning = false;
                areUltraWheelsReady = true;
                GamePresentationChanged?.Invoke();
            });

        GamePresentationChanged?.Invoke();
    }

    private void PlayUltraRewardReturnTransition()
    {
        if (!isUltraSlotUnlocked)
        {
            return;
        }

        KillUltraSlotTransition();
        slotView?.CancelWinAnimation();
        StopActiveUltraWheels();
        CacheUltraSlotLayout();
        AudioController.Instance?.PlayBonusGoingDown();

        isUltraTakeReady = false;
        areUltraWheelsReady = false;

        if (!hasCachedUltraSlotLayout ||
            !hasCachedUltraWheelLayout ||
            ultraWheelPanel == null ||
            normalSlotPanel == null)
        {
            RestoreUltraSlotLayout();
            ultraSlotPanel?.SetActive(false);
            ultraWheelPanel?.SetActive(false);
            normalSlotPanel?.SetActive(true);
            isUltraSlotTransitioning = false;
            isUltraTakeReady = true;
            GamePresentationChanged?.Invoke();
            Debug.LogWarning(
                "[GameManager] Ultra reward return requires panel RectTransforms; " +
                "used an instant swap before enabling Take.");
            return;
        }

        isUltraSlotTransitioning = true;
        ultraWheelPanel.SetActive(true);
        normalSlotPanel.SetActive(true);
        ultraWheelRectTransform.anchoredPosition =
            ultraWheelRestingPosition;
        normalSlotRectTransform.anchoredPosition =
            normalSlotRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(
                normalSlotRectTransform);

        float shakeDuration = Mathf.Max(0f, ultraRewardShakeDuration);
        float shakeStrength = Mathf.Max(0f, ultraRewardShakeStrength);
        float slideDuration = Mathf.Max(0.01f, ultraRewardExitDuration);
        Vector2 wheelExitPosition =
            ultraWheelRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(
                ultraWheelRectTransform);

        ultraSlotTransitionSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (shakeDuration > 0f && shakeStrength > 0f)
        {
            float shakeStepDuration =
                Mathf.Max(0.01f, shakeDuration / 4f);
            ultraSlotTransitionSequence
                .Append(
                    ultraWheelRectTransform
                        .DOAnchorPos(
                            ultraWheelRestingPosition +
                            Vector2.right * shakeStrength,
                            shakeStepDuration)
                        .SetEase(Ease.Linear))
                .Append(
                    ultraWheelRectTransform
                        .DOAnchorPos(
                            ultraWheelRestingPosition +
                            Vector2.left * shakeStrength,
                            shakeStepDuration)
                        .SetEase(Ease.Linear))
                .Append(
                    ultraWheelRectTransform
                        .DOAnchorPos(
                            ultraWheelRestingPosition +
                            Vector2.right * (shakeStrength * 0.5f),
                            shakeStepDuration)
                        .SetEase(Ease.Linear))
                .Append(
                    ultraWheelRectTransform
                        .DOAnchorPos(
                            ultraWheelRestingPosition,
                            shakeStepDuration)
                        .SetEase(Ease.Linear));
        }

        ultraSlotTransitionSequence
            .Append(
                ultraWheelRectTransform
                    .DOAnchorPos(
                        wheelExitPosition,
                        slideDuration)
                    .SetEase(Ease.InCubic))
            .AppendCallback(
                () => ultraWheelPanel.SetActive(false))
            .Append(
                normalSlotRectTransform
                    .DOAnchorPos(
                        normalSlotRestingPosition,
                        slideDuration)
                    .SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                ultraSlotTransitionSequence = null;
                isUltraSlotTransitioning = false;
                isUltraTakeReady = true;
                GamePresentationChanged?.Invoke();
                Debug.Log(
                    "[GameManager] Normal slot restored. The Ultra reward remains " +
                    "visible and the Take button is now active.");
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

        bool exitFromUltraWheels =
            ultraWheelPanel != null && ultraWheelPanel.activeSelf;
        GameObject outgoingPanel =
            exitFromUltraWheels ? ultraWheelPanel : ultraSlotPanel;
        RectTransform outgoingRectTransform =
            exitFromUltraWheels ? ultraWheelRectTransform : ultraSlotRectTransform;
        Vector2 outgoingRestingPosition =
            exitFromUltraWheels ? ultraWheelRestingPosition : ultraSlotRestingPosition;
        bool hasOutgoingLayout =
            exitFromUltraWheels ? hasCachedUltraWheelLayout : hasCachedUltraSlotLayout;

        if (!hasCachedUltraSlotLayout ||
            !hasOutgoingLayout ||
            outgoingPanel == null ||
            outgoingRectTransform == null)
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
        areUltraWheelsReady = false;
        isUltraSlotTransitioning = true;

        outgoingPanel.SetActive(true);
        normalSlotPanel.SetActive(true);
        outgoingRectTransform.anchoredPosition = outgoingRestingPosition;
        normalSlotRectTransform.anchoredPosition =
            normalSlotRestingPosition +
            Vector2.down * GetUltraSlotSlideDistance(normalSlotRectTransform);

        float duration = Mathf.Max(0.01f, ultraSlotTransitionDuration);
        ultraSlotTransitionSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                outgoingRectTransform
                    .DOAnchorPos(
                        outgoingRestingPosition +
                        Vector2.down * GetUltraSlotSlideDistance(outgoingRectTransform),
                        duration)
                    .SetEase(Ease.InCubic))
            .AppendCallback(() => outgoingPanel.SetActive(false))
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

        if (hasCachedUltraWheelLayout)
        {
            ultraWheelRectTransform.anchoredPosition = ultraWheelRestingPosition;
        }

        if (hasCachedUltraWheelItemLayout)
        {
            greenUltraWheelRectTransform.anchoredPosition =
                greenUltraWheelRestingPosition;
            blueUltraWheelRectTransform.anchoredPosition =
                blueUltraWheelRestingPosition;
            redUltraWheelRectTransform.anchoredPosition =
                redUltraWheelRestingPosition;
        }
    }

    private void KillUltraSlotTransition()
    {
        symbolAnimationManager?.StopUltraEntryTransition();
        symbolAnimationManager?.StopUltraWinningSymbolAnimations();

        if (ultraSlotTransitionSequence != null)
        {
            ultraSlotTransitionSequence.Kill();
            ultraSlotTransitionSequence = null;
        }

        isUltraSlotTransitioning = false;
    }

    private bool TryCaptureServerBonusData(
        string rawJson,
        ServerSpinResponse serverResponse,
        out string error)
    {
        error = null;
        bool decodedSuccessfully = false;
        object decoded = Json.Decode(rawJson, ref decodedSuccessfully);
        if (!decodedSuccessfully ||
            !(decoded is IDictionary<string, object> root))
        {
            error =
                "The raw server response could not be decoded for strict " +
                "bonus-result validation.";
            return false;
        }

        if (!root.TryGetValue("payload", out object payloadValue) ||
            !(payloadValue is IDictionary<string, object> rawPayload))
        {
            error =
                "The raw server response contains no payload object.";
            return false;
        }

        ServerPayload payload = serverResponse?.payload;
        if (payload?.ultraBonus != null)
        {
            if (!rawPayload.TryGetValue(
                    "ultraBonus",
                    out object ultraBonusValue) ||
                !(ultraBonusValue is
                  IDictionary<string, object> rawUltraBonus) ||
                !TryCaptureUltraBonus(
                    rawUltraBonus,
                    payload.ultraBonus,
                    out error))
            {
                error ??=
                    "The raw server response contains an invalid ultraBonus.";
                return false;
            }
        }

        if (payload?.scatterBonus != null)
        {
            if (!rawPayload.TryGetValue(
                    "scatterBonus",
                    out object scatterBonusValue) ||
                !(scatterBonusValue is
                  IDictionary<string, object> rawScatterBonus) ||
                !TryCaptureScatterBonus(
                    rawScatterBonus,
                    payload.scatterBonus,
                    out error))
            {
                error ??=
                    "The raw server response contains an invalid scatterBonus.";
                return false;
            }
        }

        return true;
    }

    private static bool TryCaptureUltraBonus(
        IDictionary<string, object> rawBonus,
        ServerUltraBonus bonus,
        out string error)
    {
        error = null;
        bonus.hasIsTriggered =
            TryReadRawBool(
                rawBonus,
                "isTriggered",
                out bool isTriggered);
        if (bonus.hasIsTriggered)
        {
            bonus.isTriggered = isTriggered;
        }

        bonus.hasTotalAward =
            TryReadRawDouble(
                rawBonus,
                "totalAward",
                out double totalAward);
        if (bonus.hasTotalAward)
        {
            bonus.totalAward = totalAward;
        }

        if (!TryReadRawPositions(
                rawBonus,
                "triggerPositions",
                out List<ServerGridPosition> triggerPositions,
                out error))
        {
            return false;
        }

        bonus.triggerPositions = triggerPositions;

        if (!rawBonus.TryGetValue(
                "reelResults",
                out object reelResultsValue) ||
            !(reelResultsValue is IList rawReelResults))
        {
            error =
                "ultraBonus.reelResults must be a server array.";
            return false;
        }

        bonus.reelResults =
            new List<ServerUltraReelResult>(rawReelResults.Count);
        for (int index = 0;
             index < rawReelResults.Count;
             index++)
        {
            if (!(rawReelResults[index] is
                  IDictionary<string, object> rawReelResult))
            {
                error =
                    $"ultraBonus.reelResults[{index}] must be an object.";
                return false;
            }

            var reelResult = new ServerUltraReelResult();
            reelResult.hasReelIndex =
                TryReadRawInt(
                    rawReelResult,
                    "reelIndex",
                    out reelResult.reelIndex);
            reelResult.hasBonusWheelStopIndex =
                TryReadRawInt(
                    rawReelResult,
                    "bonusWheelStopIndex",
                    out reelResult.bonusWheelStopIndex);
            reelResult.hasAssignedWheelIndex =
                TryReadRawInt(
                    rawReelResult,
                    "assignedWheelIndex",
                    out reelResult.assignedWheelIndex);
            reelResult.hasWheelState =
                TryReadRawString(
                    rawReelResult,
                    "wheelState",
                    out reelResult.wheelState);
            reelResult.hasMultiplierValue =
                TryReadRawInt(
                    rawReelResult,
                    "multiplierValue",
                    out reelResult.multiplierValue);
            bonus.reelResults.Add(reelResult);
        }

        if (!rawBonus.TryGetValue(
                "activeWheels",
                out object activeWheelsValue) ||
            !(activeWheelsValue is IList rawActiveWheels))
        {
            error =
                "ultraBonus.activeWheels must be a server array.";
            return false;
        }

        bonus.activeWheels =
            new List<ServerUltraActiveWheel>(rawActiveWheels.Count);
        for (int index = 0;
             index < rawActiveWheels.Count;
             index++)
        {
            if (!(rawActiveWheels[index] is
                  IDictionary<string, object> rawActiveWheel))
            {
                error =
                    $"ultraBonus.activeWheels[{index}] must be an object.";
                return false;
            }

            var activeWheel = new ServerUltraActiveWheel();
            activeWheel.hasWheelIndex =
                TryReadRawInt(
                    rawActiveWheel,
                    "wheelIndex",
                    out activeWheel.wheelIndex);
            activeWheel.hasStopIndex =
                TryReadRawInt(
                    rawActiveWheel,
                    "stopIndex",
                    out activeWheel.stopIndex);
            activeWheel.hasBaseAward =
                TryReadRawDouble(
                    rawActiveWheel,
                    "baseAward",
                    out activeWheel.baseAward);
            activeWheel.hasFinalAward =
                TryReadRawDouble(
                    rawActiveWheel,
                    "finalAward",
                    out activeWheel.finalAward);

            if (rawActiveWheel.TryGetValue(
                    "awards",
                    out object awardsValue))
            {
                if (!TryReadRawIntList(
                        awardsValue,
                        out activeWheel.awards))
                {
                    error =
                        $"ultraBonus.activeWheels[{index}].awards must be an " +
                        "integer array.";
                    return false;
                }
            }

            bonus.activeWheels.Add(activeWheel);
        }

        return true;
    }

    private static bool TryCaptureScatterBonus(
        IDictionary<string, object> rawBonus,
        ServerScatterBonus bonus,
        out string error)
    {
        error = null;
        bonus.hasIsTriggered =
            TryReadRawBool(
                rawBonus,
                "isTriggered",
                out bool isTriggered);
        if (bonus.hasIsTriggered)
        {
            bonus.isTriggered = isTriggered;
        }

        bonus.hasTotalAward =
            TryReadRawDouble(
                rawBonus,
                "totalAward",
                out double totalAward);
        if (bonus.hasTotalAward)
        {
            bonus.totalAward = totalAward;
        }

        if (!TryReadRawPositions(
                rawBonus,
                "triggerPositions",
                out List<ServerGridPosition> triggerPositions,
                out error))
        {
            return false;
        }

        bonus.triggerPositions = triggerPositions;

        if (!rawBonus.TryGetValue(
                "wheelSpins",
                out object wheelSpinsValue) ||
            !(wheelSpinsValue is IList rawWheelSpins))
        {
            error =
                "scatterBonus.wheelSpins must be a server array.";
            return false;
        }

        bonus.wheelSpins =
            new List<ServerScatterWheelSpin>(rawWheelSpins.Count);
        for (int index = 0;
             index < rawWheelSpins.Count;
             index++)
        {
            if (!(rawWheelSpins[index] is
                  IDictionary<string, object> rawWheelSpin))
            {
                error =
                    $"scatterBonus.wheelSpins[{index}] must be an object.";
                return false;
            }

            var wheelSpin = new ServerScatterWheelSpin();
            bool hasRow =
                TryReadRawInt(
                    rawWheelSpin,
                    "row",
                    out wheelSpin.row);
            bool hasCol =
                TryReadRawInt(
                    rawWheelSpin,
                    "col",
                    out wheelSpin.col);
            wheelSpin.hasPosition = hasRow && hasCol;
            wheelSpin.hasWheelIndex =
                TryReadRawInt(
                    rawWheelSpin,
                    "wheelIndex",
                    out wheelSpin.wheelIndex);
            wheelSpin.hasStopIndex =
                TryReadRawInt(
                    rawWheelSpin,
                    "stopIndex",
                    out wheelSpin.stopIndex);
            wheelSpin.hasAwardValue =
                TryReadRawDouble(
                    rawWheelSpin,
                    "awardValue",
                    out wheelSpin.awardValue);

            if (rawWheelSpin.TryGetValue(
                    "awards",
                    out object awardsValue))
            {
                if (!TryReadRawDoubleList(
                        awardsValue,
                        out wheelSpin.awards))
                {
                    error =
                        $"scatterBonus.wheelSpins[{index}].awards must be a " +
                        "numeric array.";
                    return false;
                }
            }

            if (rawWheelSpin.TryGetValue(
                    "values",
                    out object valuesValue))
            {
                if (!TryReadRawStringList(
                        valuesValue,
                        out wheelSpin.values))
                {
                    error =
                        $"scatterBonus.wheelSpins[{index}].values must be a " +
                        "non-null array.";
                    return false;
                }
            }

            bonus.wheelSpins.Add(wheelSpin);
        }

        return true;
    }

    private static bool TryReadRawPositions(
        IDictionary<string, object> values,
        string key,
        out List<ServerGridPosition> positions,
        out string error)
    {
        positions = new List<ServerGridPosition>();
        error = null;
        if (!values.TryGetValue(key, out object rawValue))
        {
            return true;
        }

        if (!(rawValue is IList rawPositions))
        {
            error = $"{key} must be a server array.";
            return false;
        }

        positions =
            new List<ServerGridPosition>(rawPositions.Count);
        for (int index = 0;
             index < rawPositions.Count;
             index++)
        {
            if (!(rawPositions[index] is
                  IDictionary<string, object> rawPosition))
            {
                error = $"{key}[{index}] must be an object.";
                return false;
            }

            var position = new ServerGridPosition();
            position.hasRow =
                TryReadRawInt(
                    rawPosition,
                    "row",
                    out position.row);
            position.hasCol =
                TryReadRawInt(
                    rawPosition,
                    "col",
                    out position.col);
            positions.Add(position);
        }

        return true;
    }

    private static bool TryReadRawBool(
        IDictionary<string, object> values,
        string key,
        out bool result)
    {
        result = false;
        if (!values.TryGetValue(key, out object value) ||
            value == null)
        {
            return false;
        }

        if (value is bool boolean)
        {
            result = boolean;
            return true;
        }

        return bool.TryParse(
            Convert.ToString(value, CultureInfo.InvariantCulture),
            out result);
    }

    private static bool TryReadRawInt(
        IDictionary<string, object> values,
        string key,
        out int result)
    {
        result = 0;
        if (!values.TryGetValue(key, out object value) ||
            value == null)
        {
            return false;
        }

        try
        {
            double numeric =
                Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (double.IsNaN(numeric) ||
                double.IsInfinity(numeric) ||
                numeric != Math.Truncate(numeric) ||
                numeric < int.MinValue ||
                numeric > int.MaxValue)
            {
                return false;
            }

            result = (int)numeric;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryReadRawDouble(
        IDictionary<string, object> values,
        string key,
        out double result)
    {
        result = 0d;
        if (!values.TryGetValue(key, out object value) ||
            value == null)
        {
            return false;
        }

        try
        {
            result =
                Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return !double.IsNaN(result) &&
                   !double.IsInfinity(result);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryReadRawString(
        IDictionary<string, object> values,
        string key,
        out string result)
    {
        result = null;
        if (!values.TryGetValue(key, out object value) ||
            value == null)
        {
            return false;
        }

        result =
            Convert.ToString(value, CultureInfo.InvariantCulture);
        return !string.IsNullOrWhiteSpace(result);
    }

    private static bool TryReadRawIntList(
        object value,
        out List<int> result)
    {
        result = null;
        if (!(value is IList rawValues))
        {
            return false;
        }

        result = new List<int>(rawValues.Count);
        for (int index = 0;
             index < rawValues.Count;
             index++)
        {
            var wrapper = new Dictionary<string, object>
            {
                { "value", rawValues[index] }
            };
            if (!TryReadRawInt(wrapper, "value", out int parsed))
            {
                return false;
            }

            result.Add(parsed);
        }

        return true;
    }

    private static bool TryReadRawDoubleList(
        object value,
        out List<double> result)
    {
        result = null;
        if (!(value is IList rawValues))
        {
            return false;
        }

        result = new List<double>(rawValues.Count);
        for (int index = 0;
             index < rawValues.Count;
             index++)
        {
            var wrapper = new Dictionary<string, object>
            {
                { "value", rawValues[index] }
            };
            if (!TryReadRawDouble(
                    wrapper,
                    "value",
                    out double parsed))
            {
                return false;
            }

            result.Add(parsed);
        }

        return true;
    }

    private static bool TryReadRawStringList(
        object value,
        out List<string> result)
    {
        result = null;
        if (!(value is IList rawValues))
        {
            return false;
        }

        result = new List<string>(rawValues.Count);
        for (int index = 0;
             index < rawValues.Count;
             index++)
        {
            if (rawValues[index] == null)
            {
                return false;
            }

            result.Add(
                Convert.ToString(
                    rawValues[index],
                    CultureInfo.InvariantCulture));
        }

        return true;
    }

    private bool TryValidateServerFeatureResults(
        ServerSpinResponse serverResponse,
        List<List<int>> resultMatrix,
        out string error)
    {
        error = null;
        ServerPayload payload = serverResponse?.payload;
        bool ultraSignalled =
            payload?.ultraBonus?.isTriggered == true ||
            payload?.features?.ultraWheel?.triggered == true;
        bool scatterSignalled =
            payload?.scatterBonus?.isTriggered == true ||
            payload?.scatterTriggered == true ||
            payload?.features?.scatterWheel?.triggered == true;

        if (ultraSignalled && scatterSignalled)
        {
            error =
                "The server response triggered Ultra and Scatter in the same " +
                "round. No bonus result will be displayed.";
            return false;
        }

        if (ultraSignalled &&
            !TryValidateUltraBonusResult(
                payload?.ultraBonus,
                resultMatrix,
                out error))
        {
            return false;
        }

        if (scatterSignalled &&
            !TryValidateScatterBonusResult(
                payload?.scatterBonus,
                resultMatrix,
                out error))
        {
            return false;
        }

        return true;
    }

    private bool TryValidateUltraBonusResult(
        ServerUltraBonus ultraBonus,
        List<List<int>> resultMatrix,
        out string error)
    {
        error = null;
        if (stPatricksGoldConfig == null || resultMatrix == null)
        {
            error =
                "The server Ultra result cannot be validated without the " +
                "server game configuration and reel matrix.";
            return false;
        }

        if (ultraBonus == null)
        {
            error =
                "The server triggered Ultra without an ultraBonus result.";
            return false;
        }

        if (!ultraBonus.hasIsTriggered || !ultraBonus.isTriggered)
        {
            error =
                "The server Ultra result must explicitly contain " +
                "isTriggered=true.";
            return false;
        }

        if (!ultraBonus.hasTotalAward ||
            !IsFiniteNonNegative(ultraBonus.totalAward))
        {
            error =
                "The server Ultra result is missing a valid totalAward.";
            return false;
        }

        int triggerSymbolCount = 0;
        foreach (List<int> column in resultMatrix)
        {
            if (column == null)
            {
                continue;
            }

            foreach (int symbolId in column)
            {
                if (symbolId == stPatricksGoldConfig.ultraWheelSymbolId)
                {
                    triggerSymbolCount++;
                }
            }
        }

        int requiredTriggerCount = Math.Max(
            1,
            stPatricksGoldConfig.ultraWheel?.minTriggerCount ?? 1);
        if (triggerSymbolCount < requiredTriggerCount)
        {
            error =
                $"The server Ultra trigger is inconsistent with its reel " +
                $"matrix: found {triggerSymbolCount} Ultra symbols, expected " +
                $"at least {requiredTriggerCount}.";
            return false;
        }

        if (ultraBonus.reelResults == null ||
            ultraBonus.reelResults.Count != UltraSlotView.ReelCount)
        {
            error =
                $"The server Ultra result must contain exactly " +
                $"{UltraSlotView.ReelCount} reelResults.";
            return false;
        }

        var assignedReels = new HashSet<int>();
        var assignedActiveWheels = new HashSet<int>();
        foreach (ServerUltraReelResult reelResult in ultraBonus.reelResults)
        {
            if (reelResult == null ||
                !reelResult.hasReelIndex ||
                !reelResult.hasBonusWheelStopIndex ||
                !reelResult.hasWheelState)
            {
                error =
                    "Every server Ultra reelResult must explicitly contain " +
                    "reelIndex, bonusWheelStopIndex, and wheelState.";
                return false;
            }

            if (reelResult.reelIndex < 1 ||
                reelResult.reelIndex > UltraSlotView.ReelCount ||
                !assignedReels.Add(reelResult.reelIndex))
            {
                error =
                    $"The server Ultra reelIndex {reelResult.reelIndex} is " +
                    "invalid or duplicated.";
                return false;
            }

            if (!TryResolveUltraWheelState(
                    reelResult.wheelState,
                    out bool isActive,
                    out bool hasStateMultiplier))
            {
                error =
                    $"The server Ultra reel {reelResult.reelIndex} has an " +
                    $"unsupported wheelState '{reelResult.wheelState}'.";
                return false;
            }

            if (hasStateMultiplier &&
                reelResult.hasMultiplierValue &&
                reelResult.multiplierValue < 1)
            {
                error =
                    $"The server Ultra reel {reelResult.reelIndex} contains " +
                    $"an invalid multiplierValue {reelResult.multiplierValue}.";
                return false;
            }

            if (!isActive)
            {
                continue;
            }

            if (!reelResult.hasAssignedWheelIndex ||
                reelResult.assignedWheelIndex <
                    UltraSlotView.GreenWheelSymbolId ||
                reelResult.assignedWheelIndex >
                    UltraSlotView.RedWheelSymbolId ||
                !assignedActiveWheels.Add(
                    reelResult.assignedWheelIndex))
            {
                error =
                    $"The active server Ultra reel {reelResult.reelIndex} " +
                    "must contain one unique assignedWheelIndex from 1 to 3.";
                return false;
            }
        }

        if (assignedActiveWheels.Count == 0)
        {
            error =
                "The server triggered Ultra without an active wheel.";
            return false;
        }

        if (ultraBonus.activeWheels == null ||
            ultraBonus.activeWheels.Count != assignedActiveWheels.Count)
        {
            error =
                "The server Ultra activeWheels results do not exactly match " +
                "the active reel assignments.";
            return false;
        }

        var receivedActiveWheels = new HashSet<int>();
        double calculatedTotal = 0d;
        foreach (ServerUltraActiveWheel activeWheel in
                 ultraBonus.activeWheels)
        {
            if (activeWheel == null ||
                !activeWheel.hasWheelIndex ||
                !activeWheel.hasStopIndex ||
                !activeWheel.hasBaseAward ||
                !activeWheel.hasFinalAward)
            {
                error =
                    "Every server Ultra activeWheel must explicitly contain " +
                    "wheelIndex, stopIndex, baseAward, and finalAward.";
                return false;
            }

            if (!assignedActiveWheels.Contains(activeWheel.wheelIndex) ||
                !receivedActiveWheels.Add(activeWheel.wheelIndex))
            {
                error =
                    $"Server Ultra wheel {activeWheel.wheelIndex} is not " +
                    "assigned by the reel result or is duplicated.";
                return false;
            }

            if (activeWheel.stopIndex < 0 ||
                activeWheel.stopIndex >=
                    UltraWheelController.ServerValueCount)
            {
                error =
                    $"Server Ultra wheel {activeWheel.wheelIndex} has invalid " +
                    $"stopIndex {activeWheel.stopIndex}.";
                return false;
            }

            if (!IsFiniteNonNegative(activeWheel.baseAward) ||
                !IsFiniteNonNegative(activeWheel.finalAward))
            {
                error =
                    $"Server Ultra wheel {activeWheel.wheelIndex} contains " +
                    "an invalid award.";
                return false;
            }

            IReadOnlyList<int> serverValues =
                GetUltraWheelServerValues(activeWheel);
            if (serverValues == null ||
                serverValues.Count !=
                    UltraWheelController.ServerValueCount)
            {
                error =
                    $"Server Ultra wheel {activeWheel.wheelIndex} has no " +
                    $"complete {UltraWheelController.ServerValueCount}-value " +
                    "server table.";
                return false;
            }

            for (int valueIndex = 0;
                 valueIndex < serverValues.Count;
                 valueIndex++)
            {
                if (serverValues[valueIndex] < 0)
                {
                    error =
                        $"Server Ultra wheel {activeWheel.wheelIndex} contains " +
                        $"a negative value at index {valueIndex}.";
                    return false;
                }
            }

            if (activeWheel.awards == null)
            {
                activeWheel.awards =
                    new List<int>(serverValues);
            }

            if (!AmountsMatch(
                    serverValues[activeWheel.stopIndex],
                    activeWheel.baseAward))
            {
                error =
                    $"Server Ultra wheel {activeWheel.wheelIndex} contains " +
                    "a baseAward that does not match its stopIndex.";
                return false;
            }

            calculatedTotal += activeWheel.finalAward;
        }

        if (!AmountsMatch(calculatedTotal, ultraBonus.totalAward))
        {
            error =
                "The server Ultra totalAward does not equal the supplied " +
                "active-wheel awards.";
            return false;
        }

        return true;
    }

    private bool TryValidateScatterBonusResult(
        ServerScatterBonus scatterBonus,
        List<List<int>> resultMatrix,
        out string error)
    {
        error = null;
        if (stPatricksGoldConfig == null || resultMatrix == null)
        {
            error =
                "The server Scatter result cannot be validated without the " +
                "server game configuration and reel matrix.";
            return false;
        }

        if (scatterBonus == null)
        {
            error =
                "The server triggered Scatter without a scatterBonus result.";
            return false;
        }

        if (!scatterBonus.hasIsTriggered || !scatterBonus.isTriggered)
        {
            error =
                "The server Scatter result must explicitly contain " +
                "isTriggered=true.";
            return false;
        }

        if (!scatterBonus.hasTotalAward ||
            !IsFiniteNonNegative(scatterBonus.totalAward))
        {
            error =
                "The server Scatter result is missing a valid totalAward.";
            return false;
        }

        if (scatterBonus.wheelSpins == null ||
            scatterBonus.wheelSpins.Count == 0)
        {
            error =
                "The server Scatter result contains no wheelSpins.";
            return false;
        }

        int scatterSymbolCount = 0;
        foreach (List<int> column in resultMatrix)
        {
            if (column == null)
            {
                continue;
            }

            foreach (int symbolId in column)
            {
                if (symbolId ==
                    stPatricksGoldConfig.scatterWheelSymbolId)
                {
                    scatterSymbolCount++;
                }
            }
        }

        ScatterWheelConfig scatterWheelConfig =
            stPatricksGoldConfig.scatterWheel;
        if (scatterWheelConfig == null ||
            scatterWheelConfig.wheelCount <= 0 ||
            scatterWheelConfig.wheels == null ||
            scatterWheelConfig.wheels.Count !=
                scatterWheelConfig.wheelCount)
        {
            error =
                "The init Scatter configuration must contain wheelCount " +
                "and the same number of wheel award tables.";
            return false;
        }

        int configuredWheelCount = scatterWheelConfig.wheelCount;

        int expectedWheelSpinCount =
            Math.Min(scatterSymbolCount, configuredWheelCount);
        if (scatterBonus.wheelSpins.Count != expectedWheelSpinCount)
        {
            error =
                $"The server Scatter supplied " +
                $"{scatterBonus.wheelSpins.Count} wheelSpins, but " +
                $"{expectedWheelSpinCount} were expected from " +
                $"{scatterSymbolCount} Scatter Wheel symbols with a " +
                $"{configuredWheelCount}-wheel maximum.";
            return false;
        }

        var occupiedPositions = new HashSet<int>();
        double calculatedTotal = 0d;
        for (int spinIndex = 0;
             spinIndex < scatterBonus.wheelSpins.Count;
             spinIndex++)
        {
            ServerScatterWheelSpin wheelSpin =
                scatterBonus.wheelSpins[spinIndex];
            if (wheelSpin == null ||
                !wheelSpin.hasWheelIndex ||
                !wheelSpin.hasStopIndex ||
                !wheelSpin.hasAwardValue)
            {
                error =
                    $"Server Scatter wheelSpins[{spinIndex}] must explicitly " +
                    "contain wheelIndex, stopIndex, and awardValue.";
                return false;
            }

            if (!wheelSpin.hasPosition)
            {
                if (scatterBonus.triggerPositions == null ||
                    spinIndex >= scatterBonus.triggerPositions.Count)
                {
                    error =
                        $"Server Scatter wheelSpins[{spinIndex}] has no server " +
                        "row/column position.";
                    return false;
                }

                ServerGridPosition serverPosition =
                    scatterBonus.triggerPositions[spinIndex];
                if (serverPosition == null ||
                    !serverPosition.hasRow ||
                    !serverPosition.hasCol)
                {
                    error =
                        $"Server Scatter triggerPositions[{spinIndex}] is " +
                        "incomplete.";
                    return false;
                }

                wheelSpin.row = serverPosition.row;
                wheelSpin.col = serverPosition.col;
                wheelSpin.hasPosition = true;
            }

            if (wheelSpin.col < 0 ||
                wheelSpin.col >= resultMatrix.Count ||
                resultMatrix[wheelSpin.col] == null ||
                wheelSpin.row < 0 ||
                wheelSpin.row >= resultMatrix[wheelSpin.col].Count ||
                resultMatrix[wheelSpin.col][wheelSpin.row] !=
                    stPatricksGoldConfig.scatterWheelSymbolId)
            {
                error =
                    $"Server Scatter wheelSpins[{spinIndex}] points to a " +
                    "position that is not a Scatter Wheel symbol in the " +
                    "server reel matrix.";
                return false;
            }

            int flatPosition =
                wheelSpin.row * resultMatrix.Count + wheelSpin.col;
            if (!occupiedPositions.Add(flatPosition))
            {
                error =
                    $"Server Scatter wheelSpins[{spinIndex}] duplicates a " +
                    "wheel position.";
                return false;
            }

            if (wheelSpin.wheelIndex < 0 ||
                wheelSpin.stopIndex < 0 ||
                wheelSpin.stopIndex >=
                    ScatterWheelPresentationManager.ServerValueCount ||
                !IsFiniteNonNegative(wheelSpin.awardValue))
            {
                error =
                    $"Server Scatter wheelSpins[{spinIndex}] contains an " +
                    "invalid wheelIndex, stopIndex, or awardValue.";
                return false;
            }

            if (!TryGetScatterWheelInitValues(
                    wheelSpin,
                    out List<double> initValues) ||
                initValues.Count !=
                    ScatterWheelPresentationManager.ServerValueCount)
            {
                error =
                    $"Server Scatter wheel {wheelSpin.wheelIndex} has no " +
                    $"complete " +
                    $"{ScatterWheelPresentationManager.ServerValueCount}-value " +
                    "award table in game:init.";
                return false;
            }

            wheelSpin.values =
                new List<string>(initValues.Count);
            foreach (double initValue in initValues)
            {
                wheelSpin.values.Add(
                    ServerAmountFormatter.Format(initValue));
            }
            wheelSpin.awards = null;

            for (int valueIndex = 0;
                 valueIndex < initValues.Count;
                 valueIndex++)
            {
                if (!IsFiniteNonNegative(initValues[valueIndex]))
                {
                    error =
                        $"Server Scatter wheel {wheelSpin.wheelIndex} contains " +
                        $"an invalid value at index {valueIndex}.";
                    return false;
                }
            }

            calculatedTotal += wheelSpin.awardValue;
        }

        if (!AmountsMatch(calculatedTotal, scatterBonus.totalAward))
        {
            error =
                "The server Scatter totalAward does not equal the supplied " +
                "wheel awards.";
            return false;
        }

        return true;
    }

    private IReadOnlyList<int> GetUltraWheelServerValues(
        ServerUltraActiveWheel activeWheel)
    {
        if (activeWheel == null)
        {
            return null;
        }

        if (activeWheel.awards != null)
        {
            return activeWheel.awards.Count ==
                   UltraWheelController.ServerValueCount
                ? activeWheel.awards
                : null;
        }

        UltraWheelConfig config = stPatricksGoldConfig?.ultraWheel;
        switch (activeWheel.wheelIndex)
        {
            case 1:
                return HasCompleteUltraWheelTable(config?.wheel1Awards)
                    ? config.wheel1Awards
                    : null;
            case 2:
                return HasCompleteUltraWheelTable(config?.wheel2Awards)
                    ? config.wheel2Awards
                    : null;
            case 3:
                return HasCompleteUltraWheelTable(config?.wheel3Awards)
                    ? config.wheel3Awards
                    : null;
            default:
                return null;
        }
    }

    private static bool HasCompleteUltraWheelTable(
        IReadOnlyList<int> values)
    {
        return values != null &&
               values.Count == UltraWheelController.ServerValueCount;
    }

    private bool TryGetScatterWheelInitValues(
        ServerScatterWheelSpin wheelSpin,
        out List<double> values)
    {
        values = null;
        if (wheelSpin == null)
        {
            return false;
        }

        List<ScatterWheelAwardTable> initWheelTables =
            stPatricksGoldConfig?.scatterWheel?.wheels;
        if (initWheelTables == null)
        {
            return false;
        }

        int initWheelId = wheelSpin.wheelIndex + 1;
        ScatterWheelAwardTable matchedTable = null;
        foreach (ScatterWheelAwardTable initWheelTable in initWheelTables)
        {
            if (initWheelTable == null ||
                initWheelTable.wheelId != initWheelId)
            {
                continue;
            }

            if (matchedTable != null)
            {
                return false;
            }

            matchedTable = initWheelTable;
        }

        if (matchedTable?.awards == null ||
            matchedTable.awards.Count !=
                ScatterWheelPresentationManager.ServerValueCount)
        {
            return false;
        }

        values = new List<double>(matchedTable.awards.Count);
        foreach (int initAward in matchedTable.awards)
        {
            values.Add(initAward);
        }

        return true;
    }

    private static bool IsFiniteNonNegative(double value)
    {
        return !double.IsNaN(value) &&
               !double.IsInfinity(value) &&
               value >= 0d;
    }

    private static bool AmountsMatch(double left, double right)
    {
        return Math.Abs(left - right) <= 0.005d;
    }

    private static bool TryResolveUltraWheelState(
        string wheelState,
        out bool isActive,
        out bool hasMultiplier)
    {
        isActive = false;
        hasMultiplier = false;

        string normalizedState = wheelState?.Trim();
        if (string.Equals(
                normalizedState,
                "inactive",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(
                normalizedState,
                "active",
                StringComparison.OrdinalIgnoreCase))
        {
            isActive = true;
            return true;
        }

        const string activeStatePrefix = "active_";
        if (normalizedState != null &&
            normalizedState.StartsWith(
                activeStatePrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            isActive = true;
            hasMultiplier =
                normalizedState.IndexOf(
                    "multiplier",
                    activeStatePrefix.Length,
                    StringComparison.OrdinalIgnoreCase) >= 0;
            return true;
        }

        return false;
    }

    private static bool IsUltraBonusTriggered(ServerUltraBonus ultraBonus)
    {
        return ultraBonus != null && ultraBonus.isTriggered;
    }

    private static bool IsScatterBonusTriggered(
        ServerScatterBonus scatterBonus)
    {
        return scatterBonus != null &&
               scatterBonus.isTriggered;
    }

    private static bool TryBuildUltraSlotResult(
        ServerUltraBonus ultraBonus,
        out List<int> result)
    {
        result = null;
        if (ultraBonus?.reelResults == null ||
            ultraBonus.reelResults.Count != UltraSlotView.ReelCount)
        {
            return false;
        }

        List<int> converted = UltraSlotView.CreateEmptyResult();
        var assignedReels = new HashSet<int>();

        foreach (ServerUltraReelResult reelResult in ultraBonus.reelResults)
        {
            if (reelResult == null ||
                !reelResult.hasReelIndex ||
                !reelResult.hasBonusWheelStopIndex ||
                !reelResult.hasWheelState)
            {
                return false;
            }

            int resultIndex = reelResult.reelIndex - 1;
            if (resultIndex < 0 ||
                resultIndex >= UltraSlotView.ReelCount ||
                !assignedReels.Add(resultIndex))
            {
                return false;
            }

            if (!TryResolveUltraWheelState(
                    reelResult.wheelState,
                    out bool isActive,
                    out _))
            {
                return false;
            }

            if (!isActive)
            {
                converted[UltraSlotView.GetResultIndex(
                    UltraSlotView.CenterRowIndex,
                    resultIndex)] = UltraSlotView.EmptySymbolId;
                continue;
            }

            int wheelSymbol = reelResult.assignedWheelIndex;
            if (!reelResult.hasAssignedWheelIndex ||
                wheelSymbol < UltraSlotView.GreenWheelSymbolId ||
                wheelSymbol > UltraSlotView.RedWheelSymbolId)
            {
                return false;
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

    internal void UpdateBalanceDisplay(double newBalance)
    {
        if (playerData == null)
        {
            playerData = new PlayerData();
        }

        playerData.balance = newBalance;
        GamePresentationChanged?.Invoke();

        if (CanAffordBet())
        {
            popupManager?.CloseInsufficientBalancePopup();
        }
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
            popupManager?.ShowInsufficientFundsError();
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
        return !isAutoPlaying &&
               isInitialized &&
               currentState == GameState.Idle &&
               !isUltraSlotUnlocked &&
               socketManager != null &&
               socketManager.isConnected &&
               slotView != null &&
               !slotView.IsSpinning();
    }

    private void QueueNextAutoPlaySpin()
    {
        QueueNextAutoPlaySpin(autoPlayResultHoldDuration);
    }

    private void QueueNextAutoPlaySpin(float resultHoldDuration)
    {
        if (!isAutoPlaying || autoPlayCoroutine != null) return;

        autoPlayCoroutine = StartCoroutine(
            StartNextAutoPlaySpin(resultHoldDuration));
    }

    private IEnumerator StartNextAutoPlaySpin(
        float resultHoldDuration)
    {
        // Keep the completed server result visible before starting another round.
        if (resultHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                resultHoldDuration);
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
        pendingScatterBonus = null;
        manualStopRequested = false;
        slotView?.CancelSpin();
        CancelOptimisticBalanceTransaction();
        ResetUltraSlotState();
        currentState = GameState.Idle;
        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();
        popupManager?.ShowDisconnectionPopup();
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
