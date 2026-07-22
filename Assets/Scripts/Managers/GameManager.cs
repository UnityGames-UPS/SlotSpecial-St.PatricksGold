using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    internal const int InfiniteAutoPlayRounds = -1;

    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] private SlotView slotView;

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
    internal IReadOnlyList<int> latestWinningPaylineIndices => latestWinningPaylineIndicesInternal;

    internal event System.Action<bool> SpinActivityChanged;
    internal event System.Action<SpinSpeed> SpinSpeedChanged;
    internal event System.Action GamePresentationChanged;
    internal event System.Action AutoPlayChanged;

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
    private SpinSpeed activeSpinSpeed;
    private List<List<int>> pendingResultMatrix;
    private SpinResult pendingSpinResult;
    private bool manualStopRequested;
    private double displayedWinAmount;
    private readonly List<int> latestWinningPaylineIndicesInternal = new List<int>();

    #region Initialization

    private void Start()
    {
        currentState = GameState.Initializing;
        currentSpinSpeed = SpinSpeed.Normal;
        isInitialized = false;
        initializationFailed = false;
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
        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        Debug.Log("[GameManager] Game initialized.");
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

        bool shouldContinueAutoPlay = false;
        if (isAutoPlaying)
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

        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        // The server win lines have already been converted to flat positions
        // for the visible 5x3 grid. Start their symbol animation only after all
        // reels have settled so the pulse is applied to the displayed result.
        bool hasWinLines = slotView != null &&
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



    #endregion
}
