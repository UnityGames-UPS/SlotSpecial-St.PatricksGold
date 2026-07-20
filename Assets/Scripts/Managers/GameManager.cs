using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] private SlotView slotView;

    [Header("Spin Settings")]
    [SerializeField] private float normalSpinDuration = 2.0f;
    [UnityEngine.Serialization.FormerlySerializedAs("quickSpinDuration")]
    [SerializeField] private float fastSpinDuration = 0.75f;

    internal StPatricksGoldGameConfig stPatricksGoldConfig;
    internal PlayerData playerData;
    internal ServerSpinResponse latestServerSpinResponse { get; private set; }
    internal string latestRawSpinResponse { get; private set; }
    internal IReadOnlyList<int> latestWinningPaylineIndices => latestWinningPaylineIndicesInternal;

    internal event System.Action<bool> SpinActivityChanged;
    internal event System.Action<SpinSpeed> SpinSpeedChanged;
    internal event System.Action GamePresentationChanged;

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
    private List<List<int>> pendingResultMatrix;
    private bool manualStopRequested;
    private double displayedWinAmount;
    private double pendingWinAmount;
    private double? pendingBalance;
    private readonly List<int> latestWinningPaylineIndicesInternal = new List<int>();
    private List<int> pendingWinningPaylineIndices = new List<int>();

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
        pendingWinAmount = 0;
        pendingBalance = null;
        latestWinningPaylineIndicesInternal.Clear();
        pendingWinningPaylineIndices.Clear();
        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();

        Debug.Log("[GameManager] Game initialized.");
    }

    #endregion

    #region Bet Management

    internal bool IncreaseBet()
    {
        if (!CanIncreaseBet()) return false;

        SetBetIndex(currentBetIndex + 1);
        return true;
    }

    internal bool DecreaseBet()
    {
        if (!CanDecreaseBet()) return false;

        SetBetIndex(currentBetIndex - 1);
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
        return CanChangeBet() && currentBetIndex < stPatricksGoldConfig.availableBets.Count - 1;
    }

    internal bool CanDecreaseBet()
    {
        return CanChangeBet() && currentBetIndex > 0;
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
        manualStopRequested = false;
        displayedWinAmount = 0;
        pendingWinAmount = 0;
        pendingBalance = null;
        latestWinningPaylineIndicesInternal.Clear();
        pendingWinningPaylineIndices.Clear();
        currentState = GameState.Spinning;

        slotView.StartSpin(currentSpinSpeed);
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
        float normalStopReadyTime = Time.time + GetSpinDuration();

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
        bool showResultImmediately = currentSpinSpeed == SpinSpeed.SkipSpin;
        bool useFastStop = manualStopRequested || currentSpinSpeed == SpinSpeed.FastSpin;
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

        displayedWinAmount = pendingWinAmount;
        if (pendingBalance.HasValue && playerData != null)
        {
            playerData.balance = pendingBalance.Value;
        }
        pendingWinAmount = 0;
        pendingBalance = null;
        latestWinningPaylineIndicesInternal.Clear();
        latestWinningPaylineIndicesInternal.AddRange(pendingWinningPaylineIndices);
        pendingWinningPaylineIndices.Clear();

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        currentState = GameState.Idle;
        SpinActivityChanged?.Invoke(false);
        GamePresentationChanged?.Invoke();
        Debug.Log("[GameManager] Reels stopped on the server matrix. Round returned to Idle.");
    }

    private float GetSpinDuration()
    {
        switch (currentSpinSpeed)
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

        pendingWinAmount = GetServerWinAmount(serverResponse);
        pendingBalance = serverResponse.player != null
            ? serverResponse.player.balance
            : null;
        pendingWinningPaylineIndices = GetConfiguredWinningPaylineIndices(serverResponse);
        pendingResultMatrix = resultMatrix;
    }

    private double GetServerWinAmount(ServerSpinResponse serverResponse)
    {
        if (serverResponse?.payload == null)
        {
            return 0;
        }

        return serverResponse.payload.winAmount != 0
            ? serverResponse.payload.winAmount
            : serverResponse.payload.totalWin;
    }

    private List<int> GetConfiguredWinningPaylineIndices(ServerSpinResponse serverResponse)
    {
        var result = new List<int>();
        List<ServerWinLine> serverWins = null;

        if (serverResponse?.payload?.lineWins != null && serverResponse.payload.lineWins.Count > 0)
        {
            serverWins = serverResponse.payload.lineWins;
        }
        else if (serverResponse?.payload?.winningLines != null)
        {
            serverWins = serverResponse.payload.winningLines;
        }

        if (serverWins == null || stPatricksGoldConfig?.paylines == null)
        {
            return result;
        }

        var uniqueIndices = new HashSet<int>();
        foreach (ServerWinLine serverWin in serverWins)
        {
            if (serverWin == null) continue;

            int lineIndex = serverWin.lineIndex;
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
        manualStopRequested = false;
        pendingWinAmount = 0;
        pendingBalance = null;
        pendingWinningPaylineIndices.Clear();
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
        if (IsSpinRoundActive() || isAutoPlaying)
        {
            Debug.LogWarning("[GameManager] Spin mode cannot be changed during an active round or autoplay.");
            return false;
        }

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

    internal void StartAutoPlay(int rounds)
    {
        if (currentState != GameState.Idle) return;

        // Check balance BEFORE locking any UI — if insufficient, show popup and bail.
        if (!CanAffordBet())
        {
            Debug.LogWarning("[GameManager] Insufficient funds.");
            return;
        }

        isAutoPlaying = true;
        autoPlayTotalRounds = rounds;
        autoPlayRemainingRounds = rounds;

        RequestSpin();
    }

    internal void StopAutoPlay()
    {
        isAutoPlaying = false;
        autoPlayRemainingRounds = 0;

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
        manualStopRequested = false;
        pendingWinAmount = 0;
        pendingBalance = null;
        pendingWinningPaylineIndices.Clear();
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
