using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Best.HTTP.JSON;

public class SocketIOManager : MonoBehaviour
{
    [SerializeField] private string testToken = "test-token";
    protected string testSocketURL = "https://devrealtime.dingdinghouse.com/";
    protected string nameSpace = "playground";


    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [SerializeField] internal JSFunctCalls JSManager;
    [SerializeField] private GameObject RaycastBlocker;

    private SocketManager socketManager;
    private Socket gameSocket;

    private string authToken;
    private string socketURL;

    internal bool isConnected;
    internal bool isInitialized;
    internal bool isExiting;   // True when CloseSocket is called intentionally (exit button)
    internal StPatricksGoldConfigResponse latestGameConfigResponse { get; private set; }
    internal string latestRawGameConfigResponse { get; private set; }
    internal ServerSpinResponse latestSpinResponse { get; private set; }
    internal string latestRawSpinResponse { get; private set; }

    private Coroutine pingCoroutine;
    private float lastPongTime;
    private bool waitingForPong;
    private int missedPongs;
    private const int MAX_MISSED_PONGS = 5;
    private const float PING_INTERVAL = 2f;
    private const float PONG_TIMEOUT = 5f;

    #region Initialization

    private void Awake()
    {
        isInitialized = false;
        isConnected = false;
        isExiting = false;
    }

    private void Start()
    {
        RequestAuthToken();
    }
    internal void CloseGame()
    {
        Debug.Log("Unity: Closing Game");
        StartCoroutine(CloseGameRoutine());
    }

    private IEnumerator CloseGameRoutine()
    {
        isExiting = true;

        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        // Show the loading popup immediately so it's visible during the delay
        // 1. Stop pinging and initiate socket closure immediately
        StopPingRoutine();

        if (socketManager != null)
        {
            try
            {
                socketManager.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketIO] Error closing socketManager: {ex.Message}");
            }
            socketManager = null;
        }

        isConnected = false;

        // 2. Wait 1 seconds for the websocket close handshake to complete and the connection state to settle
        yield return new WaitForSeconds(1f);

        // 3. Send OnExit to platform to unmount the iframe
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("OnExit");
        }
#endif
    }
    private void RequestAuthToken()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("authToken");
        }
#else
        authToken = testToken;
        socketURL = testSocketURL;
        InitializeSocket();
#endif
    }

    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log($"[SocketIO] Auth received");

        try
        {
            var authData = JsonUtility.FromJson<AuthTokenData>(jsonData);
            authToken = authData.cookie;
            socketURL = authData.socketURL;

            if (!string.IsNullOrEmpty(authData.nameSpace))
            {
                nameSpace = authData.nameSpace;
            }

            InitializeSocket();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Auth parse failed: {e.Message}");
        }
    }

    private void InitializeSocket()
    {
        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        SocketOptions options = new SocketOptions
        {
            AutoConnect = false,
            Reconnection = false,
            Timeout = TimeSpan.FromSeconds(3),
            ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
        };

        options.Auth = (SocketManager manager, Socket socket) => new { token = authToken };

#if UNITY_EDITOR
        socketManager = new SocketManager(new Uri(testSocketURL), options);
#else
        socketManager = new SocketManager(new Uri(socketURL), options);
#endif

        gameSocket = string.IsNullOrEmpty(nameSpace)
            ? socketManager.Socket
            : socketManager.GetSocket("/" + nameSpace);

        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        gameSocket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        gameSocket.On<string>("game:init", OnStPatricksGoldConfigReceived);
        gameSocket.On<string>("result", OnResultReceived);
        gameSocket.On<string>("pong", OnPongReceived);
        gameSocket.On<string>("AnotherDevice", OnAnotherDevice);

        socketManager.Open();
    }

    #endregion

    #region Socket Events

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("[SocketIO] Connected");

        isConnected = true;
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;

        StartPingRoutine();
    }

    private void OnSocketDisconnected()
    {
        Debug.Log("[SocketIO] Disconnected");

        isConnected = false;
        StopPingRoutine();

        if (isExiting)
        {
            // Intentional exit — show loading popup (animation) instead of disconnect popup
            // Do NOT call gameManager.OnDisconnected for an intentional exit
        }
        else
        {
            // Unexpected disconnection — show the regular disconnection popup
            if (gameManager != null)
            {
                gameManager.OnDisconnected();
            }
        }
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError($"[SocketIO] Error: {err.message}");

        if (!gameManager.isInitialized)
        {
            gameManager.initializationFailed = true;
        }

        if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
        {
            Debug.LogWarning("Session expired detected");
            OnSocketDisconnected();
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("session_expired");
#endif
        }
        else
        {

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("error");
#endif
        }
    }

    private void OnStPatricksGoldConfigReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] St. Patrick's Gold config received: {jsonData}");
        latestRawGameConfigResponse = jsonData;

        try
        {
            var configResponse = JsonUtility.FromJson<StPatricksGoldConfigResponse>(jsonData);
            latestGameConfigResponse = configResponse;

            if (!TryHydrateStPatricksGoldConfig(
                    jsonData,
                    configResponse,
                    out double? serverBalance,
                    out string hydrationError))
            {
                throw new Exception(hydrationError);
            }

            if (!TryValidateStPatricksGoldConfig(configResponse, out string validationError))
            {
                throw new Exception(validationError);
            }

            var stPatricksGoldConfig = GameDataConverter.ConvertStPatricksGoldConfig(configResponse);

            double existingBalance = gameManager != null && gameManager.playerData != null
                ? gameManager.playerData.balance
                : 0;
            double initialBalance = serverBalance ?? existingBalance;
            var playerData = GameDataConverter.CreateInitialPlayerData(configResponse, 0, initialBalance);

            if (!serverBalance.HasValue && existingBalance <= 0)
            {
                Debug.LogWarning("[SocketIO] SL-SPG config data does not include player balance. Using default balance 0 until a server balance is received.");
            }

            var initialMatrix = GenerateInitialDisplayMatrix(
                stPatricksGoldConfig.reelCount,
                stPatricksGoldConfig.rowCount,
                stPatricksGoldConfig.symbolCount);

            isInitialized = true;

            if (gameManager == null)
            {
                throw new Exception("GameManager is not assigned.");
            }

            gameManager.OnStPatricksGoldConfigReceived(stPatricksGoldConfig, playerData, initialMatrix);

            if (RaycastBlocker) RaycastBlocker.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("OnEnter");
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] SL-SPG config parse failed: {e.Message}");
            if (gameManager != null)
            {
                gameManager.initializationFailed = true;
            }
        }
    }

    private bool TryHydrateStPatricksGoldConfig(
        string jsonData,
        StPatricksGoldConfigResponse configResponse,
        out double? playerBalance,
        out string error)
    {
        playerBalance = null;
        error = null;

        if (configResponse == null)
        {
            error = "JsonUtility returned a null SL-SPG config response.";
            return false;
        }

        bool decodedSuccessfully = false;
        object decoded = Json.Decode(jsonData, ref decodedSuccessfully);
        if (!decodedSuccessfully || !(decoded is IDictionary<string, object> root))
        {
            error = "Could not decode the SL-SPG config JSON.";
            return false;
        }

        IDictionary<string, object> configFields = root;
        bool usesServerInitEnvelope = false;
        if (root.TryGetValue("gameData", out object gameDataValue))
        {
            if (!(gameDataValue is IDictionary<string, object> gameData))
            {
                error = "SL-SPG init field 'gameData' is not a JSON object.";
                return false;
            }

            configFields = gameData;
            usesServerInitEnvelope = true;
        }

        if (!configFields.TryGetValue("lines", out object linesValue) || !(linesValue is IList rawLines))
        {
            error = "SL-SPG config field 'lines' is missing from both the root and 'gameData', or is not an array.";
            return false;
        }

        var paylines = new List<List<int>>(rawLines.Count);
        for (int lineIndex = 0; lineIndex < rawLines.Count; lineIndex++)
        {
            if (!(rawLines[lineIndex] is IList rawLine))
            {
                error = $"SL-SPG payline {lineIndex} is not an array.";
                return false;
            }

            var payline = new List<int>(rawLine.Count);
            for (int reel = 0; reel < rawLine.Count; reel++)
            {
                try
                {
                    payline.Add(Convert.ToInt32(rawLine[reel], CultureInfo.InvariantCulture));
                }
                catch (Exception)
                {
                    error = $"SL-SPG payline {lineIndex}, reel {reel} is not an integer row index.";
                    return false;
                }
            }

            paylines.Add(payline);
        }

        configResponse.lines = paylines;

        if (!TryReadBets(configFields, out List<double> bets, out error))
        {
            return false;
        }

        configResponse.bets = bets;

        if (!TryFindSymbolArray(root, out IList rawSymbols))
        {
            error = "SL-SPG symbols are missing from both 'symbols' and 'uiData.paylines.symbols'.";
            return false;
        }

        if (!TryReadSymbols(rawSymbols, out List<StPatricksGoldSymbol> symbols, out error))
        {
            return false;
        }

        configResponse.symbols = symbols;

        if (TryGetJsonObject(root, configFields, "matrix", out IDictionary<string, object> rawMatrix))
        {
            configResponse.matrix = new SlotMatrixDimensions
            {
                x = ReadJsonInt(rawMatrix, "x"),
                y = ReadJsonInt(rawMatrix, "y")
            };
        }
        // The live initData envelope intentionally omits dimensions. Use the fixed
        // dimensions from the authoritative SL-SPG definition in that format only.
        else if (usesServerInitEnvelope)
        {
            configResponse.matrix = new SlotMatrixDimensions
            {
                x = StPatricksGoldDefinition.ReelCount,
                y = StPatricksGoldDefinition.RowCount
            };
        }

        if (configResponse.features == null)
        {
            configResponse.features = new StPatricksGoldFeatureConfig();
        }

        if (root.TryGetValue("features", out object featuresValue) &&
            featuresValue is IDictionary<string, object> rawFeatures &&
            TryReadJsonDouble(rawFeatures, "betMultiplier", out double betMultiplier))
        {
            configResponse.features.betMultiplier = betMultiplier;
        }

        if (configResponse.features.totalLines <= 0)
        {
            configResponse.features.totalLines = ReadJsonInt(configFields, "totalLines");
        }

        if (root.TryGetValue("player", out object playerValue) &&
            playerValue is IDictionary<string, object> player &&
            player.TryGetValue("balance", out object balanceValue) &&
            balanceValue != null)
        {
            try
            {
                playerBalance = Convert.ToDouble(balanceValue, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                error = "SL-SPG init field 'player.balance' is not a number.";
                return false;
            }
        }

        return true;
    }

    private bool TryGetJsonObject(
        IDictionary<string, object> root,
        IDictionary<string, object> configFields,
        string key,
        out IDictionary<string, object> result)
    {
        result = null;

        if (root.TryGetValue(key, out object rootValue))
        {
            result = rootValue as IDictionary<string, object>;
            return result != null;
        }

        if (!ReferenceEquals(configFields, root) && configFields.TryGetValue(key, out object nestedValue))
        {
            result = nestedValue as IDictionary<string, object>;
            return result != null;
        }

        return false;
    }

    private bool TryReadBets(
        IDictionary<string, object> configFields,
        out List<double> bets,
        out string error)
    {
        bets = null;
        error = null;

        if (!configFields.TryGetValue("bets", out object betsValue) || !(betsValue is IList rawBets))
        {
            error = "SL-SPG config field 'bets' is missing from both the root and 'gameData', or is not an array.";
            return false;
        }

        bets = new List<double>(rawBets.Count);
        for (int betIndex = 0; betIndex < rawBets.Count; betIndex++)
        {
            try
            {
                bets.Add(Convert.ToDouble(rawBets[betIndex], CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
                error = $"SL-SPG bet at index {betIndex} is not a number.";
                return false;
            }
        }

        return true;
    }

    private bool TryFindSymbolArray(IDictionary<string, object> root, out IList symbols)
    {
        symbols = null;

        if (root.TryGetValue("symbols", out object rootSymbols) && rootSymbols is IList flatSymbols)
        {
            symbols = flatSymbols;
            return true;
        }

        if (!root.TryGetValue("uiData", out object uiDataValue) ||
            !(uiDataValue is IDictionary<string, object> uiData) ||
            !uiData.TryGetValue("paylines", out object paylinesValue) ||
            !(paylinesValue is IDictionary<string, object> paylines) ||
            !paylines.TryGetValue("symbols", out object nestedSymbols) ||
            !(nestedSymbols is IList uiSymbols))
        {
            return false;
        }

        symbols = uiSymbols;
        return true;
    }

    private bool TryReadSymbols(
        IList rawSymbols,
        out List<StPatricksGoldSymbol> symbols,
        out string error)
    {
        symbols = new List<StPatricksGoldSymbol>(rawSymbols.Count);
        error = null;

        for (int symbolIndex = 0; symbolIndex < rawSymbols.Count; symbolIndex++)
        {
            if (!(rawSymbols[symbolIndex] is IDictionary<string, object> rawSymbol))
            {
                error = $"SL-SPG symbol at index {symbolIndex} is not a JSON object.";
                return false;
            }

            var symbol = new StPatricksGoldSymbol
            {
                id = ReadJsonInt(rawSymbol, "id"),
                name = ReadJsonString(rawSymbol, "name"),
                group = ReadJsonString(rawSymbol, "group"),
                description = ReadJsonString(rawSymbol, "description"),
                multiplier = ReadJsonIntList(rawSymbol, "multiplier"),
                payout3x = ReadJsonInt(rawSymbol, "payout3x"),
                payout4x = ReadJsonInt(rawSymbol, "payout4x"),
                payout5x = ReadJsonInt(rawSymbol, "payout5x")
            };

            if (rawSymbol.TryGetValue("reelsInstance", out object reelCountsValue) &&
                reelCountsValue is IDictionary<string, object> rawReelCounts)
            {
                symbol.reelsInstance = new ReelSymbolCounts
                {
                    reel0 = ReadJsonInt(rawReelCounts, "0"),
                    reel1 = ReadJsonInt(rawReelCounts, "1"),
                    reel2 = ReadJsonInt(rawReelCounts, "2"),
                    reel3 = ReadJsonInt(rawReelCounts, "3"),
                    reel4 = ReadJsonInt(rawReelCounts, "4")
                };
            }

            symbols.Add(symbol);
        }

        return true;
    }

    private string ReadJsonString(IDictionary<string, object> values, string key)
    {
        return values.TryGetValue(key, out object value) && value != null
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;
    }

    private List<int> ReadJsonIntList(IDictionary<string, object> values, string key)
    {
        var result = new List<int>();
        if (!values.TryGetValue(key, out object value) || !(value is IList rawValues))
        {
            return result;
        }

        for (int index = 0; index < rawValues.Count; index++)
        {
            result.Add(Convert.ToInt32(rawValues[index], CultureInfo.InvariantCulture));
        }

        return result;
    }

    private int ReadJsonInt(IDictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out object value) || value == null)
        {
            return 0;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private bool TryValidateStPatricksGoldConfig(StPatricksGoldConfigResponse configResponse, out string error)
    {
        error = null;

        if (configResponse == null)
        {
            error = "SL-SPG config response is null.";
            return false;
        }

        bool isParseSheetConfig = string.Equals(
            configResponse.id,
            StPatricksGoldDefinition.GameId,
            StringComparison.Ordinal);
        bool isServerInitEnvelope = string.Equals(configResponse.id, "initData", StringComparison.Ordinal);
        if (!isParseSheetConfig && !isServerInitEnvelope)
        {
            error = $"Unexpected config response id '{configResponse.id ?? "null"}'; expected '{StPatricksGoldDefinition.GameId}' or 'initData'.";
            return false;
        }

        if (configResponse.matrix == null ||
            configResponse.matrix.x != StPatricksGoldDefinition.ReelCount ||
            configResponse.matrix.y != StPatricksGoldDefinition.RowCount)
        {
            error = $"SL-SPG requires a {StPatricksGoldDefinition.ReelCount}x{StPatricksGoldDefinition.RowCount} matrix.";
            return false;
        }

        if (configResponse.bets == null || configResponse.bets.Count == 0)
        {
            error = "SL-SPG config contains no bet values.";
            return false;
        }

        if (configResponse.lines == null || configResponse.lines.Count == 0)
        {
            error = "SL-SPG config contains no server payline definitions.";
            return false;
        }

        for (int lineIndex = 0; lineIndex < configResponse.lines.Count; lineIndex++)
        {
            List<int> payline = configResponse.lines[lineIndex];
            if (payline == null || payline.Count != StPatricksGoldDefinition.ReelCount)
            {
                error = $"Payline {lineIndex} must contain one row index for each of the {StPatricksGoldDefinition.ReelCount} reels.";
                return false;
            }

            for (int reel = 0; reel < payline.Count; reel++)
            {
                if (payline[reel] < 0 || payline[reel] >= StPatricksGoldDefinition.RowCount)
                {
                    error = $"Payline {lineIndex}, reel {reel} contains invalid row index {payline[reel]}.";
                    return false;
                }
            }
        }

        if (configResponse.symbols == null || configResponse.symbols.Count != StPatricksGoldDefinition.SymbolCount)
        {
            error = $"SL-SPG config has {configResponse.symbols?.Count ?? 0} symbols; expected {StPatricksGoldDefinition.SymbolCount}.";
            return false;
        }

        var symbolIds = new HashSet<int>();
        foreach (StPatricksGoldSymbol symbol in configResponse.symbols)
        {
            if (symbol == null || symbol.id < 0 || symbol.id >= StPatricksGoldDefinition.SymbolCount || !symbolIds.Add(symbol.id))
            {
                error = "SL-SPG config contains a null, duplicate, or out-of-range symbol ID.";
                return false;
            }

            string expectedName = StPatricksGoldSymbolIds.GetName(symbol.id);
            if (!string.Equals(symbol.name, expectedName, StringComparison.Ordinal))
            {
                error = $"SL-SPG symbol ID {symbol.id} is named '{symbol.name ?? "null"}'; expected '{expectedName}'.";
                return false;
            }
        }

        if (configResponse.features == null)
        {
            error = "SL-SPG config contains no feature data.";
            return false;
        }

        if (configResponse.features.totalLines <= 0)
        {
            configResponse.features.totalLines = configResponse.lines.Count;
        }
        else if (configResponse.features.totalLines != configResponse.lines.Count)
        {
            error =
                $"SL-SPG features.totalLines is {configResponse.features.totalLines}, " +
                $"but the server supplied {configResponse.lines.Count} payline definitions.";
            return false;
        }

        return true;
    }

    private void OnResultReceived(string jsonData)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            const string emptyResponseError = "Received an empty result response.";
            Debug.LogError($"[SocketIO] {emptyResponseError}");
            latestRawSpinResponse = jsonData;
            latestSpinResponse = null;
            gameManager?.OnSpinResponseInvalid(null, jsonData, emptyResponseError);
            return;
        }

        Debug.Log($"[SocketIO] Result received: {jsonData}");
        ServerSpinResponse serverResponse = null;

        try
        {
            serverResponse = JsonUtility.FromJson<ServerSpinResponse>(jsonData);
            if (serverResponse == null)
            {
                throw new Exception("JsonUtility returned a null ServerSpinResponse.");
            }

            if (!string.Equals(serverResponse.id, "ResultData", StringComparison.Ordinal))
            {
                return;
            }

            latestRawSpinResponse = jsonData;
            latestSpinResponse = serverResponse;

            if (!serverResponse.success)
            {
                const string failedResponseError = "The server reported that the spin failed.";
                Debug.LogError($"[SocketIO] {failedResponseError}");
                gameManager?.OnSpinResponseInvalid(serverResponse, jsonData, failedResponseError);
                return;
            }

            if (!TryHydrateSpinMatrices(jsonData, serverResponse, out string hydrationError))
            {
                Debug.LogError($"[SocketIO] {hydrationError}");
                gameManager?.OnSpinResponseInvalid(serverResponse, jsonData, hydrationError);
                return;
            }

            if (gameManager == null)
            {
                Debug.LogError("[SocketIO] Cannot deliver the spin response because GameManager is not assigned.");
                return;
            }

            if (!GameDataConverter.TryConvertServerMatrix(
                    serverResponse,
                    gameManager.stPatricksGoldConfig,
                    out List<List<int>> resultMatrix,
                    out string matrixError))
            {
                Debug.LogError($"[SocketIO] Invalid spin matrix: {matrixError}");
                gameManager.OnSpinResponseInvalid(serverResponse, jsonData, matrixError);
                return;
            }

            gameManager.OnSpinResponseReceived(serverResponse, jsonData, resultMatrix);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Result parse failed: {e.Message}");
            latestRawSpinResponse = jsonData;
            latestSpinResponse = serverResponse;
            gameManager?.OnSpinResponseInvalid(serverResponse, jsonData, $"Result parse failed: {e.Message}");
        }
    }

    private bool TryHydrateSpinMatrices(string jsonData, ServerSpinResponse serverResponse, out string error)
    {
        error = null;

        bool decodedSuccessfully = false;
        object decoded = Json.Decode(jsonData, ref decodedSuccessfully);
        if (!decodedSuccessfully || !(decoded is IDictionary<string, object> root))
        {
            error = "Could not decode the result JSON while extracting the reel matrix.";
            return false;
        }

        if (root.TryGetValue("matrix", out object matrixValue))
        {
            if (!TryConvertJsonMatrix(matrixValue, "response.matrix", out List<List<string>> matrix, out error))
            {
                return false;
            }

            serverResponse.matrix = matrix;
        }

        if (root.TryGetValue("payload", out object payloadValue) &&
            payloadValue is IDictionary<string, object> payload &&
            payload.TryGetValue("reels", out object reelsValue))
        {
            if (!TryConvertJsonMatrix(reelsValue, "response.payload.reels", out List<List<string>> reels, out error))
            {
                return false;
            }

            if (serverResponse.payload == null)
            {
                serverResponse.payload = new ServerPayload();
            }

            serverResponse.payload.reels = reels;
        }

        HydrateSpinPresentationData(root, serverResponse);

        return true;
    }

    private void HydrateSpinPresentationData(
        IDictionary<string, object> root,
        ServerSpinResponse serverResponse)
    {
        if (root.TryGetValue("player", out object playerValue) &&
            playerValue is IDictionary<string, object> player &&
            TryReadJsonDouble(player, "balance", out double balance))
        {
            if (serverResponse.player == null)
            {
                serverResponse.player = new ServerPlayerBalance();
            }

            serverResponse.player.balance = balance;
        }

        if (!root.TryGetValue("payload", out object payloadValue) ||
            !(payloadValue is IDictionary<string, object> payload))
        {
            return;
        }

        if (serverResponse.payload == null)
        {
            serverResponse.payload = new ServerPayload();
        }

        if (TryReadJsonDouble(payload, "winAmount", out double winAmount))
        {
            serverResponse.payload.winAmount = winAmount;
        }

        if (TryReadJsonDouble(payload, "totalWin", out double totalWin))
        {
            serverResponse.payload.totalWin = totalWin;
        }
    }

    private bool TryReadJsonDouble(
        IDictionary<string, object> values,
        string key,
        out double result)
    {
        result = 0;
        if (!values.TryGetValue(key, out object value) || value == null)
        {
            return false;
        }

        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            Debug.LogWarning($"[SocketIO] Spin response field '{key}' is not a number and will not update the UI.");
            return false;
        }
    }

    private bool TryConvertJsonMatrix(
        object matrixValue,
        string sourceName,
        out List<List<string>> matrix,
        out string error)
    {
        matrix = null;
        error = null;

        if (matrixValue == null)
        {
            return true;
        }

        if (!(matrixValue is IList rows))
        {
            error = $"{sourceName} is not a JSON array.";
            return false;
        }

        matrix = new List<List<string>>(rows.Count);
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            if (!(rows[rowIndex] is IList columns))
            {
                error = $"{sourceName}[{rowIndex}] is not a JSON array.";
                return false;
            }

            var row = new List<string>(columns.Count);
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                object symbolValue = columns[columnIndex];
                row.Add(symbolValue == null
                    ? null
                    : Convert.ToString(symbolValue, CultureInfo.InvariantCulture));
            }

            matrix.Add(row);
        }

        return true;
    }

    private void OnAnotherDevice(string data)
    {
        Debug.Log("[SocketIO] Another device login");

        Debug.LogWarning("[SocketIO] Another device login");
    }

    #endregion

    internal void SetRaycastBlocker(bool active)
    {
        if (RaycastBlocker != null) RaycastBlocker.SetActive(active);
    }

    #region Ping/Pong Health Check

    private void StartPingRoutine()
    {
        if (pingCoroutine != null)
            StopCoroutine(pingCoroutine);

        pingCoroutine = StartCoroutine(PingRoutine());
    }

    private void StopPingRoutine()
    {
        if (pingCoroutine != null)
        {
            StopCoroutine(pingCoroutine);
            pingCoroutine = null;
        }
    }

    private IEnumerator PingRoutine()
    {
        while (isConnected)
        {
            yield return new WaitForSeconds(PING_INTERVAL);

            if (waitingForPong)
            {
                float timeSinceLastPong = Time.time - lastPongTime;

                if (timeSinceLastPong > PONG_TIMEOUT)
                {
                    missedPongs++;

                    if (missedPongs >= MAX_MISSED_PONGS)
                    {
                        Debug.LogWarning("[SocketIO] Max pongs missed - disconnecting");
                        OnSocketDisconnected();
                        yield break;
                    }

                    if (missedPongs >= 1)
                    {
                        Debug.LogWarning($"[SocketIO] Waiting for pong {missedPongs}/{MAX_MISSED_PONGS}");
                    }
                }
            }

            SendPing();
            waitingForPong = true;
        }
    }

    private void SendPing()
    {
        if (gameSocket != null && isConnected)
        {
            gameSocket.Emit("ping");
        }
    }


    private void OnPongReceived(string data)
    {
        waitingForPong = false;
        lastPongTime = Time.time;

        if (missedPongs > 0)
        {
            missedPongs = 0;

            Debug.Log("[SocketIO] Pong received after missed ping.");
        }
    }

    #endregion

    #region Spin Request

    internal bool SendSpinRequest(int betIndex)
    {
        if (!isConnected || gameSocket == null)
        {
            Debug.LogError("[SocketIO] Cannot send spin request because the game socket is not connected.");
            return false;
        }

        Debug.Log($"[SocketIO] Spin request: betIndex={betIndex}");

        try
        {
            var request = new SpinRequest
            {
                type = "SPIN",
                payload = new SpinPayload
                {
                    betIndex = betIndex,
                    isFreeSpin = false
                }
            };

            string json = JsonUtility.ToJson(request);
            gameSocket.Emit("request", json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Failed to send spin request: {e.Message}");
            return false;
        }
    }

    #endregion


    #region Cleanup

    internal void CloseSocket()
    {
        // Mark as intentional exit BEFORE closing so OnSocketDisconnected shows
        // the loading popup (with its animation) instead of the disconnect popup.
        isExiting = true;

        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        StopPingRoutine();

        if (socketManager != null)
        {
            try
            {
                socketManager.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketIO] Error closing socketManager: {ex.Message}");
            }
            socketManager = null;
        }

        isConnected = false;

        // If the socket close does not fire OnSocketDisconnected (e.g. already disconnected),
        // still show the loading popup so the exit transition always looks clean.
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("OnExit");
        }
#endif
    }

    private void OnDisable()
    {
        StopPingRoutine();
    }

    private void OnDestroy()
    {
        CloseSocket();
    }

    #endregion
    private List<List<int>> GenerateInitialDisplayMatrix(
        int reelCount,
        int rowCount,
        int symbolCount = StPatricksGoldDefinition.SymbolCount)
    {
        var matrix = new List<List<int>>();
        int maxSymbolId = Mathf.Max(1, symbolCount);
        for (int col = 0; col < reelCount; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < rowCount; row++)
            {
                column.Add(UnityEngine.Random.Range(0, maxSymbolId));
            }
            matrix.Add(column);
        }
        return matrix;
    }
}


[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}
