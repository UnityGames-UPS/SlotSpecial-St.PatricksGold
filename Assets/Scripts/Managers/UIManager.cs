using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button stopButton;

    [Header("Spin Mode Cycle Buttons")]
    [SerializeField] private Button normalSpinButton;
    [UnityEngine.Serialization.FormerlySerializedAs("quickSpinButton")]
    [SerializeField] private Button fastSpinButton;
    [SerializeField] private Button skipSpinButton;

    [Header("Dynamic Game Text")]
    [SerializeField] private TMP_Text winLinesCountText;
    [SerializeField] private TMP_Text goodLuckText;
    [SerializeField] private TMP_Text wonLabelText;
    [SerializeField] private TMP_Text winAmountText;
    [SerializeField] private TMP_Text balanceAmountText;
    [SerializeField] private TMP_Text betAmountText;

    [Header("Bet Buttons")]
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button decreaseBetButton;

    [Header("Auto Play")]
    [SerializeField] private GameObject autoPlayPanel;
    [SerializeField] private Button autoPlay10Button;
    [SerializeField] private Button autoPlay50Button;
    [SerializeField] private Button autoPlay100Button;
    [SerializeField] private Button autoPlay200Button;
    [SerializeField] private Button autoPlay500Button;
    [SerializeField] private Button autoPlayInfiniteButton;
    [SerializeField] private Button autoPlayStopButton;
    [SerializeField] private TMP_Text autoPlayCountText;
    [SerializeField, Min(0.1f)] private float autoPlayHoldDuration = 0.75f;

    private bool isAutoPlayPanelOpen;
    private bool isSpinPointerHeld;
    private bool suppressNextSpinClick;
    private Coroutine spinHoldCoroutine;
    private EventTrigger spinEventTrigger;
    private EventTrigger.Entry spinPointerDownEntry;
    private EventTrigger.Entry spinPointerUpEntry;
    private EventTrigger.Entry spinPointerExitEntry;

    private void Awake()
    {
        if (spinButton == null)
        {
            spinButton = GetComponent<Button>();
        }

        if (spinButton == null)
        {
            Debug.LogError("[UIManager] Spin Button is not assigned and no Button exists on this GameObject.");
        }

        if (stopButton == null)
        {
            Debug.LogError("[UIManager] Stop Button is not assigned.");
        }

        if (normalSpinButton == null)
        {
            Debug.LogError("[UIManager] Normal Spin Button is not assigned.");
        }

        if (fastSpinButton == null)
        {
            Debug.LogError("[UIManager] Fast Spin Button is not assigned.");
        }

        if (skipSpinButton == null)
        {
            Debug.LogError("[UIManager] Skip Spin Button is not assigned.");
        }

        if (winLinesCountText == null)
        {
            Debug.LogError("[UIManager] Win Lines Count Text is not assigned.");
        }

        if (goodLuckText == null)
        {
            Debug.LogError("[UIManager] Good Luck Text is not assigned.");
        }

        if (wonLabelText == null)
        {
            Debug.LogError("[UIManager] Won Label Text is not assigned.");
        }

        if (winAmountText == null)
        {
            Debug.LogError("[UIManager] Win Amount Text is not assigned.");
        }

        if (balanceAmountText == null)
        {
            Debug.LogError("[UIManager] Balance Amount Text is not assigned.");
        }

        if (betAmountText == null)
        {
            Debug.LogError("[UIManager] Bet Amount Text is not assigned.");
        }

        if (increaseBetButton == null)
        {
            Debug.LogError("[UIManager] Increase Bet Button is not assigned.");
        }

        if (decreaseBetButton == null)
        {
            Debug.LogError("[UIManager] Decrease Bet Button is not assigned.");
        }

        if (autoPlayPanel == null)
        {
            Debug.LogError("[UIManager] Auto Play Panel is not assigned.");
        }

        if (autoPlay10Button == null || autoPlay50Button == null ||
            autoPlay100Button == null || autoPlay200Button == null ||
            autoPlay500Button == null || autoPlayInfiniteButton == null)
        {
            Debug.LogError("[UIManager] One or more Auto Play choice buttons are not assigned.");
        }

        if (autoPlayStopButton == null)
        {
            Debug.LogError("[UIManager] Auto Play Stop Button is not assigned.");
        }

        if (autoPlayCountText == null)
        {
            Debug.LogError("[UIManager] Auto Play Count Text is not assigned.");
        }

        if (gameManager == null)
        {
            Debug.LogError("[UIManager] GameManager is not assigned.");
        }
    }

    private void OnEnable()
    {
        RegisterSpinHoldEvents();

        if (spinButton != null)
        {
            spinButton.onClick.AddListener(OnSpinButtonClicked);
        }

        if (stopButton != null)
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        if (normalSpinButton != null)
        {
            normalSpinButton.onClick.AddListener(OnNormalSpinButtonClicked);
        }

        if (fastSpinButton != null)
        {
            fastSpinButton.onClick.AddListener(OnFastSpinButtonClicked);
        }

        if (skipSpinButton != null)
        {
            skipSpinButton.onClick.AddListener(OnSkipSpinButtonClicked);
        }

        if (increaseBetButton != null)
        {
            increaseBetButton.onClick.AddListener(OnIncreaseBetButtonClicked);
        }

        if (decreaseBetButton != null)
        {
            decreaseBetButton.onClick.AddListener(OnDecreaseBetButtonClicked);
        }

        if (autoPlay10Button != null)
            autoPlay10Button.onClick.AddListener(OnAutoPlay10ButtonClicked);
        if (autoPlay50Button != null)
            autoPlay50Button.onClick.AddListener(OnAutoPlay50ButtonClicked);
        if (autoPlay100Button != null)
            autoPlay100Button.onClick.AddListener(OnAutoPlay100ButtonClicked);
        if (autoPlay200Button != null)
            autoPlay200Button.onClick.AddListener(OnAutoPlay200ButtonClicked);
        if (autoPlay500Button != null)
            autoPlay500Button.onClick.AddListener(OnAutoPlay500ButtonClicked);
        if (autoPlayInfiniteButton != null)
            autoPlayInfiniteButton.onClick.AddListener(OnAutoPlayInfiniteButtonClicked);
        if (autoPlayStopButton != null)
            autoPlayStopButton.onClick.AddListener(OnAutoPlayStopButtonClicked);

        if (gameManager != null)
        {
            gameManager.SpinActivityChanged += OnSpinActivityChanged;
            gameManager.SpinSpeedChanged += OnSpinSpeedChanged;
            gameManager.GamePresentationChanged += OnGamePresentationChanged;
            gameManager.AutoPlayChanged += OnAutoPlayChanged;
        }

        RefreshSpinControls();
        RefreshSpinModeButtons();
        RefreshGameTexts();
        RefreshBetControls();
        RefreshAutoPlayControls();
    }

    private void OnDisable()
    {
        CancelSpinHold();
        UnregisterSpinHoldEvents();

        if (spinButton != null)
        {
            spinButton.onClick.RemoveListener(OnSpinButtonClicked);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(OnStopButtonClicked);
        }

        if (normalSpinButton != null)
        {
            normalSpinButton.onClick.RemoveListener(OnNormalSpinButtonClicked);
        }

        if (fastSpinButton != null)
        {
            fastSpinButton.onClick.RemoveListener(OnFastSpinButtonClicked);
        }

        if (skipSpinButton != null)
        {
            skipSpinButton.onClick.RemoveListener(OnSkipSpinButtonClicked);
        }

        if (increaseBetButton != null)
        {
            increaseBetButton.onClick.RemoveListener(OnIncreaseBetButtonClicked);
        }

        if (decreaseBetButton != null)
        {
            decreaseBetButton.onClick.RemoveListener(OnDecreaseBetButtonClicked);
        }

        if (autoPlay10Button != null)
            autoPlay10Button.onClick.RemoveListener(OnAutoPlay10ButtonClicked);
        if (autoPlay50Button != null)
            autoPlay50Button.onClick.RemoveListener(OnAutoPlay50ButtonClicked);
        if (autoPlay100Button != null)
            autoPlay100Button.onClick.RemoveListener(OnAutoPlay100ButtonClicked);
        if (autoPlay200Button != null)
            autoPlay200Button.onClick.RemoveListener(OnAutoPlay200ButtonClicked);
        if (autoPlay500Button != null)
            autoPlay500Button.onClick.RemoveListener(OnAutoPlay500ButtonClicked);
        if (autoPlayInfiniteButton != null)
            autoPlayInfiniteButton.onClick.RemoveListener(OnAutoPlayInfiniteButtonClicked);
        if (autoPlayStopButton != null)
            autoPlayStopButton.onClick.RemoveListener(OnAutoPlayStopButtonClicked);

        if (gameManager != null)
        {
            gameManager.SpinActivityChanged -= OnSpinActivityChanged;
            gameManager.SpinSpeedChanged -= OnSpinSpeedChanged;
            gameManager.GamePresentationChanged -= OnGamePresentationChanged;
            gameManager.AutoPlayChanged -= OnAutoPlayChanged;
        }
    }

    private void OnSpinButtonClicked()
    {
        if (suppressNextSpinClick)
        {
            suppressNextSpinClick = false;
            return;
        }

        CloseAutoPlayPanel();

        if (gameManager == null)
        {
            Debug.LogError("[UIManager] Cannot spin because GameManager is not assigned.");
            RefreshSpinControls();
            return;
        }

        if (!gameManager.RequestSpin())
        {
            RefreshSpinControls();
        }
    }

    private void OnStopButtonClicked()
    {
        if (gameManager == null)
        {
            Debug.LogError("[UIManager] Cannot stop because GameManager is not assigned.");
            RefreshSpinControls();
            return;
        }

        if (!gameManager.RequestStop())
        {
            RefreshSpinControls();
            return;
        }

        // Prevent repeated stop requests while the server result is being applied.
        if (stopButton != null)
        {
            stopButton.interactable = false;
        }
    }

    private void OnSpinActivityChanged(bool isRoundActive)
    {
        if (isRoundActive)
        {
            CloseAutoPlayPanel();
        }

        ApplySpinControlState(isRoundActive);
        RefreshSpinModeButtons();
        RefreshBetControls();
        RefreshAutoPlayControls();
    }

    private void OnIncreaseBetButtonClicked()
    {
        if (gameManager == null || !gameManager.IncreaseBet())
        {
            RefreshBetControls();
        }
    }

    private void OnDecreaseBetButtonClicked()
    {
        if (gameManager == null || !gameManager.DecreaseBet())
        {
            RefreshBetControls();
        }
    }

    private void OnGamePresentationChanged()
    {
        RefreshGameTexts();
        RefreshBetControls();
        RefreshSpinControls();
        RefreshAutoPlayControls();
    }

    private void OnAutoPlay10ButtonClicked()
    {
        StartAutoPlay(10);
    }

    private void OnAutoPlay50ButtonClicked()
    {
        StartAutoPlay(50);
    }

    private void OnAutoPlay100ButtonClicked()
    {
        StartAutoPlay(100);
    }

    private void OnAutoPlay200ButtonClicked()
    {
        StartAutoPlay(200);
    }

    private void OnAutoPlay500ButtonClicked()
    {
        StartAutoPlay(500);
    }

    private void OnAutoPlayInfiniteButtonClicked()
    {
        StartAutoPlay(GameManager.InfiniteAutoPlayRounds);
    }

    private void StartAutoPlay(int rounds)
    {
        CloseAutoPlayPanel();

        if (gameManager == null || !gameManager.StartAutoPlay(rounds))
        {
            RefreshAutoPlayControls();
        }
    }

    private void OnAutoPlayStopButtonClicked()
    {
        if (gameManager == null) return;

        gameManager.StopAutoPlay();
        RefreshAutoPlayControls();
        RefreshSpinControls();
    }

    private void OnAutoPlayChanged()
    {
        if (gameManager != null && gameManager.isAutoPlaying)
        {
            isAutoPlayPanelOpen = false;
        }

        RefreshAutoPlayControls();
        RefreshSpinControls();
        RefreshSpinModeButtons();
        RefreshBetControls();
    }

    private void OnNormalSpinButtonClicked()
    {
        SelectSpinMode(SpinSpeed.FastSpin);
    }

    private void OnFastSpinButtonClicked()
    {
        SelectSpinMode(SpinSpeed.SkipSpin);
    }

    private void OnSkipSpinButtonClicked()
    {
        SelectSpinMode(SpinSpeed.Normal);
    }

    private void SelectSpinMode(SpinSpeed mode)
    {
        if (gameManager == null)
        {
            Debug.LogError("[UIManager] Cannot select a spin mode because GameManager is not assigned.");
            return;
        }

        gameManager.SetSpinSpeed(mode);
        RefreshSpinModeButtons();
    }

    private void OnSpinSpeedChanged(SpinSpeed selectedSpeed)
    {
        RefreshSpinModeButtons();
    }

    private void RegisterSpinHoldEvents()
    {
        if (spinButton == null || spinPointerDownEntry != null) return;

        spinEventTrigger = spinButton.GetComponent<EventTrigger>();
        if (spinEventTrigger == null)
        {
            spinEventTrigger = spinButton.gameObject.AddComponent<EventTrigger>();
        }

        if (spinEventTrigger.triggers == null)
        {
            spinEventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        spinPointerDownEntry = CreateSpinTriggerEntry(EventTriggerType.PointerDown, OnSpinPointerDown);
        spinPointerUpEntry = CreateSpinTriggerEntry(EventTriggerType.PointerUp, OnSpinPointerUp);
        spinPointerExitEntry = CreateSpinTriggerEntry(EventTriggerType.PointerExit, OnSpinPointerExit);

        spinEventTrigger.triggers.Add(spinPointerDownEntry);
        spinEventTrigger.triggers.Add(spinPointerUpEntry);
        spinEventTrigger.triggers.Add(spinPointerExitEntry);
    }

    private EventTrigger.Entry CreateSpinTriggerEntry(
        EventTriggerType eventType,
        System.Action<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(eventData => callback(eventData));
        return entry;
    }

    private void UnregisterSpinHoldEvents()
    {
        if (spinEventTrigger != null && spinEventTrigger.triggers != null)
        {
            spinEventTrigger.triggers.Remove(spinPointerDownEntry);
            spinEventTrigger.triggers.Remove(spinPointerUpEntry);
            spinEventTrigger.triggers.Remove(spinPointerExitEntry);
        }

        spinPointerDownEntry = null;
        spinPointerUpEntry = null;
        spinPointerExitEntry = null;
        spinEventTrigger = null;
    }

    private void OnSpinPointerDown(BaseEventData eventData)
    {
        suppressNextSpinClick = false;

        if (gameManager == null || !gameManager.CanStartAutoPlay()) return;

        isSpinPointerHeld = true;
        CancelSpinHoldCoroutine();
        spinHoldCoroutine = StartCoroutine(OpenAutoPlayPanelAfterHold());
    }

    private void OnSpinPointerUp(BaseEventData eventData)
    {
        isSpinPointerHeld = false;
        CancelSpinHoldCoroutine();
    }

    private void OnSpinPointerExit(BaseEventData eventData)
    {
        isSpinPointerHeld = false;
        CancelSpinHoldCoroutine();
    }

    private IEnumerator OpenAutoPlayPanelAfterHold()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, autoPlayHoldDuration));
        spinHoldCoroutine = null;

        if (!isSpinPointerHeld || gameManager == null || !gameManager.CanStartAutoPlay())
        {
            yield break;
        }

        isAutoPlayPanelOpen = true;
        suppressNextSpinClick = true;
        RefreshAutoPlayControls();
    }

    private void CancelSpinHold()
    {
        isSpinPointerHeld = false;
        suppressNextSpinClick = false;
        CancelSpinHoldCoroutine();
    }

    private void CancelSpinHoldCoroutine()
    {
        if (spinHoldCoroutine == null) return;

        StopCoroutine(spinHoldCoroutine);
        spinHoldCoroutine = null;
    }

    private void CloseAutoPlayPanel()
    {
        if (!isAutoPlayPanelOpen) return;

        isAutoPlayPanelOpen = false;
        RefreshAutoPlayControls();
    }

    private void RefreshSpinControls()
    {
        bool isRoundActive = gameManager != null && gameManager.IsSpinRoundActive();
        ApplySpinControlState(isRoundActive);
    }

    private void ApplySpinControlState(bool isRoundActive)
    {
        bool isAutoPlaying = gameManager != null && gameManager.isAutoPlaying;

        if (spinButton != null)
        {
            spinButton.gameObject.SetActive(!isRoundActive && !isAutoPlaying);
            spinButton.interactable = !isRoundActive &&
                                      !isAutoPlaying &&
                                      gameManager != null &&
                                      gameManager.CanRequestSpin();
        }

        if (stopButton != null)
        {
            stopButton.gameObject.SetActive(isRoundActive && !isAutoPlaying);
            stopButton.interactable = isRoundActive &&
                                      !isAutoPlaying &&
                                      gameManager != null &&
                                      gameManager.CanRequestStop();
        }
    }

    private void RefreshSpinModeButtons()
    {
        bool canChangeMode = gameManager != null;
        SpinSpeed selectedMode = gameManager != null
            ? gameManager.GetSpinSpeed()
            : SpinSpeed.Normal;

        SetSpinModeButtonState(normalSpinButton, selectedMode == SpinSpeed.Normal, canChangeMode);
        SetSpinModeButtonState(fastSpinButton, selectedMode == SpinSpeed.FastSpin, canChangeMode);
        SetSpinModeButtonState(skipSpinButton, selectedMode == SpinSpeed.SkipSpin, canChangeMode);
    }

    private void SetSpinModeButtonState(Button button, bool isSelected, bool canChangeMode)
    {
        if (button == null) return;

        button.gameObject.SetActive(isSelected);
        button.interactable = isSelected && canChangeMode;
    }

    private void RefreshGameTexts()
    {
        if (gameManager == null) return;

        if (winLinesCountText != null)
        {
            winLinesCountText.text = gameManager.GetDisplayedPaylineCount().ToString();
        }

        double winAmount = gameManager.GetDisplayedWinAmount();
        bool hasWin = winAmount > 0;

        if (goodLuckText != null)
        {
            goodLuckText.text = "GOOD LUCK !";
            goodLuckText.gameObject.SetActive(!hasWin);
        }

        if (wonLabelText != null)
        {
            wonLabelText.text = "WON";
            wonLabelText.gameObject.SetActive(hasWin);
        }

        if (winAmountText != null)
        {
            if (hasWin)
            {
                winAmountText.text = winAmount.ToString("0.00");
            }

            winAmountText.gameObject.SetActive(hasWin);
        }

        if (balanceAmountText != null)
        {
            balanceAmountText.text = $"BALANCE:  {gameManager.GetDisplayedBalance():0.00}";
        }

        if (betAmountText != null)
        {
            betAmountText.text = gameManager.GetDisplayedTotalBetAmount().ToString("0.00");
        }
    }

    private void RefreshBetControls()
    {
        if (increaseBetButton != null)
        {
            increaseBetButton.interactable = gameManager != null && gameManager.CanIncreaseBet();
        }

        if (decreaseBetButton != null)
        {
            decreaseBetButton.interactable = gameManager != null && gameManager.CanDecreaseBet();
        }
    }

    private void RefreshAutoPlayControls()
    {
        bool isAutoPlaying = gameManager != null && gameManager.isAutoPlaying;
        bool canStartAutoPlay = gameManager != null && gameManager.CanStartAutoPlay();

        if (autoPlayPanel != null)
        {
            autoPlayPanel.SetActive(isAutoPlayPanelOpen && !isAutoPlaying);
        }

        SetButtonInteractable(autoPlay10Button, canStartAutoPlay);
        SetButtonInteractable(autoPlay50Button, canStartAutoPlay);
        SetButtonInteractable(autoPlay100Button, canStartAutoPlay);
        SetButtonInteractable(autoPlay200Button, canStartAutoPlay);
        SetButtonInteractable(autoPlay500Button, canStartAutoPlay);
        SetButtonInteractable(autoPlayInfiniteButton, canStartAutoPlay);

        if (autoPlayStopButton != null)
        {
            autoPlayStopButton.gameObject.SetActive(isAutoPlaying);
            autoPlayStopButton.interactable = isAutoPlaying;
        }

        if (autoPlayCountText != null)
        {
            // If the text is a child of the stop button, control it independently.
            // If both components share one GameObject, the button visibility above controls it.
            if (autoPlayStopButton == null ||
                autoPlayCountText.gameObject != autoPlayStopButton.gameObject)
            {
                autoPlayCountText.gameObject.SetActive(isAutoPlaying);
            }

            if (isAutoPlaying)
            {
                if (gameManager.autoPlayRemainingRounds == GameManager.InfiniteAutoPlayRounds)
                {
                    autoPlayCountText.text = "∞";
                }
                else
                {
                    int spinsRemainingAfterCurrent = Mathf.Max(
                        0,
                        gameManager.autoPlayRemainingRounds - 1);
                    autoPlayCountText.text = spinsRemainingAfterCurrent.ToString();
                }
            }
        }
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}
