using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PopupManager : MonoBehaviour
{
    private const string DefaultDisconnectionMessage =
        "Game disconnected due to a network error. Please relaunch the game.";
    private const string DefaultInsufficientBalanceMessage =
        "Insufficient balance. Please add funds to continue.";
    private const string DefaultInitializationFailureMessage =
        "Game is not Initialized";

    [Header("Scatter Win Popup")]
    [Tooltip("Assign the complete ScatterWinPanel RectTransform.")]
    [SerializeField] private RectTransform scatterWinPanel;
    [Tooltip("Assign the TotalWin TMP text inside ScatterWinPanel.")]
    [SerializeField] private TMP_Text scatterTotalWinText;

    [Header("Ultra Wheel Start Popup")]
    [Tooltip(
        "Assign the number-free UltraWheelPanel shown before the first Ultra Start.")]
    [SerializeField] private RectTransform ultraWheelStartPanel;

    [Header("Ultra Wheel Reward Popup")]
    [Tooltip("Assign the complete UltraWheelRewardPanel RectTransform.")]
    [SerializeField] private RectTransform ultraWheelRewardPanel;
    [Tooltip("Assign the TotalWin TMP text inside UltraWheelRewardPanel.")]
    [SerializeField] private TMP_Text ultraTotalWinText;
    [Tooltip("Assign the award text below Icon1 (green wheel).")]
    [SerializeField] private TMP_Text greenWheelWinText;
    [Tooltip("Assign the award text below Icon2 (blue wheel).")]
    [SerializeField] private TMP_Text blueWheelWinText;
    [Tooltip("Assign the award text below Icon3 (red wheel).")]
    [SerializeField] private TMP_Text redWheelWinText;

    [Header("Popup Actions")]
    [Tooltip(
        "Used when a critical error or confirmed exit closes the game. " +
        "Found automatically when left empty.")]
    [SerializeField] private GameManager gameManager;

    [Header("Reusable Error Popup")]
    [Tooltip("Complete error popup GameObject.")]
    [SerializeField] private GameObject errorPopup;
    [Tooltip("Message TMP text inside the error popup.")]
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [Tooltip("Cancel button used to acknowledge the error.")]
    [SerializeField] private Button errorOkButton;

    [Header("Error Popup Animation")]
    [Tooltip("Time used to grow the error popup from zero to its overshoot size.")]
    [SerializeField, Min(0.01f)] private float errorPopupGrowDuration = 0.28f;
    [Tooltip("Scale reached before the error popup settles to its authored size.")]
    [SerializeField, Range(1f, 1.3f)] private float errorPopupOvershootScale = 1.08f;
    [Tooltip("Time used to settle the error popup from overshoot to its authored size.")]
    [SerializeField, Min(0.01f)] private float errorPopupSettleDuration = 0.12f;
    [Tooltip("Time used to shrink the error popup to zero before deactivation.")]
    [SerializeField, Min(0.01f)] private float errorPopupCloseDuration = 0.25f;

    [Header("Exit Game Confirmation Popup")]
    [Tooltip("Complete exit confirmation popup GameObject.")]
    [SerializeField] private GameObject exitGamePopup;
    [Tooltip("RectTransform animated when the exit popup opens and closes.")]
    [SerializeField] private RectTransform exitGamePopupRect;
    [SerializeField] private Button exitGameYesButton;
    [SerializeField] private Button exitGameNoButton;

    [Header("Reward Popup Animation")]
    [Tooltip("Time used to grow the panel from scale 0 to its authored scale.")]
    [SerializeField, Min(0.01f)] private float scaleInDuration = 0.4f;
    [Tooltip("Time used to count TotalWin from 0 to the server result.")]
    [SerializeField, Min(0.01f)] private float totalWinCountDuration = 1.2f;
    [Tooltip("Time the completed amount remains visible before the panel closes.")]
    [SerializeField, Min(0f)] private float completedWinHoldDuration = 1f;
    [Tooltip("Time used to shrink the panel back to scale 0.")]
    [SerializeField, Min(0.01f)] private float scaleOutDuration = 0.4f;

    private Vector3 scatterWinPanelNormalScale = Vector3.one;
    private Vector3 ultraWheelStartPanelNormalScale = Vector3.one;
    private Vector3 ultraWheelRewardPanelNormalScale = Vector3.one;
    private Sequence scatterWinSequence;
    private Sequence ultraStartSequence;
    private Sequence ultraWinSequence;
    private GameObject currentActivePopup;
    private bool isErrorCritical;
    private bool reportedErrorPopupVisible;
    private RectTransform errorPopupRect;
    private Vector3 errorPopupNormalScale = Vector3.one;
    private Tween currentPopupTween;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>(
                FindObjectsInactive.Include);
        }

        CacheScatterWinPanelScale();
        CacheUltraWheelStartPanelScale();
        CacheUltraWheelRewardPanelScale();
        ResolveErrorPopupRect();
        ResolveExitGamePopupRect();
        CachePanelScale(errorPopupRect, ref errorPopupNormalScale);
        SetupButtons();
        HideAllPopups();
    }

    private void OnEnable()
    {
        SetupButtons();
    }

    private void OnDisable()
    {
        if (errorOkButton != null)
        {
            errorOkButton.onClick.RemoveListener(OnErrorOkClicked);
            errorOkButton.interactable = true;
        }

        if (exitGameYesButton != null)
        {
            exitGameYesButton.onClick.RemoveListener(
                OnExitGameYesClicked);
            exitGameYesButton.interactable = true;
        }

        if (exitGameNoButton != null)
        {
            exitGameNoButton.onClick.RemoveListener(
                OnExitGameNoClicked);
            exitGameNoButton.interactable = true;
        }

        KillScatterWinSequence();
        KillUltraStartSequence();
        KillUltraWinSequence();
        KillCurrentPopupTween();
    }

    private void SetupButtons()
    {
        if (errorOkButton != null)
        {
            errorOkButton.onClick.RemoveListener(OnErrorOkClicked);
            errorOkButton.onClick.AddListener(OnErrorOkClicked);
        }

        if (exitGameYesButton != null)
        {
            exitGameYesButton.onClick.RemoveListener(
                OnExitGameYesClicked);
            exitGameYesButton.onClick.AddListener(
                OnExitGameYesClicked);
        }

        if (exitGameNoButton != null)
        {
            exitGameNoButton.onClick.RemoveListener(
                OnExitGameNoClicked);
            exitGameNoButton.onClick.AddListener(
                OnExitGameNoClicked);
        }
    }

    internal void HideAllPopups()
    {
        HideScatterWinImmediate();
        HideUltraStartImmediate();
        HideUltraWinImmediate();
        HideErrorPopupImmediate();
        HideExitGamePopupImmediate();
    }

    internal void ShowErrorPopup(
        string message,
        bool isCritical)
    {
        if (errorPopup == null)
        {
            return;
        }

        ResolveErrorPopupRect();
        CachePanelScale(errorPopupRect, ref errorPopupNormalScale);

        string popupMessage = message ?? string.Empty;

        if (currentActivePopup == errorPopup &&
            errorPopup.activeSelf &&
            isErrorCritical == isCritical &&
            (errorMessageText == null ||
             errorMessageText.text == popupMessage))
        {
            return;
        }

        CloseCurrentPopup();

        if (errorMessageText != null)
        {
            errorMessageText.text = popupMessage;
        }

        if (errorOkButton != null)
        {
            errorOkButton.interactable = true;
        }

        isErrorCritical = isCritical;
        currentActivePopup = errorPopup;
        errorPopup.SetActive(true);
        SetErrorPopupVisibilityState(true);
        AudioController.Instance?.PlayPopup();
        AnimateErrorPopupOpen();
    }

    internal void ShowExitGamePopup()
    {
        if (exitGamePopup == null)
        {
            return;
        }

        if (currentActivePopup == exitGamePopup &&
            exitGamePopup.activeSelf)
        {
            return;
        }

        CloseCurrentPopup();

        SetExitGameButtonsInteractable(true);
        currentActivePopup = exitGamePopup;
        exitGamePopup.SetActive(true);
        AudioController.Instance?.PlayPopup();
        AnimatePopupOpen(exitGamePopupRect, 0.3f);
    }

    internal void CloseCurrentPopup()
    {
        KillCurrentPopupTween();
        bool isClosingErrorPopup = currentActivePopup == errorPopup;

        if (currentActivePopup != null)
        {
            RectTransform activePopupRect =
                currentActivePopup == errorPopup
                    ? errorPopupRect
                    : currentActivePopup == exitGamePopup
                        ? exitGamePopupRect
                        : currentActivePopup.GetComponent<RectTransform>();

            if (activePopupRect != null)
            {
                activePopupRect.localScale =
                    GetPopupNormalScale(activePopupRect);
            }

            currentActivePopup.SetActive(false);
        }

        currentActivePopup = null;
        isErrorCritical = false;

        if (isClosingErrorPopup)
        {
            SetErrorPopupVisibilityState(false);
        }

        if (errorOkButton != null)
        {
            errorOkButton.interactable = true;
        }

        SetExitGameButtonsInteractable(true);
    }

    private void OnErrorOkClicked()
    {
        if (currentActivePopup != errorPopup ||
            errorPopup == null)
        {
            return;
        }

        AudioController.Instance?.PlayUiButton();
        GameObject closingPopup = currentActivePopup;
        bool shouldExitGame = isErrorCritical;

        if (errorOkButton != null)
        {
            errorOkButton.interactable = false;
        }

        AnimateErrorPopupClose(
            () =>
            {
                if (closingPopup != null)
                {
                    closingPopup.SetActive(false);
                }

                if (errorPopupRect != null)
                {
                    errorPopupRect.localScale =
                        errorPopupNormalScale;
                }

                if (currentActivePopup == closingPopup)
                {
                    currentActivePopup = null;
                }

                isErrorCritical = false;
                SetErrorPopupVisibilityState(false);

                if (errorOkButton != null)
                {
                    errorOkButton.interactable = true;
                }

                if (shouldExitGame)
                {
                    ExitGame();
                }
            });
    }

    private void AnimateErrorPopupOpen()
    {
        KillCurrentPopupTween();

        if (errorPopupRect == null)
        {
            return;
        }

        Vector3 targetScale = errorPopupNormalScale;
        Vector3 overshootScale =
            targetScale * Mathf.Max(1f, errorPopupOvershootScale);
        errorPopupRect.localScale = Vector3.zero;

        Sequence openSequence = DOTween.Sequence().SetUpdate(true);
        openSequence.Append(
            errorPopupRect
                .DOScale(
                    overshootScale,
                    Mathf.Max(0.01f, errorPopupGrowDuration))
                .SetEase(Ease.OutCubic));
        openSequence.Append(
            errorPopupRect
                .DOScale(
                    targetScale,
                    Mathf.Max(0.01f, errorPopupSettleDuration))
                .SetEase(Ease.OutQuad));
        openSequence.OnComplete(() =>
        {
            currentPopupTween = null;
            if (errorPopupRect != null)
            {
                errorPopupRect.localScale = targetScale;
            }
        });

        currentPopupTween = openSequence;
    }

    private void AnimateErrorPopupClose(Action onComplete)
    {
        KillCurrentPopupTween();

        if (errorPopupRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        currentPopupTween = errorPopupRect
            .DOScale(
                Vector3.zero,
                Mathf.Max(0.01f, errorPopupCloseDuration))
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                currentPopupTween = null;
                onComplete?.Invoke();
            });
    }

    internal event Action<bool> ErrorPopupVisibilityChanged;

    internal bool IsErrorPopupOpen()
    {
        return currentActivePopup == errorPopup &&
               errorPopup != null &&
               errorPopup.activeSelf;
    }

    private void SetErrorPopupVisibilityState(bool isVisible)
    {
        if (reportedErrorPopupVisible == isVisible)
        {
            return;
        }

        reportedErrorPopupVisible = isVisible;
        ErrorPopupVisibilityChanged?.Invoke(isVisible);
    }

    private void OnExitGameYesClicked()
    {
        CloseExitGamePopup(true);
    }

    private void OnExitGameNoClicked()
    {
        CloseExitGamePopup(false);
    }

    private void CloseExitGamePopup(bool shouldExitGame)
    {
        if (currentActivePopup != exitGamePopup ||
            exitGamePopup == null)
        {
            return;
        }

        AudioController.Instance?.PlayUiButton();
        GameObject closingPopup = currentActivePopup;
        SetExitGameButtonsInteractable(false);

        AnimatePopupClose(
            exitGamePopupRect,
            0.2f,
            () =>
            {
                if (closingPopup != null)
                {
                    closingPopup.SetActive(false);
                }

                if (exitGamePopupRect != null)
                {
                    exitGamePopupRect.localScale = Vector3.one;
                }

                if (currentActivePopup == closingPopup)
                {
                    currentActivePopup = null;
                }

                SetExitGameButtonsInteractable(true);

                if (shouldExitGame)
                {
                    ExitGame();
                }
            });
    }

    private void AnimatePopupOpen(RectTransform popupRect)
    {
        AnimatePopupOpen(
            popupRect,
            Mathf.Max(0.01f, scaleInDuration));
    }

    private void AnimatePopupOpen(
        RectTransform popupRect,
        float duration)
    {
        KillCurrentPopupTween();

        if (popupRect == null)
        {
            return;
        }

        Vector3 targetScale = GetPopupNormalScale(popupRect);
        popupRect.localScale = Vector3.zero;
        currentPopupTween = popupRect
            .DOScale(
                targetScale,
                Mathf.Max(0.01f, duration))
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                currentPopupTween = null;

                if (popupRect != null)
                {
                    popupRect.localScale = targetScale;
                }
            });
    }

    private void AnimatePopupClose(
        RectTransform popupRect,
        Action onComplete)
    {
        AnimatePopupClose(
            popupRect,
            Mathf.Max(0.01f, scaleOutDuration),
            onComplete);
    }

    private void AnimatePopupClose(
        RectTransform popupRect,
        float duration,
        Action onComplete)
    {
        KillCurrentPopupTween();

        if (popupRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        currentPopupTween = popupRect
            .DOScale(
                Vector3.zero,
                Mathf.Max(0.01f, duration))
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                currentPopupTween = null;
                onComplete?.Invoke();
            });
    }

    internal bool ShowScatterWin(double totalWin, Action onComplete)
    {
        if (scatterWinPanel == null || scatterTotalWinText == null)
        {
            Debug.LogError(
                "[PopupManager] Assign Scatter Win Panel and Scatter Total Win Text.");
            return false;
        }

        CacheScatterWinPanelScale();
        KillScatterWinSequence();
        AudioController.Instance?.PlayPopup();

        double sanitizedTotalWin = Math.Max(0d, totalWin);
        int totalWinDecimalPlaces =
            ServerAmountFormatter.GetDecimalPlaces(sanitizedTotalWin);
        double displayedTotalWin = 0d;

        scatterWinPanel.gameObject.SetActive(true);
        scatterWinPanel.localScale = Vector3.zero;
        scatterTotalWinText.gameObject.SetActive(true);
        scatterTotalWinText.text =
            ServerAmountFormatter.Format(0d, totalWinDecimalPlaces);

        scatterWinSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                scatterWinPanel
                    .DOScale(
                        scatterWinPanelNormalScale,
                        Mathf.Max(0.01f, scaleInDuration))
                    .SetEase(Ease.OutBack))
            .Append(
                DOTween.To(
                        () => displayedTotalWin,
                        value =>
                        {
                            displayedTotalWin = value;
                            scatterTotalWinText.text =
                                ServerAmountFormatter.Format(
                                    value,
                                    totalWinDecimalPlaces);
                        },
                        sanitizedTotalWin,
                        Mathf.Max(0.01f, totalWinCountDuration))
                    .SetEase(Ease.OutCubic))
            .AppendCallback(
                () =>
                    scatterTotalWinText.text =
                        ServerAmountFormatter.Format(
                            sanitizedTotalWin,
                            totalWinDecimalPlaces))
            .AppendInterval(Mathf.Max(0f, completedWinHoldDuration))
            .Append(
                scatterWinPanel
                    .DOScale(
                        Vector3.zero,
                        Mathf.Max(0.01f, scaleOutDuration))
                    .SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                scatterWinSequence = null;
                scatterWinPanel.localScale = Vector3.zero;
                scatterWinPanel.gameObject.SetActive(false);
                onComplete?.Invoke();
            });

        return true;
    }

    internal bool ShowUltraStart(Action onShown)
    {
        if (ultraWheelStartPanel == null)
        {
            Debug.LogError(
                "[PopupManager] Assign the Ultra Wheel Start Panel.");
            return false;
        }

        CacheUltraWheelStartPanelScale();
        KillUltraStartSequence();
        AudioController.Instance?.PlayPopup();

        ultraWheelStartPanel.gameObject.SetActive(true);
        ultraWheelStartPanel.localScale = Vector3.zero;

        ultraStartSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                ultraWheelStartPanel
                    .DOScale(
                        ultraWheelStartPanelNormalScale,
                        Mathf.Max(0.01f, scaleInDuration))
                    .SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                ultraStartSequence = null;
                ultraWheelStartPanel.localScale =
                    ultraWheelStartPanelNormalScale;
                onShown?.Invoke();
            });

        return true;
    }

    internal bool ShowUltraWin(
        double? greenWheelWin,
        double? blueWheelWin,
        double? redWheelWin,
        double totalWin,
        Action onComplete)
    {
        if (ultraWheelRewardPanel == null ||
            ultraTotalWinText == null ||
            greenWheelWinText == null ||
            blueWheelWinText == null ||
            redWheelWinText == null)
        {
            Debug.LogError(
                "[PopupManager] Assign the Ultra Wheel Reward Panel, Total Win Text, " +
                "and the green, blue, and red wheel win texts.");
            return false;
        }

        CacheUltraWheelRewardPanelScale();
        KillUltraWinSequence();
        AudioController.Instance?.PlayPopup();

        double? sanitizedGreenWin = greenWheelWin.HasValue
            ? Math.Max(0d, greenWheelWin.Value)
            : null;
        double? sanitizedBlueWin = blueWheelWin.HasValue
            ? Math.Max(0d, blueWheelWin.Value)
            : null;
        double? sanitizedRedWin = redWheelWin.HasValue
            ? Math.Max(0d, redWheelWin.Value)
            : null;
        double sanitizedTotalWin = Math.Max(0d, totalWin);
        int totalWinDecimalPlaces =
            ServerAmountFormatter.GetDecimalPlaces(sanitizedTotalWin);
        double displayedTotalWin = 0d;

        ultraWheelRewardPanel.gameObject.SetActive(true);
        ultraWheelRewardPanel.localScale = Vector3.zero;

        greenWheelWinText.gameObject.SetActive(true);
        blueWheelWinText.gameObject.SetActive(true);
        redWheelWinText.gameObject.SetActive(true);
        ultraTotalWinText.gameObject.SetActive(true);

        // Individual wheel results must load immediately. Only Total Win counts up.
        greenWheelWinText.text = FormatOptionalServerAmount(sanitizedGreenWin);
        blueWheelWinText.text = FormatOptionalServerAmount(sanitizedBlueWin);
        redWheelWinText.text = FormatOptionalServerAmount(sanitizedRedWin);
        ultraTotalWinText.text =
            ServerAmountFormatter.Format(0d, totalWinDecimalPlaces);

        ultraWinSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                ultraWheelRewardPanel
                    .DOScale(
                        ultraWheelRewardPanelNormalScale,
                        Mathf.Max(0.01f, scaleInDuration))
                    .SetEase(Ease.OutBack))
            .Append(
                DOTween.To(
                        () => displayedTotalWin,
                        value =>
                        {
                            displayedTotalWin = value;
                            ultraTotalWinText.text =
                                ServerAmountFormatter.Format(
                                    value,
                                    totalWinDecimalPlaces);
                        },
                        sanitizedTotalWin,
                        Mathf.Max(0.01f, totalWinCountDuration))
                    .SetEase(Ease.OutCubic))
            .AppendCallback(
                () =>
                    ultraTotalWinText.text =
                        ServerAmountFormatter.Format(
                            sanitizedTotalWin,
                            totalWinDecimalPlaces))
            .OnComplete(() =>
            {
                ultraWinSequence = null;
                ultraWheelRewardPanel.localScale =
                    ultraWheelRewardPanelNormalScale;
                onComplete?.Invoke();
            });

        return true;
    }

    internal bool ShowDisconnectionPopup()
    {
        return ShowDisconnectionPopup(
            DefaultDisconnectionMessage);
    }

    internal bool ShowDisconnectionPopup(string message)
    {
        if (errorPopup == null)
        {
            Debug.LogError(
                "[PopupManager] Assign the reusable Error Popup reference.");
            return false;
        }

        ShowErrorPopup(
            string.IsNullOrWhiteSpace(message)
                ? DefaultDisconnectionMessage
                : message,
            true);
        return true;
    }

    internal bool ShowInsufficientBalancePopup()
    {
        return ShowInsufficientBalancePopup(
            DefaultInsufficientBalanceMessage);
    }

    internal bool ShowInsufficientBalancePopup(string message)
    {
        if (errorPopup == null)
        {
            Debug.LogError(
                "[PopupManager] Assign the reusable Error Popup reference.");
            return false;
        }

        ShowErrorPopup(
            string.IsNullOrWhiteSpace(message)
                ? DefaultInsufficientBalanceMessage
                : message,
            false);
        return true;
    }

    internal bool ShowInitializationFailurePopup(string message = null)
    {
        if (errorPopup == null)
        {
            Debug.LogError(
                "[PopupManager] Assign the reusable Error Popup reference.");
            return false;
        }

        ShowErrorPopup(
            string.IsNullOrWhiteSpace(message)
                ? DefaultInitializationFailureMessage
                : message,
            true);
        return true;
    }

    internal bool ShowInsufficientFundsError()
    {
        return ShowInsufficientBalancePopup();
    }

    internal void CloseDisconnectionPopup()
    {
        if (currentActivePopup == errorPopup && isErrorCritical)
        {
            CloseCurrentPopup();
        }
    }

    internal void CloseInsufficientBalancePopup()
    {
        if (currentActivePopup == errorPopup && !isErrorCritical)
        {
            CloseCurrentPopup();
        }
    }

    internal void HideScatterWinImmediate()
    {
        KillScatterWinSequence();

        if (scatterTotalWinText != null)
        {
            scatterTotalWinText.text = ServerAmountFormatter.Format(0d);
        }

        if (scatterWinPanel != null)
        {
            scatterWinPanel.localScale = Vector3.zero;
            scatterWinPanel.gameObject.SetActive(false);
        }
    }

    internal void HideUltraWinImmediate()
    {
        KillUltraWinSequence();

        if (greenWheelWinText != null)
        {
            greenWheelWinText.text = string.Empty;
        }
        if (blueWheelWinText != null)
        {
            blueWheelWinText.text = string.Empty;
        }
        if (redWheelWinText != null)
        {
            redWheelWinText.text = string.Empty;
        }
        if (ultraTotalWinText != null)
        {
            ultraTotalWinText.text = ServerAmountFormatter.Format(0d);
        }

        if (ultraWheelRewardPanel != null)
        {
            ultraWheelRewardPanel.localScale = Vector3.zero;
            ultraWheelRewardPanel.gameObject.SetActive(false);
        }
    }

    internal void HideUltraStartImmediate()
    {
        KillUltraStartSequence();

        if (ultraWheelStartPanel != null)
        {
            ultraWheelStartPanel.localScale =
                ultraWheelStartPanelNormalScale;
            ultraWheelStartPanel.gameObject.SetActive(false);
        }
    }

    private void HideErrorPopupImmediate()
    {
        KillCurrentPopupTween();

        if (errorPopupRect != null)
        {
            errorPopupRect.localScale = errorPopupNormalScale;
        }

        if (errorPopup != null)
        {
            errorPopup.SetActive(false);
        }

        currentActivePopup = null;
        isErrorCritical = false;
        SetErrorPopupVisibilityState(false);

        if (errorOkButton != null)
        {
            errorOkButton.interactable = true;
        }
    }

    private void HideExitGamePopupImmediate()
    {
        KillCurrentPopupTween();

        if (exitGamePopupRect != null)
        {
            exitGamePopupRect.localScale = Vector3.one;
        }

        if (exitGamePopup != null)
        {
            exitGamePopup.SetActive(false);
        }

        if (currentActivePopup == exitGamePopup)
        {
            currentActivePopup = null;
        }

        SetExitGameButtonsInteractable(true);
    }

    private void ResolveErrorPopupRect()
    {
        if (errorPopupRect == null && errorPopup != null)
        {
            errorPopupRect =
                errorPopup.GetComponent<RectTransform>();
        }
    }

    private void ResolveExitGamePopupRect()
    {
        if (exitGamePopupRect == null && exitGamePopup != null)
        {
            exitGamePopupRect =
                exitGamePopup.GetComponent<RectTransform>();
        }
    }

    private void SetExitGameButtonsInteractable(bool interactable)
    {
        if (exitGameYesButton != null)
        {
            exitGameYesButton.interactable = interactable;
        }

        if (exitGameNoButton != null)
        {
            exitGameNoButton.interactable = interactable;
        }
    }

    private void ExitGame()
    {
        if (gameManager == null)
        {
            Debug.LogError(
                "[PopupManager] Cannot exit because GameManager is not assigned.");
            return;
        }

        gameManager.ExitGame();
    }

    private Vector3 GetPopupNormalScale(RectTransform popupRect)
    {
        return popupRect == errorPopupRect
            ? errorPopupNormalScale
            : Vector3.one;
    }

    private void CacheScatterWinPanelScale()
    {
        if (scatterWinPanel == null)
        {
            return;
        }

        Vector3 authoredScale = scatterWinPanel.localScale;
        if (authoredScale.sqrMagnitude > 0.0001f)
        {
            scatterWinPanelNormalScale = authoredScale;
        }
    }

    private void CacheUltraWheelRewardPanelScale()
    {
        if (ultraWheelRewardPanel == null)
        {
            return;
        }

        Vector3 authoredScale = ultraWheelRewardPanel.localScale;
        if (authoredScale.sqrMagnitude > 0.0001f)
        {
            ultraWheelRewardPanelNormalScale = authoredScale;
        }
    }

    private void CacheUltraWheelStartPanelScale()
    {
        if (ultraWheelStartPanel == null)
        {
            return;
        }

        Vector3 authoredScale = ultraWheelStartPanel.localScale;
        if (authoredScale.sqrMagnitude > 0.0001f)
        {
            ultraWheelStartPanelNormalScale = authoredScale;
        }
    }

    private static void CachePanelScale(
        RectTransform panel,
        ref Vector3 cachedScale)
    {
        if (panel == null)
        {
            return;
        }

        Vector3 authoredScale = panel.localScale;
        if (authoredScale.sqrMagnitude > 0.0001f)
        {
            cachedScale = authoredScale;
        }
    }

    private void KillScatterWinSequence()
    {
        scatterWinSequence?.Kill();
        scatterWinSequence = null;
    }

    private void KillUltraWinSequence()
    {
        ultraWinSequence?.Kill();
        ultraWinSequence = null;
    }

    private void KillUltraStartSequence()
    {
        ultraStartSequence?.Kill();
        ultraStartSequence = null;
    }

    private void KillCurrentPopupTween()
    {
        currentPopupTween?.Kill();
        currentPopupTween = null;
    }

    private static string FormatOptionalServerAmount(double? amount)
    {
        return amount.HasValue
            ? ServerAmountFormatter.Format(amount.Value)
            : string.Empty;
    }
}
