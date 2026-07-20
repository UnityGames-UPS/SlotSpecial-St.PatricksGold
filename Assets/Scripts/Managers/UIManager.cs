using UnityEngine;
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

        if (gameManager == null)
        {
            Debug.LogError("[UIManager] GameManager is not assigned.");
        }
    }

    private void OnEnable()
    {
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

        if (gameManager != null)
        {
            gameManager.SpinActivityChanged += OnSpinActivityChanged;
            gameManager.SpinSpeedChanged += OnSpinSpeedChanged;
            gameManager.GamePresentationChanged += OnGamePresentationChanged;
        }

        RefreshSpinControls();
        RefreshSpinModeButtons();
        RefreshGameTexts();
        RefreshBetControls();
    }

    private void OnDisable()
    {
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

        if (gameManager != null)
        {
            gameManager.SpinActivityChanged -= OnSpinActivityChanged;
            gameManager.SpinSpeedChanged -= OnSpinSpeedChanged;
            gameManager.GamePresentationChanged -= OnGamePresentationChanged;
        }
    }

    private void OnSpinButtonClicked()
    {
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
        ApplySpinControlState(isRoundActive);
        RefreshSpinModeButtons();
        RefreshBetControls();
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

    private void RefreshSpinControls()
    {
        bool isRoundActive = gameManager != null && gameManager.IsSpinRoundActive();
        ApplySpinControlState(isRoundActive);
    }

    private void ApplySpinControlState(bool isRoundActive)
    {
        if (spinButton != null)
        {
            spinButton.gameObject.SetActive(!isRoundActive);
            spinButton.interactable = !isRoundActive &&
                                      gameManager != null &&
                                      gameManager.CanRequestSpin();
        }

        if (stopButton != null)
        {
            stopButton.gameObject.SetActive(isRoundActive);
            stopButton.interactable = isRoundActive &&
                                      gameManager != null &&
                                      gameManager.CanRequestStop();
        }
    }

    private void RefreshSpinModeButtons()
    {
        bool canChangeMode = gameManager != null && !gameManager.IsSpinRoundActive();
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
}
