using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;

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

        gameSocket.On<string>("game:init", OnInitReceived);
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

    private void OnInitReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] Init received: {jsonData}");

        try
        {
            var rootData = JsonUtility.FromJson<Root>(jsonData);
            if (!IsRootInitData(rootData))
            {
                throw new Exception("Init data does not match the Root game config structure.");
            }

            var gameConfig = GameDataConverter.ConvertToGameConfig(rootData);

            double existingBalance = gameManager != null && gameManager.playerData != null
                ? gameManager.playerData.balance
                : 0;
            var playerData = GameDataConverter.ConvertToPlayerData(rootData, 0, existingBalance);

            if (existingBalance <= 0)
            {
                Debug.LogWarning("[SocketIO] Root init data does not include player balance. Using default balance 0 until a server balance is received.");
            }

            var initialMatrix = GenerateRandomMatrix(gameConfig.reelCount, gameConfig.rowCount, gameConfig.symbolCount);

            isInitialized = true;

            gameManager.OnInitDataReceived(gameConfig, playerData, initialMatrix);

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
            Debug.LogError($"[SocketIO] Init parse failed: {e.Message}");
            gameManager.initializationFailed = true;
        }
    }

    private bool IsRootInitData(Root rootData)
    {
        return rootData != null &&
               rootData.bets != null &&
               rootData.lines != null &&
               rootData.symbols != null;
    }

    private void OnResultReceived(string jsonData)
    {
        if (!jsonData.Contains("\"id\":\"ResultData\""))
        {
            return;
        }

        Debug.Log($"[SocketIO] Result received: {jsonData}");

        try
        {
            var serverResponse = JsonUtility.FromJson<ServerSpinResponse>(jsonData);

            if (!serverResponse.success)
            {
                Debug.LogError("[SocketIO] Spin failed");
                return;
            }

            double currentBalance = gameManager.playerData.balance;
            double betAmount = gameManager.currentBetAmount;
            GameConfig gameConfig = gameManager.gameConfig;

            SpinResult result = GameDataConverter.ConvertServerResponseToSpinResult(
                serverResponse,
                currentBalance,
                betAmount,
                gameConfig
            );

            result.playerData.currentBetIndex = gameManager.currentBetIndex;

            gameManager.OnSpinResultReceived(result);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Result parse failed: {e.Message}");
        }
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

    internal void SendSpinRequest(int betIndex)
    {
        Debug.Log($"[SocketIO] Spin request: betIndex={betIndex}");

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
    private List<List<int>> GenerateRandomMatrix(int reelCount, int rowCount, int symbolCount = 9)
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
