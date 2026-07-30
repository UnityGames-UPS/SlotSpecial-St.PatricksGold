using System;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PopupManager : MonoBehaviour
{
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

    [Header("System Popup Parent")]
    [Tooltip(
        "Optional shared parent for the disconnection and insufficient balance panels.")]
    [SerializeField] private GameObject systemPopupParent;

    [Header("Disconnection Popup")]
    [SerializeField] private RectTransform disconnectionPanel;
    [SerializeField] private TMP_Text disconnectionMessageText;
    [SerializeField] private Button disconnectionOkButton;
    [SerializeField, TextArea]
    private string defaultDisconnectionMessage =
        "Game disconnected due to a network error. Please relaunch the game.";

    [Header("Insufficient Balance Popup")]
    [SerializeField] private RectTransform insufficientBalancePanel;
    [SerializeField] private TMP_Text insufficientBalanceMessageText;
    [SerializeField] private Button insufficientBalanceOkButton;
    [SerializeField, TextArea]
    private string defaultInsufficientBalanceMessage =
        "Insufficient balance. Please add funds to continue.";

    [Header("System Popup Actions")]
    [Tooltip(
        "Used only to exit after the disconnection popup is acknowledged. " +
        "Found automatically when left empty.")]
    [SerializeField] private GameManager gameManager;

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
    private Vector3 disconnectionPanelNormalScale = Vector3.one;
    private Vector3 insufficientBalancePanelNormalScale = Vector3.one;
    private Sequence scatterWinSequence;
    private Sequence ultraStartSequence;
    private Sequence ultraWinSequence;
    private Sequence systemPopupSequence;
    private RectTransform currentSystemPopup;

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
        CacheSystemPopupScales();
        HideScatterWinImmediate();
        HideUltraStartImmediate();
        HideUltraWinImmediate();
        HideSystemPopupsImmediate();
    }

    private void OnEnable()
    {
        if (disconnectionOkButton != null)
        {
            disconnectionOkButton.onClick.AddListener(
                OnDisconnectionOkClicked);
        }

        if (insufficientBalanceOkButton != null)
        {
            insufficientBalanceOkButton.onClick.AddListener(
                OnInsufficientBalanceOkClicked);
        }
    }

    private void OnDisable()
    {
        if (disconnectionOkButton != null)
        {
            disconnectionOkButton.onClick.RemoveListener(
                OnDisconnectionOkClicked);
        }

        if (insufficientBalanceOkButton != null)
        {
            insufficientBalanceOkButton.onClick.RemoveListener(
                OnInsufficientBalanceOkClicked);
        }

        KillScatterWinSequence();
        KillUltraStartSequence();
        KillUltraWinSequence();
        KillSystemPopupSequence();
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

        double sanitizedTotalWin = Math.Max(0d, totalWin);
        double displayedTotalWin = 0d;

        scatterWinPanel.gameObject.SetActive(true);
        scatterWinPanel.localScale = Vector3.zero;
        scatterTotalWinText.gameObject.SetActive(true);
        scatterTotalWinText.text = FormatWinAmount(0d);

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
                                FormatWinAmount(value);
                        },
                        sanitizedTotalWin,
                        Mathf.Max(0.01f, totalWinCountDuration))
                    .SetEase(Ease.OutCubic))
            .AppendCallback(
                () =>
                    scatterTotalWinText.text =
                        FormatWinAmount(sanitizedTotalWin))
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
        double displayedTotalWin = 0d;

        ultraWheelRewardPanel.gameObject.SetActive(true);
        ultraWheelRewardPanel.localScale = Vector3.zero;

        greenWheelWinText.gameObject.SetActive(true);
        blueWheelWinText.gameObject.SetActive(true);
        redWheelWinText.gameObject.SetActive(true);
        ultraTotalWinText.gameObject.SetActive(true);

        // Individual wheel results must load immediately. Only Total Win counts up.
        greenWheelWinText.text = FormatOptionalWinAmount(sanitizedGreenWin);
        blueWheelWinText.text = FormatOptionalWinAmount(sanitizedBlueWin);
        redWheelWinText.text = FormatOptionalWinAmount(sanitizedRedWin);
        ultraTotalWinText.text = FormatWinAmount(0d);

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
                                FormatWinAmount(value);
                        },
                        sanitizedTotalWin,
                        Mathf.Max(0.01f, totalWinCountDuration))
                    .SetEase(Ease.OutCubic))
            .AppendCallback(
                () =>
                    ultraTotalWinText.text =
                        FormatWinAmount(sanitizedTotalWin))
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
            defaultDisconnectionMessage);
    }

    internal bool ShowDisconnectionPopup(string message)
    {
        CachePanelScale(
            disconnectionPanel,
            ref disconnectionPanelNormalScale);

        return ShowSystemPopup(
            disconnectionPanel,
            disconnectionMessageText,
            string.IsNullOrWhiteSpace(message)
                ? defaultDisconnectionMessage
                : message,
            disconnectionPanelNormalScale,
            "Disconnection Panel");
    }

    internal bool ShowInsufficientBalancePopup()
    {
        return ShowInsufficientBalancePopup(
            defaultInsufficientBalanceMessage);
    }

    internal bool ShowInsufficientBalancePopup(string message)
    {
        CachePanelScale(
            insufficientBalancePanel,
            ref insufficientBalancePanelNormalScale);

        return ShowSystemPopup(
            insufficientBalancePanel,
            insufficientBalanceMessageText,
            string.IsNullOrWhiteSpace(message)
                ? defaultInsufficientBalanceMessage
                : message,
            insufficientBalancePanelNormalScale,
            "Insufficient Balance Panel");
    }

    internal bool ShowInsufficientFundsError()
    {
        return ShowInsufficientBalancePopup();
    }

    internal void CloseDisconnectionPopup()
    {
        CloseSystemPopup(disconnectionPanel, null);
    }

    internal void CloseInsufficientBalancePopup()
    {
        CloseSystemPopup(insufficientBalancePanel, null);
    }

    internal void HideSystemPopupsImmediate()
    {
        KillSystemPopupSequence();

        HideSystemPopupImmediate(
            disconnectionPanel,
            disconnectionPanelNormalScale);
        HideSystemPopupImmediate(
            insufficientBalancePanel,
            insufficientBalancePanelNormalScale);

        currentSystemPopup = null;
        UpdateSystemPopupParentState();
    }

    private bool ShowSystemPopup(
        RectTransform panel,
        TMP_Text messageText,
        string message,
        Vector3 normalScale,
        string panelName)
    {
        if (panel == null)
        {
            Debug.LogError(
                $"[PopupManager] Assign the {panelName} reference.");
            return false;
        }

        CloseCurrentSystemPopupImmediate();

        if (systemPopupParent != null)
        {
            systemPopupParent.SetActive(true);
        }

        if (messageText != null)
        {
            messageText.text = message ?? string.Empty;
        }

        currentSystemPopup = panel;
        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.zero;

        systemPopupSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                panel
                    .DOScale(
                        normalScale,
                        Mathf.Max(0.01f, scaleInDuration))
                    .SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                systemPopupSequence = null;
                panel.localScale = normalScale;
            });

        return true;
    }

    private void OnDisconnectionOkClicked()
    {
        CloseSystemPopup(
            disconnectionPanel,
            () => gameManager?.ExitGame());
    }

    private void OnInsufficientBalanceOkClicked()
    {
        CloseSystemPopup(
            insufficientBalancePanel,
            null);
    }

    private void CloseSystemPopup(
        RectTransform panel,
        Action onComplete)
    {
        if (panel == null || !panel.gameObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        KillSystemPopupSequence();

        Vector3 normalScale =
            GetSystemPopupNormalScale(panel);
        systemPopupSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                panel
                    .DOScale(
                        normalScale * 1.1f,
                        0.1f)
                    .SetEase(Ease.OutQuad))
            .Append(
                panel
                    .DOScale(
                        Vector3.zero,
                        Mathf.Max(0.01f, scaleOutDuration))
                    .SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                systemPopupSequence = null;
                panel.localScale = normalScale;
                panel.gameObject.SetActive(false);

                if (currentSystemPopup == panel)
                {
                    currentSystemPopup = null;
                }

                UpdateSystemPopupParentState();
                onComplete?.Invoke();
            });
    }

    private void CloseCurrentSystemPopupImmediate()
    {
        KillSystemPopupSequence();

        if (currentSystemPopup == null)
        {
            return;
        }

        RectTransform panel = currentSystemPopup;
        currentSystemPopup = null;
        panel.localScale = GetSystemPopupNormalScale(panel);
        panel.gameObject.SetActive(false);
    }

    private void UpdateSystemPopupParentState()
    {
        if (systemPopupParent == null)
        {
            return;
        }

        bool hasActivePopup =
            (disconnectionPanel != null &&
             disconnectionPanel.gameObject.activeSelf) ||
            (insufficientBalancePanel != null &&
             insufficientBalancePanel.gameObject.activeSelf);
        systemPopupParent.SetActive(hasActivePopup);
    }

    private static void HideSystemPopupImmediate(
        RectTransform panel,
        Vector3 normalScale)
    {
        if (panel == null)
        {
            return;
        }

        panel.localScale = normalScale;
        panel.gameObject.SetActive(false);
    }

    private Vector3 GetSystemPopupNormalScale(
        RectTransform panel)
    {
        if (panel == disconnectionPanel)
        {
            return disconnectionPanelNormalScale;
        }

        if (panel == insufficientBalancePanel)
        {
            return insufficientBalancePanelNormalScale;
        }

        return Vector3.one;
    }

    internal void HideScatterWinImmediate()
    {
        KillScatterWinSequence();

        if (scatterTotalWinText != null)
        {
            scatterTotalWinText.text = FormatWinAmount(0d);
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
            ultraTotalWinText.text = FormatWinAmount(0d);
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

    private void CacheSystemPopupScales()
    {
        CachePanelScale(
            disconnectionPanel,
            ref disconnectionPanelNormalScale);
        CachePanelScale(
            insufficientBalancePanel,
            ref insufficientBalancePanelNormalScale);
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

    private void KillSystemPopupSequence()
    {
        systemPopupSequence?.Kill();
        systemPopupSequence = null;
    }

    private static string FormatWinAmount(double amount)
    {
        return amount.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string FormatOptionalWinAmount(double? amount)
    {
        return amount.HasValue
            ? FormatWinAmount(amount.Value)
            : string.Empty;
    }
}
