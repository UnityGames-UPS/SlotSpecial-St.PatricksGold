using System;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PopupManager : MonoBehaviour
{
    [Header("Scatter Win Popup")]
    [Tooltip("Assign the complete ScatterWinPanel RectTransform.")]
    [SerializeField] private RectTransform scatterWinPanel;
    [Tooltip("Assign the TotalWin TMP text inside ScatterWinPanel.")]
    [SerializeField] private TMP_Text scatterTotalWinText;

    [Header("Scatter Win Animation")]
    [Tooltip("Time used to grow the panel from scale 0 to its authored scale.")]
    [SerializeField, Min(0.01f)] private float scaleInDuration = 0.4f;
    [Tooltip("Time used to count TotalWin from 0 to the server result.")]
    [SerializeField, Min(0.01f)] private float totalWinCountDuration = 1.2f;
    [Tooltip("Time the completed amount remains visible before the panel closes.")]
    [SerializeField, Min(0f)] private float completedWinHoldDuration = 1f;
    [Tooltip("Time used to shrink the panel back to scale 0.")]
    [SerializeField, Min(0.01f)] private float scaleOutDuration = 0.4f;

    private Vector3 scatterWinPanelNormalScale = Vector3.one;
    private Sequence scatterWinSequence;

    private void Awake()
    {
        CacheScatterWinPanelScale();
        HideScatterWinImmediate();
    }

    private void OnDisable()
    {
        KillScatterWinSequence();
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

    private void KillScatterWinSequence()
    {
        scatterWinSequence?.Kill();
        scatterWinSequence = null;
    }

    private static string FormatWinAmount(double amount)
    {
        return amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
