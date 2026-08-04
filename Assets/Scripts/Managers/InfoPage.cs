using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Populates the paytable from the game:init configuration.
/// Add this component to the same GameObject as UIManager and assign the
/// TextMeshPro references in the Inspector.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UIManager))]
public sealed class InfoPage : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("GameManager that receives and stores the game:init configuration.")]
    [SerializeField] private GameManager gameManager;

    [Header("Symbol Payout Texts (5x, 4x, 3x)")]
    [SerializeField] private TMP_Text acePayoutText;
    [SerializeField] private TMP_Text kingPayoutText;
    [SerializeField] private TMP_Text queenPayoutText;
    [SerializeField] private TMP_Text jackPayoutText;
    [SerializeField] private TMP_Text tenPayoutText;
    [SerializeField] private TMP_Text beerGlassPayoutText;
    [SerializeField] private TMP_Text greenHatPayoutText;
    [SerializeField] private TMP_Text magnetPayoutText;
    [SerializeField] private TMP_Text cigarPayoutText;

    [Header("Optional Init-Driven Info Texts")]
    [InspectorName("Low-Symbol 5x/4x/3x Values")]
    [Tooltip("Assign SpecialIconMultiplier/MultipleIcons/Multipliers. Uses lowSymbolAnyPay values from init.")]
    [SerializeField] private TMP_Text lowSymbolAnyPayText;
    [InspectorName("Temple Riches Description (Single Multiplier)")]
    [Tooltip("Assign SpecialIconMultiplier/Icon/Text (TMP). Uses the single Temple Riches multiplier from init.")]
    [SerializeField] private TMP_Text templeRichesDescriptionText;
    [InspectorName("Wild Maximum Description")]
    [Tooltip("Description containing the maximum cumulative wild multiplier.")]
    [SerializeField] private TMP_Text wildMaximumDescriptionText;
    [InspectorName("Ultra Wheel Range Values")]
    [Tooltip("The three Ultra Wheel minimum/maximum ranges.")]
    [SerializeField] private TMP_Text ultraWheelRangesText;

    private StPatricksGoldGameConfig lastAppliedConfig;

    private void Awake()
    {
        ResolveGameManager();
    }

    private void OnEnable()
    {
        ResolveGameManager();

        if (gameManager == null)
        {
            Debug.LogError(
                "[InfoPage] GameManager is not assigned and no GameManager was found in the scene.");
            return;
        }

        gameManager.GamePresentationChanged += OnGamePresentationChanged;
        RefreshFromInitData();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.GamePresentationChanged -= OnGamePresentationChanged;
        }
    }

    private void OnGamePresentationChanged()
    {
        StPatricksGoldGameConfig config = gameManager != null
            ? gameManager.stPatricksGoldConfig
            : null;
        if (config != null && !ReferenceEquals(config, lastAppliedConfig))
        {
            RefreshFromInitData();
        }
    }

    /// <summary>
    /// Re-applies the current game:init values to all assigned paytable texts.
    /// </summary>
    internal void RefreshFromInitData()
    {
        StPatricksGoldGameConfig config = gameManager != null
            ? gameManager.stPatricksGoldConfig
            : null;
        if (config == null)
        {
            return;
        }

        int appliedSymbolCount = ApplySymbolPayouts(config.symbols);
        ApplyLowSymbolAnyPayPayouts(config.lowSymbolAnyPay);
        ApplyTempleRichesMultiplier(config.templeRiches);
        ApplyWildMaximumMultiplier(config.wildMultiplierFeature);
        ApplyUltraWheelRanges(config.ultraWheel);

        lastAppliedConfig = config;
        Debug.Log(
            $"[InfoPage] Applied init paytable values for {appliedSymbolCount}/9 standard symbols.");
    }

    private int ApplySymbolPayouts(
        IReadOnlyList<StPatricksGoldSymbolInfo> symbols)
    {
        if (symbols == null)
        {
            Debug.LogWarning("[InfoPage] The init config contains no symbol paytable data.");
            return 0;
        }

        int appliedCount = 0;
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.Ace,
            acePayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.King,
            kingPayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.Queen,
            queenPayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.Jack,
            jackPayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.Ten,
            tenPayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.BeerGlass,
            beerGlassPayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.GreenHat,
            greenHatPayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.Magnet,
            magnetPayoutText);
        appliedCount += ApplySymbolPayout(
            symbols,
            StPatricksGoldSymbolIds.Cigar,
            cigarPayoutText);

        return appliedCount;
    }

    private static int ApplySymbolPayout(
        IReadOnlyList<StPatricksGoldSymbolInfo> symbols,
        int symbolId,
        TMP_Text payoutText)
    {
        if (payoutText == null)
        {
            Debug.LogWarning(
                $"[InfoPage] Assign the payout TMP text for " +
                $"'{StPatricksGoldSymbolIds.GetName(symbolId)}' in the Inspector.");
            return 0;
        }

        StPatricksGoldSymbolInfo symbol = FindSymbol(symbols, symbolId);
        if (symbol == null)
        {
            Debug.LogWarning(
                $"[InfoPage] No init paytable data was found for symbol ID {symbolId}.");
            return 0;
        }

        if (!TryGetPayouts(
                symbol,
                out double payout3x,
                out double payout4x,
                out double payout5x))
        {
            Debug.LogWarning(
                $"[InfoPage] Symbol '{symbol.name ?? symbolId.ToString()}' has no " +
                "3x/4x/5x payout values.");
            return 0;
        }

        payoutText.text = FormatPayouts(payout3x, payout4x, payout5x);
        return 1;
    }

    private void ApplyLowSymbolAnyPayPayouts(
        LowSymbolAnyPayConfig lowSymbolAnyPay)
    {
        if (lowSymbolAnyPayText == null ||
            lowSymbolAnyPay == null ||
            (lowSymbolAnyPay.payout3x == 0 &&
             lowSymbolAnyPay.payout4x == 0 &&
             lowSymbolAnyPay.payout5x == 0))
        {
            return;
        }

        lowSymbolAnyPayText.text = FormatPayouts(
            lowSymbolAnyPay.payout3x,
            lowSymbolAnyPay.payout4x,
            lowSymbolAnyPay.payout5x);
    }

    private void ApplyTempleRichesMultiplier(
        TempleRichesConfig templeRiches)
    {
        if (templeRichesDescriptionText == null ||
            templeRiches == null ||
            templeRiches.multiplier <= 0)
        {
            return;
        }

        templeRichesDescriptionText.text =
            $"Multiplies wins by {templeRiches.multiplier}x when included, " +
            "substitutes for all symbols except:";
    }

    private void ApplyWildMaximumMultiplier(
        WildMultiplierConfig wildMultiplier)
    {
        if (wildMaximumDescriptionText == null || wildMultiplier == null)
        {
            return;
        }

        int maximumMultiplier = wildMultiplier.maxCumulative > 0
            ? wildMultiplier.maxCumulative
            : wildMultiplier.maxCumulativeMultiplier;
        if (maximumMultiplier <= 0)
        {
            return;
        }

        wildMaximumDescriptionText.text =
            "More than 1 wild multiplier may substitute \n" +
            $"and multiply any win, up to {maximumMultiplier}x";
    }

    private void ApplyUltraWheelRanges(UltraWheelConfig ultraWheel)
    {
        if (ultraWheelRangesText == null ||
            ultraWheel == null ||
            !TryGetRange(
                ultraWheel.wheel1Range,
                ultraWheel.wheel1Awards,
                out int wheel1Minimum,
                out int wheel1Maximum) ||
            !TryGetRange(
                ultraWheel.wheel2Range,
                ultraWheel.wheel2Awards,
                out int wheel2Minimum,
                out int wheel2Maximum) ||
            !TryGetRange(
                ultraWheel.wheel3Range,
                ultraWheel.wheel3Awards,
                out int wheel3Minimum,
                out int wheel3Maximum))
        {
            return;
        }

        ultraWheelRangesText.horizontalAlignment = HorizontalAlignmentOptions.Left;
        ultraWheelRangesText.text =
            $"{FormatWholeNumber(wheel1Minimum)}X TO {FormatWholeNumber(wheel1Maximum)}X\n" +
            $"{FormatWholeNumber(wheel2Minimum)}X TO {FormatWholeNumber(wheel2Maximum)}X\n" +
            $"{FormatWholeNumber(wheel3Minimum)}X TO {FormatWholeNumber(wheel3Maximum)}X";
    }

    private void ResolveGameManager()
    {
        if (gameManager != null)
        {
            return;
        }

        GameManager[] sceneGameManagers =
            FindObjectsByType<GameManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        foreach (GameManager sceneGameManager in sceneGameManagers)
        {
            if (sceneGameManager != null &&
                sceneGameManager.gameObject.scene.IsValid())
            {
                gameManager = sceneGameManager;
                return;
            }
        }
    }

    private static StPatricksGoldSymbolInfo FindSymbol(
        IReadOnlyList<StPatricksGoldSymbolInfo> symbols,
        int symbolId)
    {
        for (int index = 0; index < symbols.Count; index++)
        {
            StPatricksGoldSymbolInfo symbol = symbols[index];
            if (symbol != null && symbol.id == symbolId)
            {
                return symbol;
            }
        }

        return null;
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

        // Compact initData supplies named payout fields. The full parse-sheet
        // response can instead supply the same values in the multiplier array.
        if (payout3x != 0 || payout4x != 0 || payout5x != 0)
        {
            return true;
        }

        IReadOnlyList<double> multipliers = symbol.multipliers;
        if (multipliers == null)
        {
            return false;
        }

        if (multipliers.Count >= 6)
        {
            payout3x = multipliers[3];
            payout4x = multipliers[4];
            payout5x = multipliers[5];
            return true;
        }

        if (multipliers.Count >= 5)
        {
            payout3x = multipliers[2];
            payout4x = multipliers[3];
            payout5x = multipliers[4];
            return true;
        }

        if (multipliers.Count >= 3)
        {
            payout3x = multipliers[0];
            payout4x = multipliers[1];
            payout5x = multipliers[2];
            return true;
        }

        return false;
    }

    private static bool TryGetRange(
        IReadOnlyList<int> configuredRange,
        IReadOnlyList<int> awards,
        out int minimum,
        out int maximum)
    {
        minimum = 0;
        maximum = 0;

        if (configuredRange != null && configuredRange.Count >= 2)
        {
            minimum = configuredRange[0];
            maximum = configuredRange[1];
            return minimum <= maximum;
        }

        if (awards == null || awards.Count == 0)
        {
            return false;
        }

        minimum = awards[0];
        maximum = awards[0];
        for (int index = 1; index < awards.Count; index++)
        {
            minimum = Math.Min(minimum, awards[index]);
            maximum = Math.Max(maximum, awards[index]);
        }

        return true;
    }

    private static string FormatPayouts(
        double payout3x,
        double payout4x,
        double payout5x)
    {
        return
            $"5 X {FormatPayout(payout5x)}\n" +
            $"4 X {FormatPayout(payout4x)}\n" +
            $"3 X {FormatPayout(payout3x)}";
    }

    private static string FormatPayout(double payout)
    {
        return ServerAmountFormatter.Format(payout);
    }

    private static string FormatWholeNumber(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
