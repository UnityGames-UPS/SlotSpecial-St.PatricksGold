using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

#region Server Communication Models

public static class StPatricksGoldDefinition
{
    public const string GameId = "SL-SPG";
    public const int ReelCount = 5;
    public const int RowCount = 3;
    public const int SymbolCount = 13;
    public const int PaylineCount = 30;
}

public static class StPatricksGoldSymbolIds
{
    public const int Ace = 0;
    public const int King = 1;
    public const int Queen = 2;
    public const int Jack = 3;
    public const int Ten = 4;
    public const int BeerGlass = 5;
    public const int GreenHat = 6;
    public const int Magnet = 7;
    public const int Cigar = 8;
    public const int Wild = 9;
    public const int ScatterWheel = 10;
    public const int UltraWheel = 11;
    public const int TempleRiches = 12;

    public static string GetName(int symbolId)
    {
        return symbolId switch
        {
            Ace => "Ace",
            King => "King",
            Queen => "Queen",
            Jack => "Jack",
            Ten => "Ten",
            BeerGlass => "BeerGlass",
            GreenHat => "GreenHat",
            Magnet => "Magnet",
            Cigar => "Cigar",
            Wild => "Wild",
            ScatterWheel => "ScatterWheel",
            UltraWheel => "UltraWheel",
            TempleRiches => "TempleRiches",
            _ => null
        };
    }
}

[Serializable]
public class StPatricksGoldFeatureConfig
{
    public int totalLines;
    public double betMultiplier;
    public UltraWheelConfig ultraWheel;
    public ScatterWheelConfig scatterWheel;
    public TempleRichesConfig templeRiches;
    public WildMultiplierConfig wildMultiplier;
    public LowSymbolAnyPayConfig lowSymbolAnyPay;
}

[Serializable]
public class LowSymbolAnyPayConfig
{
    public bool enabled;
    public int payout3x;
    public int payout4x;
    public int payout5x;
    public List<int> lowSymbolIds;
}

[Serializable]
public class SlotMatrixDimensions
{
    public int x;
    public int y;
}

[Serializable]
public class ReelSymbolCounts
{
    [UnityEngine.Serialization.FormerlySerializedAs("0")] public int reel0;
    [UnityEngine.Serialization.FormerlySerializedAs("1")] public int reel1;
    [UnityEngine.Serialization.FormerlySerializedAs("2")] public int reel2;
    [UnityEngine.Serialization.FormerlySerializedAs("3")] public int reel3;
    [UnityEngine.Serialization.FormerlySerializedAs("4")] public int reel4;
}

[Serializable]
public class StPatricksGoldConfigResponse
{
    public string id;
    public List<double> bets;
    public List<List<int>> lines;
    public SlotMatrixDimensions matrix;
    public List<StPatricksGoldSymbol> symbols;
    public StPatricksGoldFeatureConfig features;
    public bool isSpecial;
}

[Serializable]
public class ScatterWheelConfig
{
    public List<ScatterWheelAwardTable> wheels;
    public bool enabled;
    public int minTriggerCount;
    // Present in the compact live initData response.
    public int wheelCount;
}

[Serializable]
public class StPatricksGoldSymbol
{
    public int id;
    public string name;
    public string group;
    public List<int> multiplier;
    public string description;
    public ReelSymbolCounts reelsInstance;
    // Present alongside multiplier in the compact live UI data.
    public int payout3x;
    public int payout4x;
    public int payout5x;
}

[Serializable]
public class TempleRichesConfig
{
    public bool enabled;
    public int multiplier;
    public List<int> excludedSymbols;
}

[Serializable]
public class UltraWheelConfig
{
    public bool enabled;
    // Compact live initData supplies min/max ranges instead of full award tables.
    public List<int> wheel1Range;
    public List<int> wheel2Range;
    public List<int> wheel3Range;
    public List<double> wheel1Probs;
    public List<double> wheel2Probs;
    public List<double> wheel3Probs;
    public List<int> triggerReels;
    public List<int> wheel1Awards;
    public List<int> wheel2Awards;
    public List<int> wheel3Awards;
    public int minTriggerCount;
    public List<string> bonusWheelStates;
    public List<double> bonusWheelStateProbs;
    public List<int> bonusWheelMultipliers;
    public List<double> bonusWheelMultiplierProbs;
}

[Serializable]
public class ScatterWheelAwardTable
{
    public List<int> awards;
    public int wheelId;
    public List<double> awardProbs;
}

[Serializable]
public class WildMultiplierConfig
{
    public bool enabled;
    public List<int> multipliers;
    public List<int> excludedSymbols;
    public List<double> multipliersProb;
    public int maxCumulativeMultiplier;
    // Alias used by the compact live initData response.
    public int maxCumulative;
}

// ============================================================================
// FIXED: Server Response Models - Must match actual server JSON structure
// ============================================================================

[Serializable]
public class ServerSpinResponse
{
    public string id;
    public bool success;
    public List<List<string>> matrix;
    public ServerPlayerBalance player;
    public ServerPayload payload;
}

[Serializable]
public class ServerPlayerBalance
{
    public double? balance; // Nullable because server sends null
}

[Serializable]
public class ServerPayload
{
    public List<List<string>> reels;        // Server sends STRINGS not ints!
    public List<ServerWinLine> winningLines; // Server uses "winningLines"
    public double totalWin;                  // Server uses "totalWin"
    public List<ServerWinLine> lineWins;     // Alternate server win-line field
    public double winAmount;                 // Alternate server total-win field
    public int scatterCount;
    public bool scatterTriggered;
}

[Serializable]
public class ServerWinLine
{
    public int lineIndex;                    // Server uses "lineIndex"
    public List<string> positions;           // Coordinate strings supplied by the server
    public string symbolId;                  // Server sends STRING!
    public string symbolName;
    public int matchCount;
    public double basePayout;
    public double payout;
    public double winAmount;
    public int wildMultiplier;
    public List<WildDetail> wildDetails;

    // Normalized [row, column] coordinates populated by SocketIOManager.
    // The live server sends nested numeric arrays while older responses used strings.
    [NonSerialized] public List<List<int>> normalizedPositions;
}

[Serializable]
public class WildDetail
{
    public int col;
    public int row;
    public int multiplier;
}

// ============================================================================
// Client-Side Spin Request
// ============================================================================

[Serializable]
public class SpinRequest
{
    public string type = "SPIN";
    public SpinPayload payload;
}

[Serializable]
public class SpinPayload
{
    public int betIndex;
    public bool isFreeSpin;
}


#endregion

#region Game Configuration (Client Side Converted)

[Serializable]
public class StPatricksGoldGameConfig
{
    public int reelCount = StPatricksGoldDefinition.ReelCount;
    public int rowCount = StPatricksGoldDefinition.RowCount;
    public int symbolCount = StPatricksGoldDefinition.SymbolCount;
    public int paylineCount = StPatricksGoldDefinition.PaylineCount;
    public List<List<int>> paylines;
    public List<double> availableBets;
    public List<StPatricksGoldSymbolInfo> symbols;

    public int wildSymbolId = StPatricksGoldSymbolIds.Wild;
    public List<int> wildMultipliers = new List<int> { 2, 3, 4, 5 };

    public int scatterWheelSymbolId = StPatricksGoldSymbolIds.ScatterWheel;
    public int ultraWheelSymbolId = StPatricksGoldSymbolIds.UltraWheel;
    public int templeRichesSymbolId = StPatricksGoldSymbolIds.TempleRiches;

    public double betMultiplier = StPatricksGoldDefinition.PaylineCount;
    public bool isSpecial;
    public UltraWheelConfig ultraWheel;
    public ScatterWheelConfig scatterWheel;
    public TempleRichesConfig templeRiches;
    public WildMultiplierConfig wildMultiplierFeature;
    public LowSymbolAnyPayConfig lowSymbolAnyPay;
}

[Serializable]
public class StPatricksGoldSymbolInfo
{
    public int id;
    public string name;
    public string group;
    public string description;
    public ReelSymbolCounts reelSymbolCounts;
    public List<double> multipliers;
    public bool isWild;
    public bool isScatter;
    public int wildMultiplier;
    public double payout;
    public double payout3x;
    public double payout4x;
    public double payout5x;
}

#endregion

#region Player & Game State (Client Side)

[Serializable]
public class PlayerData
{
    public double balance;
    public int currentBetIndex;
}

[Serializable]
public class SpinResult
{
    public List<List<int>> resultMatrix;  // Client uses int matrix
    public double winAmount;
    public List<WinLine> winLines;
    public PlayerData playerData;
    public ScatterData scatterData;
}

[Serializable]
public class WinLine
{
    public int lineId;
    public int symbolId;
    public List<int> positions;  // Flat list: [0, 5, 10, 15, 20]
    public double winAmount;
}

[Serializable]
public class ScatterData
{
    public bool isTriggered;
    public int scatterCount;
    public double winAmount;
}

#endregion

#region Platform Communication

[Serializable]
public class AuthData
{
    public string token;
    public string socketURL;
    public string nameSpace;
}

#endregion

#region Enums

public enum GameState
{
    Initializing,
    Idle,
    Spinning,
    Stopping,
    ShowingWin
}

public enum SpinSpeed
{
    Normal,
    FastSpin,
    SkipSpin
}

#endregion

#region Helper Classes for Conversion

/// <summary>
/// Converts the SL-SPG configuration response to the runtime configuration.
/// </summary>
public static class GameDataConverter
{
    internal static StPatricksGoldGameConfig ConvertStPatricksGoldConfig(StPatricksGoldConfigResponse serverData)
    {
        int reelCount = serverData.matrix != null && serverData.matrix.x > 0
            ? serverData.matrix.x
            : StPatricksGoldDefinition.ReelCount;
        int rowCount = serverData.matrix != null && serverData.matrix.y > 0 ? serverData.matrix.y : InferRowCount(serverData.lines);

        var config = new StPatricksGoldGameConfig
        {
            reelCount = reelCount,
            rowCount = rowCount,
            symbolCount = serverData.symbols != null ? serverData.symbols.Count : 0,
            paylineCount = ResolvePaylineCount(serverData),
            paylines = serverData.lines ?? new List<List<int>>(),
            availableBets = serverData.bets ?? new List<double>(),
            symbols = new List<StPatricksGoldSymbolInfo>(),
            isSpecial = serverData.isSpecial
        };

        if (serverData.symbols != null)
        {
            foreach (var serverSymbol in serverData.symbols)
            {
                var symbolInfo = new StPatricksGoldSymbolInfo
                {
                    id = serverSymbol.id,
                    name = serverSymbol.name,
                    group = serverSymbol.group,
                    description = serverSymbol.description,
                    reelSymbolCounts = serverSymbol.reelsInstance,
                    multipliers = serverSymbol.multiplier != null
                        ? serverSymbol.multiplier.Select(value => (double)value).ToList()
                        : new List<double>(),
                    isWild = IsSymbolType(serverSymbol, "wild"),
                    isScatter = IsSymbolType(serverSymbol, "scatter"),
                    wildMultiplier = 1,
                    payout = 0,
                    payout3x = serverSymbol.payout3x,
                    payout4x = serverSymbol.payout4x,
                    payout5x = serverSymbol.payout5x
                };

                config.symbols.Add(symbolInfo);

                if (symbolInfo.isWild)
                {
                    config.wildSymbolId = symbolInfo.id;
                }

                if (symbolInfo.isScatter)
                {
                    config.scatterWheelSymbolId = symbolInfo.id;
                }
            }
        }

        config.betMultiplier = config.paylineCount > 0 ? config.paylineCount : 1;

        if (serverData.features != null)
        {
            if (serverData.features.totalLines > 0)
            {
                config.paylineCount = serverData.features.totalLines;
            }

            config.betMultiplier = serverData.features.betMultiplier > 0
                ? serverData.features.betMultiplier
                : (config.paylineCount > 0 ? config.paylineCount : 1);

            config.ultraWheel = serverData.features.ultraWheel;
            config.scatterWheel = serverData.features.scatterWheel;
            config.templeRiches = serverData.features.templeRiches;
            config.wildMultiplierFeature = serverData.features.wildMultiplier;
            config.lowSymbolAnyPay = serverData.features.lowSymbolAnyPay;

            if (serverData.features.wildMultiplier?.multipliers != null &&
                serverData.features.wildMultiplier.multipliers.Count > 0)
            {
                config.wildMultipliers = serverData.features.wildMultiplier.multipliers;
            }
        }

        return config;
    }

    internal static PlayerData CreateInitialPlayerData(StPatricksGoldConfigResponse serverData, int defaultBetIndex = 0, double defaultBalance = 0)
    {
        return new PlayerData
        {
            balance = defaultBalance,
            currentBetIndex = defaultBetIndex
        };
    }

    private static int ResolvePaylineCount(StPatricksGoldConfigResponse serverData)
    {
        if (serverData.features != null && serverData.features.totalLines > 0)
        {
            return serverData.features.totalLines;
        }

        return serverData.lines != null ? serverData.lines.Count : 0;
    }

    private static int InferRowCount(List<List<int>> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            return StPatricksGoldDefinition.RowCount;
        }

        int maxRow = 0;
        foreach (var line in lines)
        {
            if (line == null) continue;

            foreach (var rowIdx in line)
            {
                if (rowIdx > maxRow)
                {
                    maxRow = rowIdx;
                }
            }
        }

        return maxRow + 1;
    }

    private static bool IsSymbolType(StPatricksGoldSymbol symbol, string typeName)
    {
        return ContainsIgnoreCase(symbol.name, typeName) ||
               ContainsIgnoreCase(symbol.group, typeName) ||
               ContainsIgnoreCase(symbol.description, typeName);
    }

    private static bool ContainsIgnoreCase(string value, string search)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Converts the authoritative server matrix from [row][column] strings to
    /// the [column][row] integer layout used by SlotView.
    /// </summary>
    internal static bool TryConvertServerMatrix(
        ServerSpinResponse serverResponse,
        StPatricksGoldGameConfig gameConfig,
        out List<List<int>> resultMatrix,
        out string error)
    {
        resultMatrix = null;
        error = null;

        if (serverResponse == null)
        {
            error = "Spin response is null.";
            return false;
        }

        if (gameConfig == null)
        {
            error = "Game configuration is unavailable; matrix dimensions cannot be validated.";
            return false;
        }

        int expectedRows = gameConfig.rowCount;
        int expectedColumns = gameConfig.reelCount;
        if (expectedRows <= 0 || expectedColumns <= 0)
        {
            error = $"Game configuration has invalid matrix dimensions: rows={expectedRows}, columns={expectedColumns}.";
            return false;
        }

        List<List<string>> sourceMatrix = serverResponse.matrix;
        string sourceName = "response.matrix";

        if (sourceMatrix == null || sourceMatrix.Count == 0)
        {
            sourceMatrix = serverResponse.payload?.reels;
            sourceName = "response.payload.reels";
        }

        if (sourceMatrix == null || sourceMatrix.Count == 0)
        {
            error = "Spin response contains neither response.matrix nor response.payload.reels.";
            return false;
        }

        if (sourceMatrix.Count != expectedRows)
        {
            error = $"{sourceName} has {sourceMatrix.Count} rows; expected {expectedRows}.";
            return false;
        }

        var convertedMatrix = new List<List<int>>(expectedColumns);
        for (int column = 0; column < expectedColumns; column++)
        {
            convertedMatrix.Add(new List<int>(expectedRows));
        }

        for (int row = 0; row < expectedRows; row++)
        {
            List<string> sourceRow = sourceMatrix[row];
            if (sourceRow == null)
            {
                error = $"{sourceName} row {row} is null.";
                return false;
            }

            if (sourceRow.Count != expectedColumns)
            {
                error = $"{sourceName} row {row} has {sourceRow.Count} columns; expected {expectedColumns}.";
                return false;
            }

            for (int column = 0; column < expectedColumns; column++)
            {
                string symbolValue = sourceRow[column];
                if (string.IsNullOrWhiteSpace(symbolValue) ||
                    !int.TryParse(symbolValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int symbolId))
                {
                    error = $"{sourceName}[{row}][{column}] contains invalid symbol ID '{symbolValue ?? "null"}'.";
                    return false;
                }

                convertedMatrix[column].Add(symbolId);
            }
        }

        resultMatrix = convertedMatrix;
        return true;
    }

    /// <summary>
    /// CRITICAL: Converts server response to client SpinResult
    /// Handles string-to-int conversion, matrix transposition, and wild multiplier mapping
    /// Server sends [row][col], while SlotView needs [column][row].
    /// </summary>
    internal static SpinResult ConvertServerResponseToSpinResult(
        ServerSpinResponse serverResponse,
        double currentBalance,
        double totalBetAmount,
        int currentBetIndex,
        StPatricksGoldGameConfig gameConfig)
    {
        double totalWinAmount = 0;
        if (serverResponse.payload != null)
        {
            totalWinAmount = serverResponse.payload.winAmount > 0 ? serverResponse.payload.winAmount : serverResponse.payload.totalWin;
        }

        double newBalance = serverResponse.player?.balance ??
                            CalculateNewBalance(currentBalance, totalBetAmount, totalWinAmount);

        List<ServerWinLine> winsSource = null;
        if (serverResponse.payload?.lineWins != null && serverResponse.payload.lineWins.Count > 0)
        {
            winsSource = serverResponse.payload.lineWins;
        }
        else if (serverResponse.payload?.winningLines != null)
        {
            winsSource = serverResponse.payload.winningLines;
        }

        if (!TryConvertServerMatrix(serverResponse, gameConfig, out List<List<int>> convertedMatrix, out string matrixError))
        {
            UnityEngine.Debug.LogError($"[GameDataConverter] {matrixError}");
        }

        var result = new SpinResult
        {
            resultMatrix = convertedMatrix,

            // Map totalWin to winAmount
            winAmount = totalWinAmount,

            // Convert winningLines to winLines
            winLines = ConvertWinningLines(winsSource, gameConfig),

            // Update player data — use server balance directly
            playerData = new PlayerData
            {
                balance = newBalance,
                currentBetIndex = currentBetIndex
            },

            // Convert scatter data
            scatterData = (serverResponse.payload != null && serverResponse.payload.scatterTriggered)
                ? new ScatterData
                {
                    isTriggered = true,
                    scatterCount = serverResponse.payload.scatterCount,
                    winAmount = 0 // Calculate if needed
                }
                : null
        };

        return result;
    }


    private static List<List<int>> ConvertReelsToMatrix(List<List<string>> serverReels, List<ServerWinLine> winningLines, Dictionary<string, int> stickyWilds, StPatricksGoldGameConfig gameConfig)
    {
        int rows = gameConfig != null ? gameConfig.rowCount : StPatricksGoldDefinition.RowCount;
        int cols = gameConfig != null ? gameConfig.reelCount : StPatricksGoldDefinition.ReelCount;

        if (serverReels == null || serverReels.Count != rows)
        {
            UnityEngine.Debug.LogError($"Invalid server reels: expected {rows} rows, got {serverReels?.Count}");
            return null;
        }

        // Build wild multiplier lookup: [col][row] -> multiplier
        var wildMultipliers = new Dictionary<string, int>();

        // 1. Add winning line wild details (format explicit col, row)
        if (winningLines != null)
        {
            foreach (var line in winningLines)
            {
                if (line.wildDetails != null)
                {
                    foreach (var wild in line.wildDetails)
                    {
                        string key = $"{wild.col}_{wild.row}";
                        wildMultipliers[key] = wild.multiplier;
                    }
                }
            }
        }

        // 2. Add sticky wilds (format row_col) - these override winningLines if they overlap
        // to ensure the authoritative sticky multiplier is used (e.g. 3x instead of 1x)
        if (stickyWilds != null)
        {
            foreach (var kvp in stickyWilds)
            {
                string[] parts = kvp.Key.Split('_');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int row) &&
                    int.TryParse(parts[1], out int col))
                {
                    // Convert row_col to col_row for lookup
                    string key = $"{col}_{row}";
                    wildMultipliers[key] = kvp.Value;
                }
            }
        }

        var matrix = new List<List<int>>();

        // Transpose: iterate by columns
        for (int col = 0; col < cols; col++)
        {
            var column = new List<int>();

            // Each column has rows
            for (int row = 0; row < rows; row++)
            {
                if (col >= serverReels[row].Count)
                {
                    UnityEngine.Debug.LogError($"Invalid server data at row {row}, col {col}");
                    return null;
                }

                string symbolStr = serverReels[row][col];

                if (!int.TryParse(symbolStr, out int symbolId))
                {
                    UnityEngine.Debug.LogError($"Failed to parse symbol: {symbolStr}");
                    return null;
                }

                // Check if this is a wild with multiplier
                if (symbolId == gameConfig.wildSymbolId)
                {
                    string key = $"{col}_{row}";
                    if (wildMultipliers.TryGetValue(key, out int multiplier))
                    {
                        // Map wild multiplier to correct symbol ID
                        symbolId = GetWildSymbolIdForMultiplier(multiplier, gameConfig);
                    }
                }

                column.Add(symbolId);
            }

            matrix.Add(column);
        }

        return matrix;
    }

    /// <summary>
    /// Maps wild multiplier to correct symbol ID
    /// 1x → 11 (Wild), 2x → 13 (Wild2x), 3x → 14 (Wild3x), 5x → 15 (Wild5x)
    /// </summary>
    private static int GetWildSymbolIdForMultiplier(int multiplier, StPatricksGoldGameConfig gameConfig)
    {
        return gameConfig != null ? gameConfig.wildSymbolId : StPatricksGoldSymbolIds.Wild;
    }


    /// <summary>
    /// Converts server winningLines to client winLines.
    /// Supports server position strings such as "1,0".
    /// Encodes as flat index = row * cols + col.
    /// </summary>
    private static List<WinLine> ConvertWinningLines(List<ServerWinLine> serverWinLines, StPatricksGoldGameConfig gameConfig)
    {
        var winLines = new List<WinLine>();

        if (serverWinLines == null) return winLines;

        int cols = gameConfig != null ? gameConfig.reelCount : StPatricksGoldDefinition.ReelCount;
        int rows = gameConfig != null ? gameConfig.rowCount : StPatricksGoldDefinition.RowCount;

        foreach (var serverLine in serverWinLines)
        {
            // Try parsing symbolId as int first, fall back to resolving symbolName
            int symbolId = 0;
            if (!string.IsNullOrEmpty(serverLine.symbolId) && int.TryParse(serverLine.symbolId, out int parsedId))
            {
                symbolId = parsedId;
            }
            else if (!string.IsNullOrEmpty(serverLine.symbolName) && gameConfig?.symbols != null)
            {
                // Resolve symbolName (e.g. "Any 7") to an id by matching name
                var match = gameConfig.symbols.Find(s => string.Equals(s.name, serverLine.symbolName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    symbolId = match.id;
                }
                else if (serverLine.symbolName.StartsWith("Any", StringComparison.OrdinalIgnoreCase))
                {
                    // "Any 7" or "Any Bar" are mixed combinations, which don't map to a single symbol ID.
                    // This is expected, so we default to 0 and do not log a warning.
                    symbolId = 0;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[ConvertWinningLines] Unknown symbolName: {serverLine.symbolName}");
                }
            }

            var flatPositions = new List<int>();

            if (serverLine.normalizedPositions != null && serverLine.normalizedPositions.Count > 0)
            {
                foreach (List<int> coordinate in serverLine.normalizedPositions)
                {
                    if (coordinate == null || coordinate.Count < 2) continue;

                    int row = coordinate[0];
                    int col = coordinate[1];
                    if (row < 0 || row >= rows || col < 0 || col >= cols) continue;

                    int flatIndex = row * cols + col;
                    if (!flatPositions.Contains(flatIndex))
                    {
                        flatPositions.Add(flatIndex);
                    }
                }
            }
            else if (serverLine.positions != null && serverLine.positions.Count > 0)
            {
                // Server positions are coordinate strings.
                foreach (var posStr in serverLine.positions)
                {
                    string[] parts = posStr.Split(',');
                    if (parts.Length >= 2 &&
                        int.TryParse(parts[0], out int row) &&
                        int.TryParse(parts[1], out int col))
                    {
                        if (row >= 0 && row < rows && col >= 0 && col < cols)
                        {
                            int flatIndex = row * cols + col;
                            if (!flatPositions.Contains(flatIndex))
                            {
                                flatPositions.Add(flatIndex);
                            }
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[ConvertWinningLines] Failed to parse position string: {posStr}");
                    }
                }
            }
            else
            {
                // Fallback: derive from payline definition + matchCount if positions missing
                UnityEngine.Debug.LogWarning($"[ConvertWinningLines] No positions from server for lineIndex {serverLine.lineIndex}, falling back to payline table");
                if (gameConfig?.paylines != null &&
                    serverLine.lineIndex >= 0 &&
                    serverLine.lineIndex < gameConfig.paylines.Count)
                {
                    var payline = gameConfig.paylines[serverLine.lineIndex];
                    for (int col = 0; col < serverLine.matchCount && col < payline.Count; col++)
                    {
                        int row = payline[col];
                        flatPositions.Add(row * cols + col);
                    }
                }
            }

            // Some older/reskinned responses contain fewer coordinates than
            // matchCount. Complete only the missing matched positions from the
            // configured payline so every symbol that contributed is animated.
            int expectedMatchCount = Math.Min(cols, Math.Max(0, serverLine.matchCount));
            if (expectedMatchCount > 0 && flatPositions.Count > expectedMatchCount)
            {
                // Some responses send all five coordinates that draw the
                // payline. Only the first matchCount positions contributed to
                // the left-to-right win and should receive a symbol pulse.
                flatPositions = flatPositions.Take(expectedMatchCount).ToList();
            }

            if (flatPositions.Count < expectedMatchCount &&
                gameConfig?.paylines != null &&
                serverLine.lineIndex >= 0 &&
                serverLine.lineIndex < gameConfig.paylines.Count)
            {
                List<int> payline = gameConfig.paylines[serverLine.lineIndex];
                for (int col = 0; col < expectedMatchCount && col < payline.Count; col++)
                {
                    int row = payline[col];
                    if (row < 0 || row >= rows) continue;

                    int flatIndex = row * cols + col;
                    if (!flatPositions.Contains(flatIndex))
                    {
                        flatPositions.Add(flatIndex);
                    }
                }
            }

            // Use winAmount if available, fallback to payout
            double lineWin = serverLine.winAmount > 0 ? serverLine.winAmount : serverLine.payout;

            winLines.Add(new WinLine
            {
                lineId = serverLine.lineIndex,
                symbolId = symbolId,
                positions = flatPositions,
                winAmount = lineWin
            });
        }

        return winLines;
    }

    private static double CalculateNewBalance(double currentBalance, double totalBetAmount, double winAmount)
    {
        return currentBalance - totalBetAmount + winAmount;
    }
}

#endregion
