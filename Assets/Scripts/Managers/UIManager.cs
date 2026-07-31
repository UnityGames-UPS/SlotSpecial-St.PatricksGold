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
    [Tooltip(
        "Shows shared error and exit confirmation popups. " +
        "Found automatically when left empty.")]
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private OrientationChange orientationChange;
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

    [Header("Shared Sound Panel")]
    [SerializeField] private GameObject soundPanel;
    [SerializeField] private Button soundPanelButton;
    [SerializeField] private Button soundPanelCloseButton;

    [Header("Platform Navigation")]
    [SerializeField] private JSFunctCalls jsFunctCalls;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button moreGamesButton;
    [UnityEngine.Serialization.FormerlySerializedAs("fullScreenButton")]
    [SerializeField] private Button expandButton;
    [UnityEngine.Serialization.FormerlySerializedAs("smallScreenButton")]
    [SerializeField] private Button shrinkButton;

    [Header("Portrait Main Controls")]
    [SerializeField] private Button portraitSpinButton;
    [SerializeField] private Button portraitStopButton;
    [SerializeField] private Button portraitUltraStartButton;
    [SerializeField] private Button portraitUltraTakeButton;

    [Header("Portrait Spin Mode Controls")]
    [SerializeField] private Button portraitNormalSpinButton;
    [SerializeField] private Button portraitFastSpinButton;
    [SerializeField] private Button portraitSkipSpinButton;

    [Header("Portrait Dynamic Game Text")]
    [SerializeField] private TMP_Text portraitWinLinesCountText;
    [SerializeField] private TMP_Text portraitGoodLuckText;
    [SerializeField] private TMP_Text portraitWonLabelText;
    [SerializeField] private TMP_Text portraitWinAmountText;
    [SerializeField] private TMP_Text portraitBalanceAmountText;
    [SerializeField] private TMP_Text portraitBetAmountText;

    [Header("Portrait Bet Controls")]
    [SerializeField] private Button portraitIncreaseBetButton;
    [SerializeField] private Button portraitDecreaseBetButton;

    [Header("Portrait Auto Play")]
    [SerializeField] private GameObject portraitAutoPlayPanel;
    [SerializeField] private Button portraitAutoPlay10Button;
    [SerializeField] private Button portraitAutoPlay50Button;
    [SerializeField] private Button portraitAutoPlay100Button;
    [SerializeField] private Button portraitAutoPlay200Button;
    [SerializeField] private Button portraitAutoPlay500Button;
    [SerializeField] private Button portraitAutoPlayInfiniteButton;
    [SerializeField] private Button portraitAutoPlayStopButton;
    [SerializeField] private TMP_Text portraitAutoPlayCountText;

    [Header("Portrait Hamburger Menu")]
    [SerializeField] private GameObject portraitHamburgerMenuPanel;
    [SerializeField] private Button portraitHamburgerMenuButton;
    [SerializeField] private Button portraitHamburgerMenuDownButton;

    [Header("Portrait Page Buttons")]
    [SerializeField] private Button portraitInfoPageButton;
    [SerializeField] private Button portraitGuidePageButton;
    [SerializeField] private Button portraitSoundPanelButton;

    [Header("Portrait Platform Navigation")]
    [SerializeField] private Button portraitHomeButton;
    [SerializeField] private Button portraitMoreGamesButton;
    [UnityEngine.Serialization.FormerlySerializedAs(
        "portraitFullScreenButton")]
    [SerializeField] private Button portraitExpandButton;
    [UnityEngine.Serialization.FormerlySerializedAs(
        "portraitSmallScreenButton")]
    [SerializeField] private Button portraitShrinkButton;

    private bool isAutoPlayPanelOpen;
    private bool isSpinPointerHeld;
    private bool suppressNextSpinClick;
    private Coroutine spinHoldCoroutine;
    private SpinHoldEventRegistration landscapeSpinHoldRegistration;
    private SpinHoldEventRegistration portraitSpinHoldRegistration;
    private AutoPlayPanelAnimationState landscapeAutoPlayPanelAnimation;
    private AutoPlayPanelAnimationState portraitAutoPlayPanelAnimation;
    private bool isWaitingForStoppedAutoPlayRound;
    private bool waitForAutoPlayDismissPointerRelease;
    private bool isExpanded;
    private readonly List<RaycastResult> autoPlayDismissRaycastResults = new List<RaycastResult>();
    private CanvasGroup hamburgerMenuCanvasGroup;
    private CanvasGroup portraitHamburgerMenuCanvasGroup;
    private Tween hamburgerMenuTween;
    private Tween portraitHamburgerMenuTween;
    private Coroutine hamburgerMenuShowCoroutine;
    private Coroutine portraitHamburgerMenuShowCoroutine;
    private bool isHamburgerMenuOpen;
    private bool isPortraitPresentationActive;
    private Sequence winLineAmountFontSizeSequence;

    private sealed class SpinHoldEventRegistration
    {
        public EventTrigger Trigger;
        public EventTrigger.Entry PointerDown;
        public EventTrigger.Entry PointerUp;
        public EventTrigger.Entry PointerExit;
    }

    private sealed class AutoPlayPanelAnimationState
    {
        public GameObject Panel;
        public RectTransform RectTransform;
        public CanvasGroup CanvasGroup;
        public Vector3 RestingLocalPosition;
        public Vector3 RestingScale;
        public Tween Tween;
        public bool IsClosing;
    }

    private void Awake()
    {
        if (popupManager == null)
        {
            popupManager = FindFirstObjectByType<PopupManager>(
                FindObjectsInactive.Include);
        }

        if (orientationChange == null)
        {
            orientationChange = FindFirstObjectByType<OrientationChange>(
                FindObjectsInactive.Include);
        }

        if (orientationChange == null)
        {
            Debug.LogWarning(
                "[UIManager] Orientation Change is not assigned. UI state " +
                "cannot be synchronized during layout changes.");
        }

        isPortraitPresentationActive =
            orientationChange != null &&
            orientationChange.CurrentMode ==
                OrientationChange.OrientationMode.MobilePortrait;

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
            landscapeAutoPlayPanelAnimation =
                CreateAutoPlayPanelAnimationState(autoPlayPanel);
            if (landscapeAutoPlayPanelAnimation == null)
            {
                Debug.LogError(
                    "[UIManager] Auto Play Panel requires a RectTransform " +
                    "for its popup animation.");
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

        if (portraitAutoPlayPanel == null ||
            portraitAutoPlay10Button == null ||
            portraitAutoPlay50Button == null ||
            portraitAutoPlay100Button == null ||
            portraitAutoPlay200Button == null ||
            portraitAutoPlay500Button == null ||
            portraitAutoPlayInfiniteButton == null ||
            portraitAutoPlayStopButton == null ||
            portraitAutoPlayCountText == null)
        {
            Debug.LogError(
                "[UIManager] One or more Portrait Auto Play references are not assigned.");
        }
        else
        {
            portraitAutoPlayPanelAnimation =
                CreateAutoPlayPanelAnimationState(
                    portraitAutoPlayPanel);
            if (portraitAutoPlayPanelAnimation == null)
            {
                Debug.LogError(
                    "[UIManager] Portrait Auto Play Panel requires a " +
                    "RectTransform for its popup animation.");
            }
        }

        SetAutoPlayPanelImmediate(
            landscapeAutoPlayPanelAnimation,
            false);
        SetAutoPlayPanelImmediate(
            portraitAutoPlayPanelAnimation,
            false);

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

        }

        if (portraitHamburgerMenuPanel == null ||
            portraitHamburgerMenuButton == null ||
            portraitHamburgerMenuDownButton == null)
        {
            Debug.LogError(
                "[UIManager] Portrait hamburger menu panel and toggle buttons must be assigned.");
        }
        else
        {
            portraitHamburgerMenuCanvasGroup =
                portraitHamburgerMenuPanel.GetComponent<CanvasGroup>();
            if (portraitHamburgerMenuCanvasGroup == null)
            {
                portraitHamburgerMenuCanvasGroup =
                    portraitHamburgerMenuPanel.AddComponent<CanvasGroup>();
            }

        }

        ResetHamburgerMenu();

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

        if (soundPanel == null ||
            soundPanelButton == null ||
            soundPanelCloseButton == null ||
            portraitSoundPanelButton == null)
        {
            Debug.LogError(
                "[UIManager] Shared Sound Panel, both Sound buttons, and " +
                "the shared Close button must be assigned.");
        }
        else
        {
            soundPanel.SetActive(false);
        }

        if (jsFunctCalls == null ||
            homeButton == null ||
            portraitHomeButton == null ||
            moreGamesButton == null ||
            portraitMoreGamesButton == null ||
            expandButton == null ||
            shrinkButton == null ||
            portraitExpandButton == null ||
            portraitShrinkButton == null)
        {
            Debug.LogError(
                "[UIManager] JSCall and both landscape/portrait Home, " +
                "MoreGames, FullScreen, and SmallScreen buttons must be assigned.");
        }

        SetExpandShrinkButtons(false);

        if (gameManager == null)
        {
            Debug.LogError("[UIManager] GameManager is not assigned.");
        }

        if (popupManager == null)
        {
            Debug.LogError(
                "[UIManager] PopupManager is not assigned and could not be found.");
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

    private static void AddButtonListener(
        Button button,
        UnityEngine.Events.UnityAction callback)
    {
        if (button != null)
        {
            button.onClick.AddListener(callback);
        }
    }

    private static void RemoveButtonListener(
        Button button,
        UnityEngine.Events.UnityAction callback)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(callback);
        }
    }

    private void OnEnable()
    {
        RegisterSpinHoldEvents();

        if (orientationChange != null)
        {
            isPortraitPresentationActive =
                orientationChange.CurrentMode ==
                    OrientationChange.OrientationMode.MobilePortrait;
            orientationChange.OnOrientationChangedInstance +=
                OnOrientationChanged;
        }

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
        AddButtonListener(soundPanelButton, OpenSoundPanel);
        AddButtonListener(soundPanelCloseButton, CloseSoundPanel);
        AddButtonListener(homeButton, OnHomeButtonClicked);
        AddButtonListener(moreGamesButton, OnMoreGamesButtonClicked);
        AddButtonListener(expandButton, OnExpand);
        AddButtonListener(shrinkButton, OnShrink);

        AddButtonListener(portraitSpinButton, OnSpinButtonClicked);
        AddButtonListener(portraitStopButton, OnStopButtonClicked);
        AddButtonListener(
            portraitUltraStartButton,
            OnUltraStartButtonClicked);
        AddButtonListener(
            portraitUltraTakeButton,
            OnUltraTakeButtonClicked);
        AddButtonListener(
            portraitNormalSpinButton,
            OnNormalSpinButtonClicked);
        AddButtonListener(
            portraitFastSpinButton,
            OnFastSpinButtonClicked);
        AddButtonListener(
            portraitSkipSpinButton,
            OnSkipSpinButtonClicked);
        AddButtonListener(
            portraitIncreaseBetButton,
            OnIncreaseBetButtonClicked);
        AddButtonListener(
            portraitDecreaseBetButton,
            OnDecreaseBetButtonClicked);
        AddButtonListener(
            portraitAutoPlay10Button,
            OnAutoPlay10ButtonClicked);
        AddButtonListener(
            portraitAutoPlay50Button,
            OnAutoPlay50ButtonClicked);
        AddButtonListener(
            portraitAutoPlay100Button,
            OnAutoPlay100ButtonClicked);
        AddButtonListener(
            portraitAutoPlay200Button,
            OnAutoPlay200ButtonClicked);
        AddButtonListener(
            portraitAutoPlay500Button,
            OnAutoPlay500ButtonClicked);
        AddButtonListener(
            portraitAutoPlayInfiniteButton,
            OnAutoPlayInfiniteButtonClicked);
        AddButtonListener(
            portraitAutoPlayStopButton,
            OnAutoPlayStopButtonClicked);
        AddButtonListener(
            portraitHamburgerMenuButton,
            OpenHamburgerMenu);
        AddButtonListener(
            portraitHamburgerMenuDownButton,
            CloseHamburgerMenu);
        AddButtonListener(
            portraitInfoPageButton,
            OpenInfoPage);
        AddButtonListener(
            portraitGuidePageButton,
            OpenGuidePage);
        AddButtonListener(
            portraitSoundPanelButton,
            OpenSoundPanel);
        AddButtonListener(
            portraitHomeButton,
            OnHomeButtonClicked);
        AddButtonListener(
            portraitMoreGamesButton,
            OnMoreGamesButtonClicked);
        AddButtonListener(
            portraitExpandButton,
            OnExpand);
        AddButtonListener(
            portraitShrinkButton,
            OnShrink);

        if (jsFunctCalls != null)
        {
            jsFunctCalls.RegisterFullscreenListener(gameObject.name);
        }

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

        if (orientationChange != null)
        {
            orientationChange.OnOrientationChangedInstance -=
                OnOrientationChanged;
        }

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
        RemoveButtonListener(soundPanelButton, OpenSoundPanel);
        RemoveButtonListener(soundPanelCloseButton, CloseSoundPanel);
        RemoveButtonListener(homeButton, OnHomeButtonClicked);
        RemoveButtonListener(moreGamesButton, OnMoreGamesButtonClicked);
        RemoveButtonListener(expandButton, OnExpand);
        RemoveButtonListener(shrinkButton, OnShrink);

        RemoveButtonListener(portraitSpinButton, OnSpinButtonClicked);
        RemoveButtonListener(portraitStopButton, OnStopButtonClicked);
        RemoveButtonListener(
            portraitUltraStartButton,
            OnUltraStartButtonClicked);
        RemoveButtonListener(
            portraitUltraTakeButton,
            OnUltraTakeButtonClicked);
        RemoveButtonListener(
            portraitNormalSpinButton,
            OnNormalSpinButtonClicked);
        RemoveButtonListener(
            portraitFastSpinButton,
            OnFastSpinButtonClicked);
        RemoveButtonListener(
            portraitSkipSpinButton,
            OnSkipSpinButtonClicked);
        RemoveButtonListener(
            portraitIncreaseBetButton,
            OnIncreaseBetButtonClicked);
        RemoveButtonListener(
            portraitDecreaseBetButton,
            OnDecreaseBetButtonClicked);
        RemoveButtonListener(
            portraitAutoPlay10Button,
            OnAutoPlay10ButtonClicked);
        RemoveButtonListener(
            portraitAutoPlay50Button,
            OnAutoPlay50ButtonClicked);
        RemoveButtonListener(
            portraitAutoPlay100Button,
            OnAutoPlay100ButtonClicked);
        RemoveButtonListener(
            portraitAutoPlay200Button,
            OnAutoPlay200ButtonClicked);
        RemoveButtonListener(
            portraitAutoPlay500Button,
            OnAutoPlay500ButtonClicked);
        RemoveButtonListener(
            portraitAutoPlayInfiniteButton,
            OnAutoPlayInfiniteButtonClicked);
        RemoveButtonListener(
            portraitAutoPlayStopButton,
            OnAutoPlayStopButtonClicked);
        RemoveButtonListener(
            portraitHamburgerMenuButton,
            OpenHamburgerMenu);
        RemoveButtonListener(
            portraitHamburgerMenuDownButton,
            CloseHamburgerMenu);
        RemoveButtonListener(
            portraitInfoPageButton,
            OpenInfoPage);
        RemoveButtonListener(
            portraitGuidePageButton,
            OpenGuidePage);
        RemoveButtonListener(
            portraitSoundPanelButton,
            OpenSoundPanel);
        RemoveButtonListener(
            portraitHomeButton,
            OnHomeButtonClicked);
        RemoveButtonListener(
            portraitMoreGamesButton,
            OnMoreGamesButtonClicked);
        RemoveButtonListener(
            portraitExpandButton,
            OnExpand);
        RemoveButtonListener(
            portraitShrinkButton,
            OnShrink);

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

    private void OnOrientationChanged(
        OrientationChange.OrientationMode mode,
        int width,
        int height)
    {
        isPortraitPresentationActive =
            mode == OrientationChange.OrientationMode.MobilePortrait;

        // A deactivated layout may not receive PointerUp, so do not carry a
        // partially held spin gesture into the newly active controls.
        CancelSpinHold();
        waitForAutoPlayDismissPointerRelease = false;

        SnapAutoPlayPanelsToSharedState();
        SynchronizeHamburgerMenusForOrientation();
        RefreshSpinControls();
        RefreshSpinModeButtons();
        RefreshGameTexts();
        RefreshBetControls();
        RefreshAutoPlayControls();
    }

    private void SnapAutoPlayPanelsToSharedState()
    {
        ResetAutoPlayPanelAnimation();
        SetAutoPlayPanelImmediate(
            landscapeAutoPlayPanelAnimation,
            false);
        SetAutoPlayPanelImmediate(
            portraitAutoPlayPanelAnimation,
            false);
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
        landscapeSpinHoldRegistration =
            RegisterSpinHoldEvents(spinButton);
        portraitSpinHoldRegistration =
            RegisterSpinHoldEvents(portraitSpinButton);
    }

    private SpinHoldEventRegistration RegisterSpinHoldEvents(
        Button targetButton)
    {
        if (targetButton == null)
        {
            return null;
        }

        EventTrigger eventTrigger =
            targetButton.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger =
                targetButton.gameObject.AddComponent<EventTrigger>();
        }

        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers =
                new List<EventTrigger.Entry>();
        }

        var registration = new SpinHoldEventRegistration
        {
            Trigger = eventTrigger,
            PointerDown =
                CreateSpinTriggerEntry(
                    EventTriggerType.PointerDown,
                    OnSpinPointerDown),
            PointerUp =
                CreateSpinTriggerEntry(
                    EventTriggerType.PointerUp,
                    OnSpinPointerUp),
            PointerExit =
                CreateSpinTriggerEntry(
                    EventTriggerType.PointerExit,
                    OnSpinPointerExit)
        };

        eventTrigger.triggers.Add(registration.PointerDown);
        eventTrigger.triggers.Add(registration.PointerUp);
        eventTrigger.triggers.Add(registration.PointerExit);
        return registration;
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
        UnregisterSpinHoldEvents(landscapeSpinHoldRegistration);
        UnregisterSpinHoldEvents(portraitSpinHoldRegistration);

        landscapeSpinHoldRegistration = null;
        portraitSpinHoldRegistration = null;
    }

    private static void UnregisterSpinHoldEvents(
        SpinHoldEventRegistration registration)
    {
        if (registration?.Trigger == null ||
            registration.Trigger.triggers == null)
        {
            return;
        }

        registration.Trigger.triggers.Remove(
            registration.PointerDown);
        registration.Trigger.triggers.Remove(
            registration.PointerUp);
        registration.Trigger.triggers.Remove(
            registration.PointerExit);
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
                IsTransformInside(hitTransform, portraitAutoPlayPanel != null ? portraitAutoPlayPanel.transform : null) ||
                IsTransformInside(hitTransform, increaseBetButton != null ? increaseBetButton.transform : null) ||
                IsTransformInside(hitTransform, decreaseBetButton != null ? decreaseBetButton.transform : null) ||
                IsTransformInside(hitTransform, normalSpinButton != null ? normalSpinButton.transform : null) ||
                IsTransformInside(hitTransform, fastSpinButton != null ? fastSpinButton.transform : null) ||
                IsTransformInside(hitTransform, skipSpinButton != null ? skipSpinButton.transform : null) ||
                IsTransformInside(hitTransform, portraitIncreaseBetButton != null ? portraitIncreaseBetButton.transform : null) ||
                IsTransformInside(hitTransform, portraitDecreaseBetButton != null ? portraitDecreaseBetButton.transform : null) ||
                IsTransformInside(hitTransform, portraitNormalSpinButton != null ? portraitNormalSpinButton.transform : null) ||
                IsTransformInside(hitTransform, portraitFastSpinButton != null ? portraitFastSpinButton.transform : null) ||
                IsTransformInside(hitTransform, portraitSkipSpinButton != null ? portraitSkipSpinButton.transform : null))
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

    private static AutoPlayPanelAnimationState
        CreateAutoPlayPanelAnimationState(GameObject panel)
    {
        if (panel == null)
        {
            return null;
        }

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return null;
        }

        RectTransform originalParent =
            rectTransform.parent as RectTransform;
        bool alreadyWrapped =
            originalParent != null &&
            originalParent.name ==
                "AutoplayPanelAnimationViewport" &&
            originalParent.GetComponent<RectMask2D>() != null;

        if (originalParent != null && !alreadyWrapped)
        {
            int originalSiblingIndex =
                rectTransform.GetSiblingIndex();
            Vector2 originalAnchorMin =
                rectTransform.anchorMin;
            Vector2 originalAnchorMax =
                rectTransform.anchorMax;
            Vector3 originalAnchoredPosition =
                rectTransform.anchoredPosition3D;
            Vector2 originalSizeDelta =
                rectTransform.sizeDelta;
            Vector2 originalPivot =
                rectTransform.pivot;
            Quaternion originalLocalRotation =
                rectTransform.localRotation;
            Vector3 originalLocalScale =
                rectTransform.localScale;

            GameObject viewportObject = new GameObject(
                "AutoplayPanelAnimationViewport",
                typeof(RectTransform),
                typeof(RectMask2D));
            viewportObject.layer = panel.layer;

            RectTransform viewport =
                viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(originalParent, false);
            viewport.SetSiblingIndex(originalSiblingIndex);
            viewport.anchorMin = originalAnchorMin;
            viewport.anchorMax = originalAnchorMax;
            viewport.anchoredPosition3D =
                originalAnchoredPosition;
            viewport.sizeDelta = originalSizeDelta;
            viewport.pivot = originalPivot;
            viewport.localRotation = originalLocalRotation;
            viewport.localScale = originalLocalScale;

            rectTransform.SetParent(viewport, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = originalPivot;
            rectTransform.localRotation =
                Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        return new AutoPlayPanelAnimationState
        {
            Panel = panel,
            RectTransform = rectTransform,
            CanvasGroup = canvasGroup,
            RestingLocalPosition =
                rectTransform.localPosition,
            RestingScale = rectTransform.localScale
        };
    }

    private void ShowAutoPlayPanelAnimated(
        AutoPlayPanelAnimationState animationState)
    {
        if (animationState == null)
        {
            return;
        }

        bool isReversingClose =
            animationState.IsClosing &&
            animationState.Panel.activeSelf;
        animationState.Tween?.Kill();
        animationState.Tween = null;
        animationState.IsClosing = false;
        animationState.CanvasGroup.alpha = 1f;
        animationState.CanvasGroup.interactable = true;
        animationState.CanvasGroup.blocksRaycasts = true;

        if (!isReversingClose)
        {
            float fullPanelDistance =
                animationState.RectTransform.rect.height +
                2f;
            animationState.RectTransform.localPosition =
                animationState.RestingLocalPosition +
                (Vector3.down *
                 Mathf.Max(
                     autoPlayPanelSlideDistance,
                     fullPanelDistance));
        }

        animationState.RectTransform.localScale =
            animationState.RestingScale;
        animationState.Panel.SetActive(true);

        float duration =
            Mathf.Max(
                0.01f,
                autoPlayPanelSlideDuration) *
            1.35f;
        Tween slideTween = animationState.RectTransform
            .DOLocalMove(
                animationState.RestingLocalPosition,
                duration)
            .SetEase(Ease.InOutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                animationState.RectTransform.localPosition =
                    animationState.RestingLocalPosition;
                animationState.Tween = null;
            });

        animationState.Tween = slideTween;
    }

    private void HideAutoPlayPanelAnimated(
        AutoPlayPanelAnimationState animationState)
    {
        if (animationState == null)
        {
            return;
        }

        waitForAutoPlayDismissPointerRelease = false;
        animationState.Tween?.Kill();
        animationState.Tween = null;
        animationState.IsClosing = true;
        animationState.CanvasGroup.interactable = false;
        animationState.CanvasGroup.blocksRaycasts = false;
        animationState.CanvasGroup.alpha = 1f;

        float duration =
            Mathf.Max(
                0.01f,
                autoPlayPanelSlideDuration) *
            1.35f;
        float fullPanelDistance =
            animationState.RectTransform.rect.height +
            2f;
        Vector3 closeTargetPosition =
            animationState.RestingLocalPosition +
            (Vector3.down *
             Mathf.Max(
                 autoPlayPanelSlideDistance,
                 fullPanelDistance));

        animationState.RectTransform.localScale =
            animationState.RestingScale;
        Tween slideTween = animationState.RectTransform
            .DOLocalMove(
                closeTargetPosition,
                duration)
            .SetEase(Ease.InOutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                animationState.Tween = null;
                SetAutoPlayPanelImmediate(
                    animationState,
                    false);
            });

        animationState.Tween = slideTween;
    }

    private static void SetAutoPlayPanelImmediate(
        AutoPlayPanelAnimationState animationState,
        bool isOpen)
    {
        if (animationState == null)
        {
            return;
        }

        animationState.Tween?.Kill();
        animationState.Tween = null;
        animationState.IsClosing = false;
        animationState.CanvasGroup.alpha = 1f;
        animationState.CanvasGroup.interactable = isOpen;
        animationState.CanvasGroup.blocksRaycasts = isOpen;
        animationState.RectTransform.localPosition =
            animationState.RestingLocalPosition;
        animationState.RectTransform.localScale =
            animationState.RestingScale;
        animationState.Panel.SetActive(isOpen);
    }

    private void ResetAutoPlayPanelAnimation()
    {
        ResetAutoPlayPanelAnimation(
            landscapeAutoPlayPanelAnimation);
        ResetAutoPlayPanelAnimation(
            portraitAutoPlayPanelAnimation);
    }

    private static void ResetAutoPlayPanelAnimation(
        AutoPlayPanelAnimationState animationState)
    {
        if (animationState == null)
        {
            return;
        }

        animationState.Tween?.Kill();
        animationState.Tween = null;
        animationState.IsClosing = false;
        if (animationState.RectTransform == null)
        {
            return;
        }

        animationState.RectTransform.localPosition =
            animationState.RestingLocalPosition;
        animationState.RectTransform.localScale =
            animationState.RestingScale;
    }

    private void OpenHamburgerMenu()
    {
        isHamburgerMenuOpen = true;
        SetHamburgerMenuImmediate(
            !isPortraitPresentationActive,
            false);
        ScheduleHamburgerMenuOpen(
            isPortraitPresentationActive);
    }

    private void CloseHamburgerMenu()
    {
        if (!isHamburgerMenuOpen)
        {
            return;
        }

        isHamburgerMenuOpen = false;
        CancelHamburgerMenuOpenCoroutines();
        SetHamburgerMenuImmediate(
            !isPortraitPresentationActive,
            false);
        HideHamburgerMenuAnimated(
            isPortraitPresentationActive);
    }

    private void ResetHamburgerMenu()
    {
        isHamburgerMenuOpen = false;
        CancelHamburgerMenuOpenCoroutines();
        SetHamburgerMenuImmediate(false, false);
        SetHamburgerMenuImmediate(true, false);
    }

    private void SynchronizeHamburgerMenusForOrientation()
    {
        CancelHamburgerMenuOpenCoroutines();
        SetHamburgerMenuImmediate(false, false);
        SetHamburgerMenuImmediate(true, false);

        if (isHamburgerMenuOpen)
        {
            ScheduleHamburgerMenuOpen(
                isPortraitPresentationActive);
        }
    }

    private void ScheduleHamburgerMenuOpen(
        bool usePortrait)
    {
        Coroutine existingCoroutine =
            usePortrait
                ? portraitHamburgerMenuShowCoroutine
                : hamburgerMenuShowCoroutine;
        if (existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        Coroutine showCoroutine = StartCoroutine(
            ShowHamburgerMenuAfterPointerDispatch(
                usePortrait));
        if (usePortrait)
        {
            portraitHamburgerMenuShowCoroutine =
                showCoroutine;
        }
        else
        {
            hamburgerMenuShowCoroutine =
                showCoroutine;
        }
    }

    private IEnumerator ShowHamburgerMenuAfterPointerDispatch(
        bool usePortrait)
    {
        // Enabling a hierarchy of Selectables from inside Button.OnClick can
        // corrupt Unity's internal Selectable registry. Resume next frame,
        // after the EventSystem has finished dispatching the pointer event.
        yield return null;

        if (usePortrait)
        {
            portraitHamburgerMenuShowCoroutine = null;
        }
        else
        {
            hamburgerMenuShowCoroutine = null;
        }

        if (!isHamburgerMenuOpen ||
            usePortrait != isPortraitPresentationActive)
        {
            yield break;
        }

        ShowHamburgerMenuAnimated(usePortrait);
    }

    private void CancelHamburgerMenuOpenCoroutines()
    {
        if (hamburgerMenuShowCoroutine != null)
        {
            StopCoroutine(hamburgerMenuShowCoroutine);
            hamburgerMenuShowCoroutine = null;
        }

        if (portraitHamburgerMenuShowCoroutine != null)
        {
            StopCoroutine(
                portraitHamburgerMenuShowCoroutine);
            portraitHamburgerMenuShowCoroutine = null;
        }
    }

    private void ShowHamburgerMenuAnimated(bool usePortrait)
    {
        GameObject panel = usePortrait
            ? portraitHamburgerMenuPanel
            : hamburgerMenuPanel;
        CanvasGroup canvasGroup = usePortrait
            ? portraitHamburgerMenuCanvasGroup
            : hamburgerMenuCanvasGroup;
        Button openButton = usePortrait
            ? portraitHamburgerMenuButton
            : hamburgerMenuButton;
        Button closeButton = usePortrait
            ? portraitHamburgerMenuDownButton
            : hamburgerMenuDownButton;

        if (panel == null || canvasGroup == null)
        {
            return;
        }

        KillHamburgerMenuTween(usePortrait);
        panel.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        SetHamburgerToggleButtonState(
            openButton,
            closeButton,
            true);

        Tween fadeTween = canvasGroup
            .DOFade(
                1f,
                Mathf.Max(
                    0.01f,
                    hamburgerMenuFadeDuration))
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
        SetHamburgerMenuTween(usePortrait, fadeTween);
        fadeTween.OnComplete(() =>
        {
            canvasGroup.alpha = 1f;
            SetHamburgerMenuTween(usePortrait, null);
        });
    }

    private void HideHamburgerMenuAnimated(bool usePortrait)
    {
        GameObject panel = usePortrait
            ? portraitHamburgerMenuPanel
            : hamburgerMenuPanel;
        CanvasGroup canvasGroup = usePortrait
            ? portraitHamburgerMenuCanvasGroup
            : hamburgerMenuCanvasGroup;
        Button openButton = usePortrait
            ? portraitHamburgerMenuButton
            : hamburgerMenuButton;
        Button closeButton = usePortrait
            ? portraitHamburgerMenuDownButton
            : hamburgerMenuDownButton;

        if (panel == null || canvasGroup == null)
        {
            SetHamburgerToggleButtonState(
                openButton,
                closeButton,
                false);
            return;
        }

        KillHamburgerMenuTween(usePortrait);
        // Keep the buttons in their normal visual state while the menu fades.
        // Setting interactable to false makes their disabled sprites flash
        // before the panel disappears. Raycasts are still blocked immediately.
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false;

        Tween fadeTween = canvasGroup
            .DOFade(
                0f,
                Mathf.Max(
                    0.01f,
                    hamburgerMenuFadeDuration))
            .SetEase(Ease.InQuad)
            .SetUpdate(true);
        SetHamburgerMenuTween(usePortrait, fadeTween);
        fadeTween.OnComplete(() =>
        {
            canvasGroup.alpha = 0f;
            panel.SetActive(false);
            SetHamburgerToggleButtonState(
                openButton,
                closeButton,
                false);
            SetHamburgerMenuTween(usePortrait, null);
        });
    }

    private void SetHamburgerMenuImmediate(
        bool usePortrait,
        bool isOpen)
    {
        CanvasGroup canvasGroup = usePortrait
            ? portraitHamburgerMenuCanvasGroup
            : hamburgerMenuCanvasGroup;
        Button openButton = usePortrait
            ? portraitHamburgerMenuButton
            : hamburgerMenuButton;
        Button closeButton = usePortrait
            ? portraitHamburgerMenuDownButton
            : hamburgerMenuDownButton;

        KillHamburgerMenuTween(usePortrait);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isOpen ? 1f : 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = isOpen;
        }

        GameObject panel = usePortrait
            ? portraitHamburgerMenuPanel
            : hamburgerMenuPanel;
        if (panel != null)
        {
            panel.SetActive(isOpen);
        }

        SetHamburgerToggleButtonState(
            openButton,
            closeButton,
            isOpen);
    }

    private static void SetHamburgerToggleButtonState(
        Button openButton,
        Button closeButton,
        bool isOpen)
    {
        if (openButton != null)
        {
            openButton.gameObject.SetActive(!isOpen);
            openButton.interactable = true;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(isOpen);
            closeButton.interactable = true;
        }
    }

    private void KillHamburgerMenuTween(bool usePortrait)
    {
        Tween tween = usePortrait
            ? portraitHamburgerMenuTween
            : hamburgerMenuTween;
        tween?.Kill();
        SetHamburgerMenuTween(usePortrait, null);
    }

    private void SetHamburgerMenuTween(
        bool usePortrait,
        Tween tween)
    {
        if (usePortrait)
        {
            portraitHamburgerMenuTween = tween;
        }
        else
        {
            hamburgerMenuTween = tween;
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

    private void OpenSoundPanel()
    {
        if (soundPanel == null)
        {
            Debug.LogError(
                "[UIManager] Cannot open Sound Panel because it is not assigned.");
            return;
        }

        CloseAutoPlayPanel();
        ResetHamburgerMenu();
        CloseInfoPage();
        CloseGuidePage();
        soundPanel.SetActive(true);
    }

    private void CloseSoundPanel()
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(false);
        }
    }

    private void OnHomeButtonClicked()
    {
        if (popupManager == null)
        {
            Debug.LogError(
                "[UIManager] Cannot show the exit confirmation because " +
                "PopupManager is not assigned.");
            return;
        }

        popupManager.ShowExitGamePopup();
    }

    private void OnMoreGamesButtonClicked()
    {
        jsFunctCalls?.SendCustomMessage("MoreGames");
    }

    private void OnExpand()
    {
        isExpanded = true;
        jsFunctCalls?.RequestExpandGame();
        SetExpandShrinkButtons(isExpanded);
    }

    private void OnShrink()
    {
        isExpanded = false;
        jsFunctCalls?.RequestShrinkGame();
        SetExpandShrinkButtons(isExpanded);
    }

    public void OnFullscreenChanged(string isFullscreen)
    {
        bool browserIsExpanded =
            string.Equals(
                isFullscreen,
                "1",
                System.StringComparison.Ordinal);
        SetExpandShrinkButtons(browserIsExpanded);
    }

    private void SetExpandShrinkButtons(bool expanded)
    {
        isExpanded = expanded;

        SetActionButtonState(
            expandButton,
            !isExpanded,
            true);
        SetActionButtonState(
            shrinkButton,
            isExpanded,
            true);
        SetActionButtonState(
            portraitExpandButton,
            !isExpanded,
            true);
        SetActionButtonState(
            portraitShrinkButton,
            isExpanded,
            true);
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
        bool showAutoPlayStop =
            !isUltraUnlocked &&
            gameManager != null &&
            gameManager.isAutoPlaying;
        bool showStopButton =
            !isUltraUnlocked &&
            !showAutoPlayStop &&
            !isWaitingForStoppedAutoPlayRound &&
            gameManager != null &&
            gameManager.IsSpinning();
        bool showSpinButton =
            !showAutoPlayStop &&
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

        if (autoPlayStopButton != null)
        {
            autoPlayStopButton.gameObject.SetActive(showAutoPlayStop);
            autoPlayStopButton.interactable = showAutoPlayStop;
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

        SetActionButtonState(
            portraitSpinButton,
            showSpinButton,
            true);
        SetActionButtonState(
            portraitStopButton,
            showStopButton,
            true);
        SetActionButtonState(
            portraitAutoPlayStopButton,
            showAutoPlayStop,
            showAutoPlayStop);
        SetActionButtonState(
            portraitUltraStartButton,
            showUltraStart,
            true);
        SetActionButtonState(
            portraitUltraTakeButton,
            showUltraTake,
            true);
    }

    private static void SetActionButtonState(
        Button button,
        bool isVisible,
        bool isInteractable)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(isVisible);
        button.interactable = isInteractable;
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
        SetSpinModeButtonState(
            portraitNormalSpinButton,
            selectedMode == SpinSpeed.Normal,
            canChangeMode);
        SetSpinModeButtonState(
            portraitFastSpinButton,
            selectedMode == SpinSpeed.FastSpin,
            canChangeMode);
        SetSpinModeButtonState(
            portraitSkipSpinButton,
            selectedMode == SpinSpeed.SkipSpin,
            canChangeMode);
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

        if (portraitWinLinesCountText != null)
        {
            portraitWinLinesCountText.text =
                gameManager.GetDisplayedPaylineCount().ToString();
        }

        if (portraitGoodLuckText != null)
        {
            portraitGoodLuckText.text = "GOOD LUCK !";
            portraitGoodLuckText.gameObject.SetActive(!hasWin);
        }

        if (portraitWonLabelText != null)
        {
            portraitWonLabelText.text = "WON";
            portraitWonLabelText.gameObject.SetActive(hasWin);
        }

        if (portraitWinAmountText != null)
        {
            if (hasWin)
            {
                portraitWinAmountText.text =
                    winAmount.ToString("0.00");
            }

            portraitWinAmountText.gameObject.SetActive(hasWin);
        }

        if (portraitBalanceAmountText != null)
        {
            portraitBalanceAmountText.text =
                $"BALANCE:  {gameManager.GetDisplayedBalance():0.00}";
        }

        if (portraitBetAmountText != null)
        {
            portraitBetAmountText.text =
                gameManager.GetDisplayedTotalBetAmount()
                    .ToString("0.00");
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

        if (portraitIncreaseBetButton != null)
        {
            portraitIncreaseBetButton.interactable =
                gameManager != null &&
                gameManager.CanIncreaseBet();
        }

        if (portraitDecreaseBetButton != null)
        {
            portraitDecreaseBetButton.interactable =
                gameManager != null &&
                gameManager.CanDecreaseBet();
        }
    }

    private void RefreshAutoPlayControls()
    {
        bool isAutoPlaying = gameManager != null && gameManager.isAutoPlaying;
        bool isUltraUnlocked = gameManager != null && gameManager.IsUltraSlotUnlocked();
        bool showAutoPlayStop = isAutoPlaying && !isUltraUnlocked;
        bool canStartAutoPlay = gameManager != null && gameManager.CanStartAutoPlay();
        bool shouldShowAutoPlayPanel = isAutoPlayPanelOpen &&
                                       !isAutoPlaying &&
                                       !isUltraUnlocked;

        AutoPlayPanelAnimationState activeAnimation =
            isPortraitPresentationActive
                ? portraitAutoPlayPanelAnimation
                : landscapeAutoPlayPanelAnimation;
        AutoPlayPanelAnimationState hiddenAnimation =
            isPortraitPresentationActive
                ? landscapeAutoPlayPanelAnimation
                : portraitAutoPlayPanelAnimation;

        SetAutoPlayPanelImmediate(hiddenAnimation, false);

        if (activeAnimation != null)
        {
            if (shouldShowAutoPlayPanel)
            {
                if (!activeAnimation.Panel.activeSelf ||
                    activeAnimation.IsClosing)
                {
                    ShowAutoPlayPanelAnimated(activeAnimation);
                }
            }
            else if (activeAnimation.Panel.activeSelf &&
                     !activeAnimation.IsClosing)
            {
                HideAutoPlayPanelAnimated(activeAnimation);
            }
        }

        bool canUseAutoPlayChoices = canStartAutoPlay &&
                                     shouldShowAutoPlayPanel &&
                                     (activeAnimation == null ||
                                      !activeAnimation.IsClosing);
        SetButtonInteractable(autoPlay10Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay50Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay100Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay200Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlay500Button, canUseAutoPlayChoices);
        SetButtonInteractable(autoPlayInfiniteButton, canUseAutoPlayChoices);
        SetButtonInteractable(
            portraitAutoPlay10Button,
            canUseAutoPlayChoices);
        SetButtonInteractable(
            portraitAutoPlay50Button,
            canUseAutoPlayChoices);
        SetButtonInteractable(
            portraitAutoPlay100Button,
            canUseAutoPlayChoices);
        SetButtonInteractable(
            portraitAutoPlay200Button,
            canUseAutoPlayChoices);
        SetButtonInteractable(
            portraitAutoPlay500Button,
            canUseAutoPlayChoices);
        SetButtonInteractable(
            portraitAutoPlayInfiniteButton,
            canUseAutoPlayChoices);

        if (autoPlayStopButton != null)
        {
            autoPlayStopButton.gameObject.SetActive(showAutoPlayStop);
            autoPlayStopButton.interactable = showAutoPlayStop;
        }

        if (portraitAutoPlayStopButton != null)
        {
            portraitAutoPlayStopButton.gameObject.SetActive(
                showAutoPlayStop);
            portraitAutoPlayStopButton.interactable =
                showAutoPlayStop;
        }

        RefreshAutoPlayCountText(
            autoPlayCountText,
            autoPlayStopButton,
            isAutoPlaying);
        RefreshAutoPlayCountText(
            portraitAutoPlayCountText,
            portraitAutoPlayStopButton,
            isAutoPlaying);
    }

    private void RefreshAutoPlayCountText(
        TMP_Text countText,
        Button owningStopButton,
        bool isAutoPlaying)
    {
        if (countText == null)
        {
            return;
        }

        // If the text is a child of the stop button, control it
        // independently. If both components share one GameObject, the
        // button visibility above controls it.
        if (owningStopButton == null ||
            countText.gameObject != owningStopButton.gameObject)
        {
            countText.gameObject.SetActive(isAutoPlaying);
        }

        if (!isAutoPlaying)
        {
            return;
        }

        if (gameManager.autoPlayRemainingRounds ==
            GameManager.InfiniteAutoPlayRounds)
        {
            countText.text = "∞";
        }
        else
        {
            int spinsRemainingAfterCurrent = Mathf.Max(
                0,
                gameManager.autoPlayRemainingRounds - 1);
            countText.text = spinsRemainingAfterCurrent.ToString();
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
