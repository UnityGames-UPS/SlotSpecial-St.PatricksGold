using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button stopButton;

    [Header("Ultra Slot")]
    [Tooltip("Shown only after the server or parse sheet unlocks the Ultra slot.")]
    [SerializeField] private Button ultraStartButton;
    [Tooltip(
        "Shown after the Ultra reward count and return transition finish.")]
    [SerializeField] private Button ultraTakeButton;

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

    [Header("Win Line Row Amounts")]
    [Tooltip("The SlotView that presents individual winning lines. It is found automatically when left empty.")]
    [SerializeField] private SlotView slotView;
    [Tooltip("Amount shown when the winning line uses the top cell of the center reel.")]
    [SerializeField] private TMP_Text topWinLineAmountText;
    [Tooltip("Amount shown when the winning line uses the middle cell of the center reel. This row has priority.")]
    [SerializeField] private TMP_Text middleWinLineAmountText;
    [Tooltip("Amount shown when the winning line uses the bottom cell of the center reel.")]
    [SerializeField] private TMP_Text bottomWinLineAmountText;
    [UnityEngine.Serialization.FormerlySerializedAs("totalWinCountDuration")]
    [SerializeField, Min(0.01f)] private float winLineGrowDuration = 1.2f;
    [UnityEngine.Serialization.FormerlySerializedAs("totalWinPeakFontSize")]
    [SerializeField, Min(1f)] private float winLinePeakFontSize = 184f;
    [UnityEngine.Serialization.FormerlySerializedAs("totalWinFinalFontSize")]
    [SerializeField, Min(1f)] private float winLineFinalFontSize = 148f;
    [UnityEngine.Serialization.FormerlySerializedAs("totalWinSettleDuration")]
    [SerializeField, Min(0.01f)] private float winLineSettleDuration = 0.25f;

    [Header("Wild Multiplier Icons")]
    [Tooltip("Icon displayed on a winning Wild when the server returns a 2x multiplier.")]
    [SerializeField] private Sprite wildMultiplier2xIcon;
    [Tooltip("Icon displayed on a winning Wild when the server returns a 3x multiplier.")]
    [SerializeField] private Sprite wildMultiplier3xIcon;
    [Tooltip("Icon displayed on a winning Wild when the server returns a 4x multiplier.")]
    [SerializeField] private Sprite wildMultiplier4xIcon;
    [Tooltip("Icon displayed on a winning Wild when the server returns a 5x multiplier.")]
    [SerializeField] private Sprite wildMultiplier5xIcon;
    [Tooltip("Temporary scale used for the small pop during each shake.")]
    [SerializeField, Min(1f)] private float wildMultiplierPopScale = 1.1f;
    [Tooltip("Maximum left/right Z angle used by the multiplier shake. This does not perform a full rotation.")]
    [SerializeField, Range(0f, 2f)] private float wildMultiplierShakeAngle = 2f;
    [Tooltip("Number of complete left-right shakes during one Wild symbol animation loop.")]
    [SerializeField, Min(1)] private int wildMultiplierShakesPerSymbolLoop = 4;

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
    [SerializeField, Min(0f)] private float autoPlayPanelSlideDistance = 250f;
    [SerializeField, Min(0.01f)] private float autoPlayPanelSlideDuration = 0.45f;

    [Header("Hamburger Menu")]
    [SerializeField] private GameObject hamburgerMenuPanel;
    [SerializeField] private Button hamburgerMenuButton;
    [SerializeField] private Button hamburgerMenuDownButton;
    [SerializeField, Min(0.01f)] private float hamburgerMenuFadeDuration = 0.3f;

    [Header("Info Page")]
    [SerializeField] private GameObject infoPagePanel;
    [SerializeField] private Button infoPageButton;
    [SerializeField] private Button infoPageBackButton;

    [Header("Guide Page")]
    [SerializeField] private GameObject guidePagePanel;
    [SerializeField] private Button guidePageButton;
    [SerializeField] private Button guidePageBackButton;

    private bool isAutoPlayPanelOpen;
    private bool isSpinPointerHeld;
    private bool suppressNextSpinClick;
    private Coroutine spinHoldCoroutine;
    private EventTrigger spinEventTrigger;
    private EventTrigger.Entry spinPointerDownEntry;
    private EventTrigger.Entry spinPointerUpEntry;
    private EventTrigger.Entry spinPointerExitEntry;
    private RectTransform autoPlayPanelViewport;
    private RectTransform autoPlayPanelRectTransform;
    private Vector3 autoPlayPanelRestingLocalPosition;
    private Vector3 autoPlayPanelRestingScale;
    private Tween autoPlayPanelTween;
    private bool isAutoPlayPanelClosing;
    private bool isWaitingForStoppedAutoPlayRound;
    private bool waitForAutoPlayDismissPointerRelease;
    private readonly List<RaycastResult> autoPlayDismissRaycastResults = new List<RaycastResult>();
    private CanvasGroup hamburgerMenuCanvasGroup;
    private Tween hamburgerMenuTween;
    private bool isHamburgerMenuOpen;
    private Sequence winLineAmountFontSizeSequence;

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

        if (ultraStartButton == null)
        {
            ultraStartButton = FindButtonByName("Start");
        }

        if (ultraStartButton == null)
        {
            Debug.LogError(
                "[UIManager] Ultra Start Button is not assigned and no scene Button named 'Start' was found.");
        }

        if (ultraTakeButton == null)
        {
            ultraTakeButton = FindButtonByName("Take");
        }

        if (ultraTakeButton == null)
        {
            Debug.LogError(
                "[UIManager] Ultra Take Button is not assigned and no scene Button named 'Take' was found.");
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

        ResolveWinLineAmountReferences();
        ResetWinLineAmountDisplay();
        if (topWinLineAmountText == null ||
            middleWinLineAmountText == null ||
            bottomWinLineAmountText == null)
        {
            Debug.LogWarning(
                "[UIManager] Assign the Top, Middle, and Bottom Win Line Amount Text fields under Win Line Row Amounts.");
        }

        if (wildMultiplier2xIcon == null ||
            wildMultiplier3xIcon == null ||
            wildMultiplier4xIcon == null ||
            wildMultiplier5xIcon == null)
        {
            Debug.LogWarning(
                "[UIManager] Assign the 2x, 3x, 4x, and 5x sprites under Wild Multiplier Icons.");
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
        else
        {
            autoPlayPanelRectTransform = autoPlayPanel.GetComponent<RectTransform>();
            if (autoPlayPanelRectTransform == null)
            {
                Debug.LogError("[UIManager] Auto Play Panel requires a RectTransform for its slide animation.");
            }
            else
            {
                CreateAutoPlayPanelViewport();
                autoPlayPanelRestingLocalPosition = autoPlayPanelRectTransform.localPosition;
                autoPlayPanelRestingScale = autoPlayPanelRectTransform.localScale;
            }
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

        if (hamburgerMenuPanel == null ||
            hamburgerMenuButton == null ||
            hamburgerMenuDownButton == null)
        {
            Debug.LogError("[UIManager] Hamburger menu panel and toggle buttons must be assigned.");
        }
        else
        {
            hamburgerMenuCanvasGroup = hamburgerMenuPanel.GetComponent<CanvasGroup>();
            if (hamburgerMenuCanvasGroup == null)
            {
                hamburgerMenuCanvasGroup = hamburgerMenuPanel.AddComponent<CanvasGroup>();
            }

            ResetHamburgerMenu();
        }

        ResolveInfoPageReferences();
        if (infoPagePanel == null ||
            infoPageButton == null ||
            infoPageBackButton == null)
        {
            Debug.LogError(
                "[UIManager] Info Page panel, Info button, and Back button must be assigned.");
        }
        else
        {
            ResetInfoPage();
        }

        ResolveGuidePageReferences();
        if (guidePagePanel == null ||
            guidePageButton == null ||
            guidePageBackButton == null)
        {
            Debug.LogError(
                "[UIManager] Guide Page panel, Guide button, and Back button must be assigned.");
        }
        else
        {
            ResetGuidePage();
        }

        if (gameManager == null)
        {
            Debug.LogError("[UIManager] GameManager is not assigned.");
        }
    }

    private static Button FindButtonByName(string buttonName)
    {
        Button[] sceneButtons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button sceneButton in sceneButtons)
        {
            if (sceneButton != null &&
                sceneButton.gameObject.scene.IsValid() &&
                string.Equals(sceneButton.name, buttonName, System.StringComparison.Ordinal))
            {
                return sceneButton;
            }
        }

        return null;
    }

    private static TMP_Text FindTextByName(string textName)
    {
        TMP_Text[] sceneTexts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (TMP_Text sceneText in sceneTexts)
        {
            if (sceneText != null &&
                sceneText.gameObject.scene.IsValid() &&
                string.Equals(
                    sceneText.name,
                    textName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return sceneText;
            }
        }

        return null;
    }

    internal Sprite GetWildMultiplierIcon(int multiplier)
    {
        switch (multiplier)
        {
            case 2:
                return wildMultiplier2xIcon;
            case 3:
                return wildMultiplier3xIcon;
            case 4:
                return wildMultiplier4xIcon;
            case 5:
                return wildMultiplier5xIcon;
            default:
                return null;
        }
    }

    internal float GetWildMultiplierPopScale()
    {
        return Mathf.Max(1f, wildMultiplierPopScale);
    }

    internal float GetWildMultiplierShakeAngle()
    {
        return Mathf.Clamp(wildMultiplierShakeAngle, 0f, 2f);
    }

    internal int GetWildMultiplierShakesPerSymbolLoop()
    {
        return Mathf.Max(1, wildMultiplierShakesPerSymbolLoop);
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

        if (ultraStartButton != null)
        {
            ultraStartButton.onClick.AddListener(OnUltraStartButtonClicked);
        }

        if (ultraTakeButton != null)
        {
            ultraTakeButton.onClick.AddListener(OnUltraTakeButtonClicked);
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
        if (hamburgerMenuButton != null)
            hamburgerMenuButton.onClick.AddListener(OpenHamburgerMenu);
        if (hamburgerMenuDownButton != null)
            hamburgerMenuDownButton.onClick.AddListener(CloseHamburgerMenu);
        if (infoPageButton != null)
            infoPageButton.onClick.AddListener(OpenInfoPage);
        if (infoPageBackButton != null)
            infoPageBackButton.onClick.AddListener(CloseInfoPage);
        if (guidePageButton != null)
            guidePageButton.onClick.AddListener(OpenGuidePage);
        if (guidePageBackButton != null)
            guidePageBackButton.onClick.AddListener(CloseGuidePage);

        if (gameManager != null)
        {
            gameManager.SpinActivityChanged += OnSpinActivityChanged;
            gameManager.SpinSpeedChanged += OnSpinSpeedChanged;
            gameManager.GamePresentationChanged += OnGamePresentationChanged;
            gameManager.AutoPlayChanged += OnAutoPlayChanged;
        }

        if (slotView != null)
        {
            slotView.WinLineAmountPresentationChanged +=
                OnWinLineAmountPresentationChanged;
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
        waitForAutoPlayDismissPointerRelease = false;
        ResetAutoPlayPanelAnimation();
        ResetHamburgerMenu();
        UnregisterSpinHoldEvents();

        if (spinButton != null)
        {
            spinButton.onClick.RemoveListener(OnSpinButtonClicked);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(OnStopButtonClicked);
        }

        if (ultraStartButton != null)
        {
            ultraStartButton.onClick.RemoveListener(OnUltraStartButtonClicked);
        }

        if (ultraTakeButton != null)
        {
            ultraTakeButton.onClick.RemoveListener(OnUltraTakeButtonClicked);
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
        if (hamburgerMenuButton != null)
            hamburgerMenuButton.onClick.RemoveListener(OpenHamburgerMenu);
        if (hamburgerMenuDownButton != null)
            hamburgerMenuDownButton.onClick.RemoveListener(CloseHamburgerMenu);
        if (infoPageButton != null)
            infoPageButton.onClick.RemoveListener(OpenInfoPage);
        if (infoPageBackButton != null)
            infoPageBackButton.onClick.RemoveListener(CloseInfoPage);
        if (guidePageButton != null)
            guidePageButton.onClick.RemoveListener(OpenGuidePage);
        if (guidePageBackButton != null)
            guidePageBackButton.onClick.RemoveListener(CloseGuidePage);

        if (gameManager != null)
        {
            gameManager.SpinActivityChanged -= OnSpinActivityChanged;
            gameManager.SpinSpeedChanged -= OnSpinSpeedChanged;
            gameManager.GamePresentationChanged -= OnGamePresentationChanged;
            gameManager.AutoPlayChanged -= OnAutoPlayChanged;
        }

        if (slotView != null)
        {
            slotView.WinLineAmountPresentationChanged -=
                OnWinLineAmountPresentationChanged;
        }

        ResetWinLineAmountDisplay();
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

        if (!gameManager.CanRequestSpin())
        {
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

        // The GameManager rejects repeated stop requests. Keep the visible
        // button visually enabled so unavailable clicks simply do nothing.
    }

    private void OnUltraStartButtonClicked()
    {
        if (gameManager == null)
        {
            Debug.LogError("[UIManager] Cannot start the Ultra slot because GameManager is not assigned.");
            RefreshSpinControls();
            return;
        }

        if (!gameManager.RequestUltraStart())
        {
            RefreshSpinControls();
        }
    }

    private void OnUltraTakeButtonClicked()
    {
        if (gameManager == null)
        {
            Debug.LogError(
                "[UIManager] Cannot take the Ultra reward because GameManager is not assigned.");
            RefreshSpinControls();
            return;
        }

        if (!gameManager.RequestUltraTake())
        {
            RefreshSpinControls();
        }
    }

    private void OnSpinActivityChanged(bool isRoundActive)
    {
        if (!isRoundActive)
        {
            isWaitingForStoppedAutoPlayRound = false;
        }

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
        RefreshSpinModeButtons();
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
        isWaitingForStoppedAutoPlayRound = false;

        if (gameManager == null || !gameManager.StartAutoPlay(rounds))
        {
            RefreshAutoPlayControls();
        }
    }

    private void OnAutoPlayStopButtonClicked()
    {
        if (gameManager == null) return;

        // Autoplay stops scheduling new rounds immediately, but an active
        // server-backed round must still settle. During that short period show
        // the spin control (disabled), never the normal manual-stop control.
        isWaitingForStoppedAutoPlayRound = gameManager.IsSpinRoundActive();
        gameManager.StopAutoPlay();
        RefreshAutoPlayControls();
        RefreshSpinControls();
    }

    private void OnAutoPlayChanged()
    {
        if (gameManager != null && gameManager.isAutoPlaying)
        {
            isAutoPlayPanelOpen = false;
            isWaitingForStoppedAutoPlayRound = false;
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
        waitForAutoPlayDismissPointerRelease = true;
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
        waitForAutoPlayDismissPointerRelease = false;
        RefreshAutoPlayControls();
    }

    private void Update()
    {
        if (!isAutoPlayPanelOpen) return;

        // The panel opens while the spin-button pointer is still held. Wait for
        // that original press to end before accepting a click as a dismissal.
        if (waitForAutoPlayDismissPointerRelease)
        {
            if (!IsPrimaryPointerPressed())
            {
                waitForAutoPlayDismissPointerRelease = false;
            }
            return;
        }

        if (!TryGetPrimaryPointerDownPosition(out Vector2 pointerPosition)) return;
        if (IsAutoPlayDismissException(pointerPosition)) return;

        CloseAutoPlayPanel();
    }

    private static bool IsPrimaryPointerPressed()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }

    private static bool TryGetPrimaryPointerDownPosition(out Vector2 pointerPosition)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            pointerPosition = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pointerPosition = mouse.position.ReadValue();
            return true;
        }

        pointerPosition = default;
        return false;
    }

    private bool IsAutoPlayDismissException(Vector2 pointerPosition)
    {
        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null) return false;

        PointerEventData pointerData = new PointerEventData(currentEventSystem)
        {
            position = pointerPosition
        };

        autoPlayDismissRaycastResults.Clear();
        currentEventSystem.RaycastAll(pointerData, autoPlayDismissRaycastResults);

        foreach (RaycastResult result in autoPlayDismissRaycastResults)
        {
            Transform hitTransform = result.gameObject.transform;
            if (IsTransformInside(hitTransform, autoPlayPanel != null ? autoPlayPanel.transform : null) ||
                IsTransformInside(hitTransform, increaseBetButton != null ? increaseBetButton.transform : null) ||
                IsTransformInside(hitTransform, decreaseBetButton != null ? decreaseBetButton.transform : null) ||
                IsTransformInside(hitTransform, normalSpinButton != null ? normalSpinButton.transform : null) ||
                IsTransformInside(hitTransform, fastSpinButton != null ? fastSpinButton.transform : null) ||
                IsTransformInside(hitTransform, skipSpinButton != null ? skipSpinButton.transform : null))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTransformInside(Transform candidate, Transform allowedRoot)
    {
        return candidate != null && allowedRoot != null &&
               (candidate == allowedRoot || candidate.IsChildOf(allowedRoot));
    }

    private void CreateAutoPlayPanelViewport()
    {
        RectTransform originalParent = autoPlayPanelRectTransform.parent as RectTransform;
        if (originalParent == null) return;

        int originalSiblingIndex = autoPlayPanelRectTransform.GetSiblingIndex();
        Vector2 originalAnchorMin = autoPlayPanelRectTransform.anchorMin;
        Vector2 originalAnchorMax = autoPlayPanelRectTransform.anchorMax;
        Vector3 originalAnchoredPosition = autoPlayPanelRectTransform.anchoredPosition3D;
        Vector2 originalSizeDelta = autoPlayPanelRectTransform.sizeDelta;
        Vector2 originalPivot = autoPlayPanelRectTransform.pivot;
        Quaternion originalLocalRotation = autoPlayPanelRectTransform.localRotation;
        Vector3 originalLocalScale = autoPlayPanelRectTransform.localScale;

        GameObject viewportObject = new GameObject(
            "AutoplayPanelAnimationViewport",
            typeof(RectTransform),
            typeof(RectMask2D)
        );
        viewportObject.layer = autoPlayPanel.layer;

        autoPlayPanelViewport = viewportObject.GetComponent<RectTransform>();
        autoPlayPanelViewport.SetParent(originalParent, false);
        autoPlayPanelViewport.SetSiblingIndex(originalSiblingIndex);
        autoPlayPanelViewport.anchorMin = originalAnchorMin;
        autoPlayPanelViewport.anchorMax = originalAnchorMax;
        autoPlayPanelViewport.anchoredPosition3D = originalAnchoredPosition;
        autoPlayPanelViewport.sizeDelta = originalSizeDelta;
        autoPlayPanelViewport.pivot = originalPivot;
        autoPlayPanelViewport.localRotation = originalLocalRotation;
        autoPlayPanelViewport.localScale = originalLocalScale;

        autoPlayPanelRectTransform.SetParent(autoPlayPanelViewport, false);
        autoPlayPanelRectTransform.anchorMin = Vector2.zero;
        autoPlayPanelRectTransform.anchorMax = Vector2.one;
        autoPlayPanelRectTransform.offsetMin = Vector2.zero;
        autoPlayPanelRectTransform.offsetMax = Vector2.zero;
        autoPlayPanelRectTransform.pivot = originalPivot;
        autoPlayPanelRectTransform.localRotation = Quaternion.identity;
        autoPlayPanelRectTransform.localScale = Vector3.one;
    }

    private void ShowAutoPlayPanelAnimated()
    {
        if (autoPlayPanel == null) return;

        if (autoPlayPanelRectTransform == null)
        {
            autoPlayPanel.SetActive(true);
            return;
        }

        bool isReversingClose = isAutoPlayPanelClosing && autoPlayPanel.activeSelf;
        autoPlayPanelTween?.Kill();
        autoPlayPanelTween = null;
        isAutoPlayPanelClosing = false;

        float fullPanelDistance = autoPlayPanelRectTransform.rect.height + 2f;
        if (!isReversingClose)
        {
            // Activate while fully below the viewport. The mask reveals the
            // panel progressively as it travels upward into its resting place.
            autoPlayPanelRectTransform.localPosition = autoPlayPanelRestingLocalPosition
                + (Vector3.down * Mathf.Max(autoPlayPanelSlideDistance, fullPanelDistance));
        }

        autoPlayPanelRectTransform.localScale = autoPlayPanelRestingScale;
        autoPlayPanel.SetActive(true);

        float duration = Mathf.Max(0.01f, autoPlayPanelSlideDuration) * 1.35f;
        autoPlayPanelTween = autoPlayPanelRectTransform
            .DOLocalMove(autoPlayPanelRestingLocalPosition, duration)
            .SetEase(Ease.InOutCubic)
            .SetUpdate(true)
            .OnComplete(() => autoPlayPanelTween = null);
    }

    private void HideAutoPlayPanelAnimated()
    {
        if (autoPlayPanel == null) return;

        waitForAutoPlayDismissPointerRelease = false;
        autoPlayPanelTween?.Kill();
        autoPlayPanelTween = null;

        if (autoPlayPanelRectTransform == null)
        {
            autoPlayPanel.SetActive(false);
            isAutoPlayPanelClosing = false;
            return;
        }

        isAutoPlayPanelClosing = true;
        float duration = Mathf.Max(0.01f, autoPlayPanelSlideDuration) * 1.35f;
        float fullPanelDistance = autoPlayPanelRectTransform.rect.height + 2f;
        Vector3 closeTargetPosition = autoPlayPanelRestingLocalPosition
            + (Vector3.down * Mathf.Max(autoPlayPanelSlideDistance, fullPanelDistance));

        autoPlayPanelRectTransform.localScale = autoPlayPanelRestingScale;
        autoPlayPanelTween = autoPlayPanelRectTransform
            .DOLocalMove(closeTargetPosition, duration)
            .SetEase(Ease.InOutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Hide first, then restore the resting transform invisibly so
                // the panel can never flash back into view for one frame.
                autoPlayPanel.SetActive(false);
                autoPlayPanelRectTransform.localPosition = autoPlayPanelRestingLocalPosition;
                autoPlayPanelRectTransform.localScale = autoPlayPanelRestingScale;
                isAutoPlayPanelClosing = false;
                autoPlayPanelTween = null;
            });
    }

    private void ResetAutoPlayPanelAnimation()
    {
        autoPlayPanelTween?.Kill();
        autoPlayPanelTween = null;
        isAutoPlayPanelClosing = false;

        if (autoPlayPanelRectTransform != null)
        {
            autoPlayPanelRectTransform.localPosition = autoPlayPanelRestingLocalPosition;
            autoPlayPanelRectTransform.localScale = autoPlayPanelRestingScale;
        }
    }

    private void OpenHamburgerMenu()
    {
        if (hamburgerMenuPanel == null || hamburgerMenuCanvasGroup == null) return;

        hamburgerMenuTween?.Kill();
        hamburgerMenuTween = null;
        isHamburgerMenuOpen = true;

        if (hamburgerMenuButton != null)
        {
            hamburgerMenuButton.gameObject.SetActive(false);
        }
        if (hamburgerMenuDownButton != null)
        {
            hamburgerMenuDownButton.gameObject.SetActive(true);
            hamburgerMenuDownButton.interactable = true;
        }

        hamburgerMenuPanel.SetActive(true);
        hamburgerMenuCanvasGroup.alpha = 0f;
        hamburgerMenuCanvasGroup.interactable = true;
        hamburgerMenuCanvasGroup.blocksRaycasts = true;

        hamburgerMenuTween = hamburgerMenuCanvasGroup
            .DOFade(1f, Mathf.Max(0.01f, hamburgerMenuFadeDuration))
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => hamburgerMenuTween = null);
    }

    private void CloseHamburgerMenu()
    {
        if (!isHamburgerMenuOpen ||
            hamburgerMenuPanel == null ||
            hamburgerMenuCanvasGroup == null)
        {
            return;
        }

        hamburgerMenuTween?.Kill();
        hamburgerMenuTween = null;
        isHamburgerMenuOpen = false;
        // Keep child buttons visually in their normal state while fading.
        // Blocking raycasts prevents clicks without triggering disabled sprites.
        hamburgerMenuCanvasGroup.blocksRaycasts = false;

        hamburgerMenuTween = hamburgerMenuCanvasGroup
            .DOFade(0f, Mathf.Max(0.01f, hamburgerMenuFadeDuration))
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                hamburgerMenuPanel.SetActive(false);

                if (hamburgerMenuDownButton != null)
                {
                    hamburgerMenuDownButton.gameObject.SetActive(false);
                    hamburgerMenuDownButton.interactable = true;
                }
                if (hamburgerMenuButton != null)
                {
                    hamburgerMenuButton.gameObject.SetActive(true);
                }

                hamburgerMenuTween = null;
            });
    }

    private void ResetHamburgerMenu()
    {
        hamburgerMenuTween?.Kill();
        hamburgerMenuTween = null;
        isHamburgerMenuOpen = false;

        if (hamburgerMenuCanvasGroup != null)
        {
            hamburgerMenuCanvasGroup.alpha = 0f;
            hamburgerMenuCanvasGroup.interactable = false;
            hamburgerMenuCanvasGroup.blocksRaycasts = false;
        }
        if (hamburgerMenuPanel != null)
        {
            hamburgerMenuPanel.SetActive(false);
        }
        if (hamburgerMenuDownButton != null)
        {
            hamburgerMenuDownButton.gameObject.SetActive(false);
            hamburgerMenuDownButton.interactable = true;
        }
        if (hamburgerMenuButton != null)
        {
            hamburgerMenuButton.gameObject.SetActive(true);
        }
    }

    private void ResolveInfoPageReferences()
    {
        if (infoPagePanel == null)
        {
            infoPagePanel = FindGameObjectByName("InfoPage");
        }

        if (infoPageButton == null)
        {
            infoPageButton = FindButtonByName("Info");
        }

        if (infoPageBackButton == null)
        {
            infoPageBackButton = FindButtonByName("BackButton");
        }
    }

    private void ResolveGuidePageReferences()
    {
        if (guidePagePanel == null)
        {
            guidePagePanel =
                FindOutermostGameObjectByName("GuidePage");
        }

        if (guidePageButton == null)
        {
            guidePageButton =
                FindButtonByName("Guide") ??
                FindButtonByName("Bulb");
        }

        if (guidePageBackButton == null &&
            guidePagePanel != null)
        {
            guidePageBackButton =
                FindButtonInChildrenByName(
                    guidePagePanel,
                    "BackButton");
        }
    }

    private static GameObject FindGameObjectByName(string objectName)
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform != null &&
                sceneTransform.gameObject.scene.IsValid() &&
                string.Equals(
                    sceneTransform.name,
                    objectName,
                    System.StringComparison.Ordinal))
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindOutermostGameObjectByName(
        string objectName)
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        GameObject fallback = null;

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform == null ||
                !sceneTransform.gameObject.scene.IsValid() ||
                !string.Equals(
                    sceneTransform.name,
                    objectName,
                    System.StringComparison.Ordinal))
            {
                continue;
            }

            fallback ??= sceneTransform.gameObject;
            if (sceneTransform.parent == null ||
                !string.Equals(
                    sceneTransform.parent.name,
                    objectName,
                    System.StringComparison.Ordinal))
            {
                return sceneTransform.gameObject;
            }
        }

        return fallback;
    }

    private static Button FindButtonInChildrenByName(
        GameObject parent,
        string buttonName)
    {
        if (parent == null)
        {
            return null;
        }

        Button[] buttons =
            parent.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null &&
                string.Equals(
                    button.name,
                    buttonName,
                    System.StringComparison.Ordinal))
            {
                return button;
            }
        }

        return null;
    }

    private void OpenInfoPage()
    {
        if (infoPagePanel == null)
        {
            Debug.LogError("[UIManager] Cannot open Info Page because its panel is not assigned.");
            return;
        }

        CloseAutoPlayPanel();
        ResetHamburgerMenu();
        CloseGuidePage();
        infoPagePanel.SetActive(true);
    }

    private void CloseInfoPage()
    {
        if (infoPagePanel != null)
        {
            infoPagePanel.SetActive(false);
        }
    }

    private void ResetInfoPage()
    {
        CloseInfoPage();
    }

    private void OpenGuidePage()
    {
        if (guidePagePanel == null)
        {
            Debug.LogError(
                "[UIManager] Cannot open Guide Page because its panel is not assigned.");
            return;
        }

        CloseAutoPlayPanel();
        ResetHamburgerMenu();
        CloseInfoPage();
        guidePagePanel.SetActive(true);
    }

    private void CloseGuidePage()
    {
        if (guidePagePanel != null)
        {
            guidePagePanel.SetActive(false);
        }
    }

    private void ResetGuidePage()
    {
        CloseGuidePage();
    }

    private void RefreshSpinControls()
    {
        bool isRoundActive = gameManager != null && gameManager.IsSpinRoundActive();
        ApplySpinControlState(isRoundActive);
    }

    private void ApplySpinControlState(bool isRoundActive)
    {
        bool isUltraUnlocked = gameManager != null && gameManager.IsUltraSlotUnlocked();
        bool showUltraTake =
            isUltraUnlocked &&
            gameManager != null &&
            gameManager.ShouldShowUltraTakeButton();
        bool showUltraStart =
            isUltraUnlocked &&
            !showUltraTake &&
            gameManager != null &&
            gameManager.ShouldShowUltraStartButton();
        bool showStopButton =
            !isUltraUnlocked &&
            gameManager != null &&
            gameManager.IsSpinning();
        bool showSpinButton =
            !showStopButton &&
            !showUltraStart &&
            !showUltraTake;

        if (spinButton != null)
        {
            spinButton.gameObject.SetActive(showSpinButton);
            // Keep the visible control in its normal visual state. The click
            // handler asks GameManager whether the action is currently valid.
            spinButton.interactable = true;
        }

        if (stopButton != null)
        {
            stopButton.gameObject.SetActive(showStopButton);
            stopButton.interactable = true;
        }

        if (ultraStartButton != null)
        {
            ultraStartButton.gameObject.SetActive(showUltraStart);
            ultraStartButton.interactable = true;
        }

        if (ultraTakeButton != null)
        {
            ultraTakeButton.gameObject.SetActive(showUltraTake);
            ultraTakeButton.interactable = true;
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

    private void ResolveWinLineAmountReferences()
    {
        if (slotView == null)
        {
            slotView = FindFirstObjectByType<SlotView>(
                FindObjectsInactive.Include);
        }
    }

    private void OnWinLineAmountPresentationChanged(int row, double winAmount)
    {
        ResetWinLineAmountDisplay();

        if (row < 0 || winAmount <= 0)
        {
            return;
        }

        TMP_Text rowText = GetWinLineAmountText(row);
        if (rowText == null)
        {
            return;
        }

        rowText.text = winAmount.ToString("0.00");
        rowText.fontSize = 0f;
        rowText.gameObject.SetActive(true);

        float peakFontSize = Mathf.Max(1f, winLinePeakFontSize);
        float finalFontSize = Mathf.Clamp(
            winLineFinalFontSize,
            1f,
            peakFontSize);

        winLineAmountFontSizeSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                DOTween.To(
                    () => rowText.fontSize,
                    value => rowText.fontSize = value,
                    peakFontSize,
                    Mathf.Max(0.01f, winLineGrowDuration))
                    .SetEase(Ease.OutCubic))
            .Append(
                DOTween.To(
                    () => rowText.fontSize,
                    value => rowText.fontSize = value,
                    finalFontSize,
                    Mathf.Max(0.01f, winLineSettleDuration))
                    .SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                winLineAmountFontSizeSequence = null;
                rowText.fontSize = finalFontSize;
            });
    }

    private TMP_Text GetWinLineAmountText(int row)
    {
        switch (row)
        {
            case 0:
                return topWinLineAmountText;
            case 1:
                return middleWinLineAmountText;
            case 2:
                return bottomWinLineAmountText;
            default:
                return null;
        }
    }

    private void ResetWinLineAmountDisplay()
    {
        winLineAmountFontSizeSequence?.Kill();
        winLineAmountFontSizeSequence = null;

        HideWinLineAmountText(topWinLineAmountText);
        HideWinLineAmountText(middleWinLineAmountText);
        HideWinLineAmountText(bottomWinLineAmountText);
    }

    private void HideWinLineAmountText(TMP_Text rowText)
    {
        if (rowText == null)
        {
            return;
        }

        rowText.fontSize = Mathf.Max(1f, winLineFinalFontSize);
        rowText.gameObject.SetActive(false);
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
        bool isUltraUnlocked = gameManager != null && gameManager.IsUltraSlotUnlocked();
        bool canStartAutoPlay = gameManager != null && gameManager.CanStartAutoPlay();
        bool shouldShowAutoPlayPanel = isAutoPlayPanelOpen &&
                                       !isAutoPlaying &&
                                       !isUltraUnlocked;

        if (autoPlayPanel != null)
        {
            if (shouldShowAutoPlayPanel)
            {
                if (!autoPlayPanel.activeSelf || isAutoPlayPanelClosing)
                {
                    ShowAutoPlayPanelAnimated();
                }
            }
            else if (autoPlayPanel.activeSelf && !isAutoPlayPanelClosing)
            {
                HideAutoPlayPanelAnimated();
            }
        }

        bool canUseAutoPlayChoices = canStartAutoPlay &&
                                     shouldShowAutoPlayPanel &&
                                     !isAutoPlayPanelClosing;
        SetButtonInteractable(autoPlay10Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay50Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay100Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay200Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay500Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlayInfiniteButton, canUseAutoPlayChoices);

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
