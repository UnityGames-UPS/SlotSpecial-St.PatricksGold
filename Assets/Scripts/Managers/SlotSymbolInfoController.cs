using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIManager))]
public sealed class SlotSymbolInfoController : MonoBehaviour
{
    private const string ScatterWheelDescription =
        "3 or more, in any position, trigger the scatter wheel feature";
    private const string UltraWheelDescription =
        "3 in any position on reels 2,3 and 4 trigger the ultra wheel bonus";
    private const string TempleRichesDescription =
        "Multiplies wins by 5x when included, substitutes for all symbols except";
    private const string WildDescription =
        "Substitutes for all Symbols except Scatter";

    [Header("Sources")]
    [Tooltip("The normal five-reel SlotView. Found automatically when left empty.")]
    [SerializeField] private SlotView slotView;
    [Tooltip("Provides the symbol values received in init data. Found automatically when left empty.")]
    [SerializeField] private GameManager gameManager;

    [Header("Slot Info UI")]
    [Tooltip("The complete SlotInfo object that is shown and hidden.")]
    [SerializeField] private GameObject slotInfoRoot;
    [Tooltip("Controls the alpha of the complete panel. Created automatically on Slot Info Root when left empty.")]
    [SerializeField] private CanvasGroup slotInfoCanvasGroup;
    [Tooltip("The Image whose sprite changes for the left/right panel artwork.")]
    [SerializeField] private Image panelImage;
    [Tooltip("The TMP text that displays the clicked symbol's init information.")]
    [SerializeField] private TMP_Text infoText;

    [Header("Side Artwork")]
    [Tooltip("Panel used for reels 1 and 2. Its pointer should face left toward the clicked symbol.")]
    [SerializeField] private Sprite panelShownOnRightSprite;
    [Tooltip("Panel used for reels 3, 4, and 5. Its pointer should face right toward the clicked symbol.")]
    [SerializeField] private Sprite panelShownOnLeftSprite;

    [Header("Text Styling")]
    [Tooltip("Gold used only for the X5, X4, and X3 payout labels.")]
    [SerializeField] private Color payoutLabelColor =
        new Color(1f, 0.78f, 0f, 1f);
    [SerializeField] private float payoutLineSpacing = 10f;
    [SerializeField] private float descriptionLineSpacing;
    [SerializeField, Min(1f)] private float descriptionMinimumFontSize = 10f;

    [Header("Timing And Position")]
    [SerializeField, Min(-75f)] private float horizontalGap = -75f;
    [SerializeField, Min(0.01f)] private float visibleDuration = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;

    private readonly List<ClickRegistration> clickRegistrations =
        new List<ClickRegistration>();
    private Coroutine hideCoroutine;
    private float authoredInfoFontSize = 36f;
    private float authoredInfoWordWrappingRatio = 0.4f;

    private sealed class ClickRegistration
    {
        internal Image Image;
        internal bool PreviousRaycastTarget;
        internal EventTrigger Trigger;
        internal EventTrigger.Entry Entry;
    }

    private void Awake()
    {
        ResolveSources();
        ResolveCanvasGroup();
        if (infoText != null)
        {
            authoredInfoFontSize = Mathf.Max(1f, infoText.fontSize);
            authoredInfoWordWrappingRatio =
                Mathf.Clamp01(infoText.wordWrappingRatios);
        }
        HideImmediate();
    }

    private IEnumerator Start()
    {
        // SlotView owns the serialized reel-image lists. Waiting one frame lets
        // its normal initialization finish before click listeners are added.
        yield return null;
        BindVisibleSymbolClicks();
    }

    private void Update()
    {
        if (slotInfoRoot != null &&
            slotInfoRoot.activeSelf &&
            slotView != null &&
            slotView.IsSpinning())
        {
            HideImmediate();
        }
    }

    private void OnDisable()
    {
        HideImmediate();
    }

    private void OnDestroy()
    {
        UnbindVisibleSymbolClicks();
    }

    private void ResolveSources()
    {
        if (slotView == null)
        {
            slotView = FindFirstObjectByType<SlotView>(
                FindObjectsInactive.Include);
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>(
                FindObjectsInactive.Include);
        }
    }

    private void ResolveCanvasGroup()
    {
        if (slotInfoCanvasGroup != null || slotInfoRoot == null)
        {
            return;
        }

        slotInfoCanvasGroup = slotInfoRoot.GetComponent<CanvasGroup>();
        if (slotInfoCanvasGroup == null)
        {
            slotInfoCanvasGroup = slotInfoRoot.AddComponent<CanvasGroup>();
        }

        slotInfoCanvasGroup.interactable = false;
        slotInfoCanvasGroup.blocksRaycasts = false;
    }

    private void BindVisibleSymbolClicks()
    {
        if (slotView == null)
        {
            Debug.LogError(
                "[SlotSymbolInfo] SlotView is not assigned and could not be found.");
            return;
        }

        if (clickRegistrations.Count > 0)
        {
            return;
        }

        for (int column = 0;
             column < StPatricksGoldDefinition.ReelCount;
             column++)
        {
            for (int row = 0;
                 row < StPatricksGoldDefinition.RowCount;
                 row++)
            {
                Image symbolImage =
                    slotView.GetVisibleSymbolImage(column, row);
                if (symbolImage == null)
                {
                    Debug.LogWarning(
                        $"[SlotSymbolInfo] No visible symbol Image was found " +
                        $"for reel {column + 1}, row {row + 1}.");
                    continue;
                }

                EventTrigger trigger =
                    symbolImage.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = symbolImage.gameObject.AddComponent<EventTrigger>();
                }

                if (trigger.triggers == null)
                {
                    trigger.triggers = new List<EventTrigger.Entry>();
                }

                int capturedColumn = column;
                int capturedRow = row;
                RectTransform capturedRect = symbolImage.rectTransform;
                var entry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };
                entry.callback.AddListener(
                    eventData => OnSymbolClicked(
                        capturedColumn,
                        capturedRow,
                        capturedRect,
                        eventData as PointerEventData));

                bool previousRaycastTarget = symbolImage.raycastTarget;
                symbolImage.raycastTarget = true;
                trigger.triggers.Add(entry);
                clickRegistrations.Add(
                    new ClickRegistration
                    {
                        Image = symbolImage,
                        PreviousRaycastTarget = previousRaycastTarget,
                        Trigger = trigger,
                        Entry = entry
                    });
            }
        }
    }

    private void UnbindVisibleSymbolClicks()
    {
        foreach (ClickRegistration registration in clickRegistrations)
        {
            if (registration?.Trigger != null &&
                registration.Trigger.triggers != null)
            {
                registration.Trigger.triggers.Remove(registration.Entry);
            }

            if (registration?.Image != null)
            {
                registration.Image.raycastTarget =
                    registration.PreviousRaycastTarget;
            }
        }

        clickRegistrations.Clear();
    }

    private void OnSymbolClicked(
        int column,
        int row,
        RectTransform symbolRect,
        PointerEventData pointerEvent)
    {
        if (!isActiveAndEnabled ||
            pointerEvent == null ||
            pointerEvent.button != PointerEventData.InputButton.Left ||
            slotView == null ||
            slotView.IsSpinning())
        {
            return;
        }

        List<List<int>> matrix = slotView.GetCurrentDisplayMatrix();
        if (matrix == null ||
            column < 0 ||
            column >= matrix.Count ||
            matrix[column] == null ||
            row < 0 ||
            row >= matrix[column].Count)
        {
            return;
        }

        int symbolId = matrix[column][row];
        if (!TryBuildInfoText(
                symbolId,
                out string text,
                out bool isStandardPaySymbol))
        {
            Debug.LogWarning(
                $"[SlotSymbolInfo] Init data contains no display information " +
                $"for symbol ID {symbolId}.");
            HideImmediate();
            return;
        }

        Show(
            column,
            symbolRect,
            text,
            isStandardPaySymbol);
    }

    private void Show(
        int column,
        RectTransform symbolRect,
        string text,
        bool isStandardPaySymbol)
    {
        if (slotInfoRoot == null || panelImage == null || infoText == null)
        {
            Debug.LogError(
                "[SlotSymbolInfo] Assign Slot Info Root, Panel Image, and Info Text.");
            return;
        }

        bool showOnRight = column < 2;
        Sprite sideSprite = showOnRight
            ? panelShownOnRightSprite
            : panelShownOnLeftSprite;
        if (sideSprite == null)
        {
            Debug.LogError(
                showOnRight
                    ? "[SlotSymbolInfo] Assign Panel Shown On Right Sprite."
                    : "[SlotSymbolInfo] Assign Panel Shown On Left Sprite.");
            return;
        }

        panelImage.sprite = sideSprite;
        infoText.text = text;
        ApplyTextStyle(isStandardPaySymbol);
        AudioController.Instance?.PlayInfoPanelArrowButton();
        panelImage.rectTransform.localRotation = Quaternion.identity;
        infoText.rectTransform.localRotation = Quaternion.identity;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        ResolveCanvasGroup();
        if (slotInfoCanvasGroup != null)
        {
            slotInfoCanvasGroup.alpha = 1f;
        }

        slotInfoRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        PositionBesideSymbol(symbolRect, showOnRight);

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private void PositionBesideSymbol(
        RectTransform symbolRect,
        bool showOnRight)
    {
        if (symbolRect == null || panelImage == null)
        {
            return;
        }

        RectTransform panelRect = panelImage.rectTransform;
        RectTransform panelParent = panelRect.parent as RectTransform;
        if (panelParent == null)
        {
            return;
        }

        var worldCorners = new Vector3[4];
        symbolRect.GetWorldCorners(worldCorners);
        Vector3 symbolEdge = showOnRight
            ? (worldCorners[2] + worldCorners[3]) * 0.5f
            : (worldCorners[0] + worldCorners[1]) * 0.5f;

        Camera sourceCamera = GetCanvasCamera(symbolRect);
        Camera destinationCamera = GetCanvasCamera(panelParent);
        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(sourceCamera, symbolEdge);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelParent,
                screenPoint,
                destinationCamera,
                out Vector2 localPoint))
        {
            return;
        }

        panelRect.pivot = new Vector2(showOnRight ? 0f : 1f, 0.5f);
        Vector3 localPosition = panelRect.localPosition;
        localPosition.x = localPoint.x +
                          (showOnRight ? horizontalGap : -horizontalGap);
        localPosition.y = localPoint.y;
        panelRect.localPosition = localPosition;
    }

    private void ApplyTextStyle(bool isStandardPaySymbol)
    {
        infoText.richText = true;
        infoText.color = Color.white;
        infoText.verticalAlignment = VerticalAlignmentOptions.Middle;
        infoText.fontSize = authoredInfoFontSize;

        if (isStandardPaySymbol)
        {
            infoText.horizontalAlignment =
                HorizontalAlignmentOptions.Flush;
            // Give each payout line one real word gap and make TMP put all
            // Flush expansion into that gap, never between X and its count.
            infoText.wordWrappingRatios = 0f;
            infoText.enableAutoSizing = false;
            infoText.textWrappingMode = TextWrappingModes.NoWrap;
            infoText.lineSpacing = payoutLineSpacing;
            return;
        }

        infoText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        infoText.wordWrappingRatios = authoredInfoWordWrappingRatio;
        infoText.enableAutoSizing = true;
        infoText.textWrappingMode = TextWrappingModes.Normal;
        infoText.fontSizeMin = Mathf.Min(
            descriptionMinimumFontSize,
            authoredInfoFontSize);
        infoText.fontSizeMax = authoredInfoFontSize;
        infoText.lineSpacing = descriptionLineSpacing;
    }

    private static Camera GetCanvasCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform != null
            ? rectTransform.GetComponentInParent<Canvas>()
            : null;
        return canvas != null &&
               canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    private bool TryBuildInfoText(
        int symbolId,
        out string text,
        out bool isStandardPaySymbol)
    {
        text = null;
        isStandardPaySymbol = false;
        if (TryGetSpecialSymbolDescription(symbolId, out text))
        {
            return true;
        }

        IReadOnlyList<StPatricksGoldSymbolInfo> symbols =
            gameManager?.stPatricksGoldConfig?.symbols;
        if (symbols == null)
        {
            return false;
        }

        StPatricksGoldSymbolInfo symbol = null;
        for (int index = 0; index < symbols.Count; index++)
        {
            if (symbols[index] != null && symbols[index].id == symbolId)
            {
                symbol = symbols[index];
                break;
            }
        }

        if (symbol == null)
        {
            return false;
        }

        bool isSpecialSymbol =
            string.Equals(
                symbol.group,
                "special",
                StringComparison.OrdinalIgnoreCase) ||
            symbol.isWild ||
            symbol.isScatter ||
            symbol.id == StPatricksGoldSymbolIds.Wild ||
            symbol.id == StPatricksGoldSymbolIds.ScatterWheel ||
            symbol.id == StPatricksGoldSymbolIds.UltraWheel ||
            symbol.id == StPatricksGoldSymbolIds.TempleRiches;
        isStandardPaySymbol = !isSpecialSymbol;

        if (isSpecialSymbol)
        {
            if (string.IsNullOrWhiteSpace(symbol.description))
            {
                return false;
            }

            text = symbol.description.Trim();
            return true;
        }

        if (!TryGetPayouts(
                symbol,
                out double payout3x,
                out double payout4x,
                out double payout5x))
        {
            return false;
        }

        // Init data supplies the symbol paytable as multipliers. Convert them
        // to the monetary values for the base bet selected at click time.
        double baseBet = Math.Max(0d, gameManager.GetDisplayedBetAmount());
        payout3x *= baseBet;
        payout4x *= baseBet;
        payout5x *= baseBet;

        text =
            BuildPayoutLine(5, payout5x) + "\n" +
            BuildPayoutLine(4, payout4x) + "\n" +
            BuildPayoutLine(3, payout3x);
        return true;
    }

    private static bool TryGetSpecialSymbolDescription(
        int symbolId,
        out string description)
    {
        switch (symbolId)
        {
            case StPatricksGoldSymbolIds.ScatterWheel:
                description = ScatterWheelDescription;
                return true;
            case StPatricksGoldSymbolIds.UltraWheel:
                description = UltraWheelDescription;
                return true;
            case StPatricksGoldSymbolIds.TempleRiches:
                description = TempleRichesDescription;
                return true;
            case StPatricksGoldSymbolIds.Wild:
                description = WildDescription;
                return true;
            default:
                description = null;
                return false;
        }
    }

    private string BuildPayoutLine(int matchCount, double value)
    {
        string goldHex = ColorUtility.ToHtmlStringRGB(payoutLabelColor);
        return
            $"<color=#{goldHex}>X{matchCount}</color>" +
            $" {FormatValue(value)}";
    }

    private static bool TryGetPayouts(
        StPatricksGoldSymbolInfo symbol,
        out double payout3x,
        out double payout4x,
        out double payout5x)
    {
        payout3x = symbol.payout3x;
        payout4x = symbol.payout4x;
        payout5x = symbol.payout5x;
        if (payout3x != 0d || payout4x != 0d || payout5x != 0d)
        {
            return true;
        }

        IReadOnlyList<double> multipliers = symbol.multipliers;
        if (multipliers == null)
        {
            return false;
        }

        if (multipliers.Count >= 3)
        {
            // Live initData supplies multiplier as [5x, 4x, 3x].
            payout5x = multipliers[0];
            payout4x = multipliers[1];
            payout3x = multipliers[2];
            return true;
        }

        return false;
    }

    private static string FormatValue(double value)
    {
        return ServerAmountFormatter.Format(value);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.01f, visibleDuration));

        ResolveCanvasGroup();
        float duration = Mathf.Max(0f, fadeOutDuration);
        if (slotInfoCanvasGroup != null && duration > 0f)
        {
            float startingAlpha = slotInfoCanvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                slotInfoCanvasGroup.alpha = Mathf.Lerp(
                    startingAlpha,
                    0f,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        if (slotInfoCanvasGroup != null)
        {
            slotInfoCanvasGroup.alpha = 0f;
        }

        hideCoroutine = null;
        HideImmediate();
    }

    private void HideImmediate()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (slotInfoRoot != null)
        {
            ResolveCanvasGroup();
            if (slotInfoCanvasGroup != null)
            {
                slotInfoCanvasGroup.alpha = 0f;
            }

            slotInfoRoot.SetActive(false);
        }
    }

    private void OnValidate()
    {
        horizontalGap = Mathf.Max(-75f, horizontalGap);
        descriptionMinimumFontSize = Mathf.Max(
            1f,
            descriptionMinimumFontSize);
        visibleDuration = Mathf.Max(0.01f, visibleDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
    }
}
