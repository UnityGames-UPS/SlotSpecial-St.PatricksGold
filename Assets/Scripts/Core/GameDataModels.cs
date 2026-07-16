using System;
using System.Collections.Generic;
using System.Linq;

#region Server Communication Models

[Serializable]
public class Features
{
    public int totalLines;
    public UltraWheel ultraWheel;
    public ScatterWheel scatterWheel;
    public TempleRiches templeRiches;
    public WildMultiplier wildMultiplier;
    public LowSymbolAnyPay lowSymbolAnyPay;
}

[Serializable]
public class LowSymbolAnyPay
{
    public bool enabled;
    public int payout3x;
    public int payout4x;
    public int payout5x;
    public List<int> lowSymbolIds;
}

[Serializable]
public class Matrix
{
    public int x;
    public int y;
}

[Serializable]
public class ReelsInstance
{
    public int _0;
    public int _1;
    public int _2;
    public int _3;
    public int _4;
}

[Serializable]
public class Root
{
    public string id;
    public List<double> bets;
    public List<List<int>> lines;
    public Matrix matrix;
    public List<Symbol> symbols;
    public Features features;
    public bool isSpecial;
}

[Serializable]
public class ScatterWheel
{
    public List<Wheel> wheels;
    public bool enabled;
    public int minTriggerCount;
}

[Serializable]
public class Symbol
{
    public int id;
    public string name;
    public string group;
    public List<int> multiplier;
    public string description;
    public ReelsInstance reelsInstance;
}

[Serializable]
public class TempleRiches
{
    public bool enabled;
    public int multiplier;
    public List<int> excludedSymbols;
}

[Serializable]
public class UltraWheel
{
    public bool enabled;
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
public class Wheel
{
    public List<int> awards;
    public int wheelId;
    public List<double> awardProbs;
}

[Serializable]
public class WildMultiplier
{
    public bool enabled;
    public List<int> multipliers;
    public List<int> excludedSymbols;
    public List<double> multipliersProb;
    public int maxCumulativeMultiplier;
}

// ============================================================================
// FIXED: Server Response Models - Must match actual server JSON structure
// ============================================================================

[Serializable]
public class ServerSpinResponse
{
    public string id = "ResultData";
    public bool success;
    public List<List<string>> matrix;
    public ServerPlayerBalance player;
    public ServerPayload payload;
    public ServerFeaturesResult features;
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
    public List<ServerWinLine> lineWins;     // Server uses "lineWins" in Diamond Rose
    public double winAmount;                 // Server uses "winAmount" in Diamond Rose
    public int scatterCount;
    public bool scatterTriggered;
    public ServerFreeSpinState freeSpinState; // Can be null
    public bool isRoundOver;                 // True when free spin round is over
    public double totalRoundWin;             // Total round win (at payload level when isRoundOver)
}

[Serializable]
public class ServerFreeSpinState
{
    public bool isActive;
    public int spinsRemaining;
    public int spinsUsed;
    public double totalRoundWin;
    public bool isBought;
    public Dictionary<string, int> stickyWilds;
}

[Serializable]
public class ServerWinLine
{
    public int lineIndex;                    // Server uses "lineIndex"
    public List<string> positions;           // Diamond Rose format: ["1,0", "0,1", "1,2"] (col,row strings)
    public string symbolId;                  // Server sends STRING!
    public string symbolName;                // Diamond Rose sends "Any 7" etc.
    public int matchCount;
    public double basePayout;
    public double payout;
    public double winAmount;                 // Diamond Rose sends winAmount per line
    public int wildMultiplier;
    public List<WildDetail> wildDetails;
}

[Serializable]
public class WildDetail
{
    public int col;
    public int row;
    public int multiplier;
}

[Serializable]
public class ServerFeaturesResult
{
    public ServerFreeSpinResult freeSpins;
}

[Serializable]
public class ServerFreeSpinResult
{
    public bool triggered;
    public int spinsAwarded;
    public bool isFreeSpin;
    public bool isRoundOver;
    public int spinsRemaining;
    public int spinsUsed;  // Added: Server sends this in features.freeSpins
    public int stickyWildsCount;
    public ServerOverlayScatter overlayScatter;
}

[Serializable]
public class ServerOverlayScatter
{
    public bool isTriggered;
    public int count;
    public int extraSpins;
    public List<List<int>> positions;
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
public class GameConfig
{
    public int reelCount = 5;
    public int rowCount = 4;
    public int symbolCount = 13;
    public int paylineCount = 40;
    public List<List<int>> paylines;
    public List<double> availableBets;
    public List<SymbolInfo> symbols;

    // Wild configuration
    public int wildSymbolId = 11;      // Base wild (1x)
    public int wild2xSymbolId = 13;     // Wild 2x multiplier
    public int wild3xSymbolId = 14;     // Wild 3x multiplier
    public int wild5xSymbolId = 15;     // Wild 5x multiplier
    public List<int> wildMultipliers = new List<int> { 1, 2, 3, 5 };

    // Scatter configuration
    public int scatterSymbolId = 12;



    public int betMultiplier = 100;
    public bool isSpecial;
    public UltraWheel ultraWheel;
    public ScatterWheel scatterWheel;
    public TempleRiches templeRiches;
    public WildMultiplier wildMultiplierFeature;
    public LowSymbolAnyPay lowSymbolAnyPay;
}

[Serializable]
public class SymbolInfo
{
    public int id;
    public string name;
    public string group;
    public string description;
    public ReelsInstance reelsInstance;
    public List<double> multipliers;
    public bool isWild;
    public bool isScatter;
    public int wildMultiplier;
    public double payout;
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
    public FreeSpinData freeSpinData;
    public ScatterData scatterData;
    public OverlayScatterData overlayScatterData;
    public Dictionary<string, int> stickyWilds;

    // Server-authoritative free spin state
    public int serverSpinsRemaining;
    public int serverSpinsUsed;
    public double serverTotalRoundWin;
    public bool isRoundOver;
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
public class FreeSpinData
{
    public bool isTriggered;
    public int spinsAwarded;
    public int remainingSpins;
    public bool isBought;
}

[Serializable]
public class ScatterData
{
    public bool isTriggered;
    public int scatterCount;
    public double winAmount;
}

[Serializable]
public class OverlayScatterData
{
    public bool isTriggered;
    public int count;
    public int extraSpins;
    public List<List<int>> positions;
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
    ShowingWin,
    FreeSpinMode
}

public enum SpinSpeed
{
    Normal,
    Turbo,
    QuickSpin
}

#endregion

#region Helper Classes for Conversion

/// <summary>
/// Converts server data to client GameConfig
/// </summary>
public static class GameDataConverter
{
    internal static GameConfig ConvertToGameConfig(Root serverData)
    {
        int reelCount = serverData.matrix != null && serverData.matrix.x > 0 ? serverData.matrix.x : 3;
        int rowCount = serverData.matrix != null && serverData.matrix.y > 0 ? serverData.matrix.y : InferRowCount(serverData.lines);

        var config = new GameConfig
        {
            reelCount = reelCount,
            rowCount = rowCount,
            symbolCount = serverData.symbols != null ? serverData.symbols.Count : 0,
            paylineCount = ResolvePaylineCount(serverData),
            paylines = serverData.lines ?? new List<List<int>>(),
            availableBets = serverData.bets ?? new List<double>(),
            symbols = new List<SymbolInfo>(),
            isSpecial = serverData.isSpecial
        };

        if (serverData.symbols != null)
        {
            foreach (var serverSymbol in serverData.symbols)
            {
                var symbolInfo = new SymbolInfo
                {
                    id = serverSymbol.id,
                    name = serverSymbol.name,
                    group = serverSymbol.group,
                    description = serverSymbol.description,
                    reelsInstance = serverSymbol.reelsInstance,
                    multipliers = serverSymbol.multiplier != null
                        ? serverSymbol.multiplier.Select(value => (double)value).ToList()
                        : new List<double>(),
                    isWild = IsSymbolType(serverSymbol, "wild"),
                    isScatter = IsSymbolType(serverSymbol, "scatter"),
                    wildMultiplier = 1,
                    payout = 0
                };

                config.symbols.Add(symbolInfo);

                if (symbolInfo.isWild)
                {
                    config.wildSymbolId = symbolInfo.id;
                }

                if (symbolInfo.isScatter)
                {
                    config.scatterSymbolId = symbolInfo.id;
                }
            }
        }

        config.betMultiplier = config.paylineCount > 0 ? config.paylineCount : 1;

        if (serverData.features != null)
        {
            if (serverData.features.totalLines > 0)
            {
                config.paylineCount = serverData.features.totalLines;
                config.betMultiplier = serverData.features.totalLines;
            }

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

    internal static PlayerData ConvertToPlayerData(Root serverData, int defaultBetIndex = 0, double defaultBalance = 0)
    {
        return new PlayerData
        {
            balance = defaultBalance,
            currentBetIndex = defaultBetIndex
        };
    }

    private static int ResolvePaylineCount(Root serverData)
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
            return 3;
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

    private static bool IsSymbolType(Symbol symbol, string typeName)
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
    /// CRITICAL: Converts server response to client SpinResult
    /// Handles string-to-int conversion, matrix transposition, and wild multiplier mapping
    /// Server sends [row][col] (4 rows x 5 cols), Client needs [col][row] (5 cols x 4 rows)
    /// </summary>
    internal static SpinResult ConvertServerResponseToSpinResult(ServerSpinResponse serverResponse, double currentBalance, double betAmount, GameConfig gameConfig)
    {
        double totalWinAmount = 0;
        if (serverResponse.payload != null)
        {
            totalWinAmount = serverResponse.payload.winAmount > 0 ? serverResponse.payload.winAmount : serverResponse.payload.totalWin;
        }

        double newBalance = serverResponse.player?.balance ?? CalculateNewBalance(currentBalance, betAmount, totalWinAmount);

        // Get server free spin state values
        int spinsRemaining = serverResponse.features?.freeSpins?.spinsRemaining ?? serverResponse.payload?.freeSpinState?.spinsRemaining ?? 0;
        int spinsUsed = serverResponse.features?.freeSpins?.spinsUsed ?? serverResponse.payload?.freeSpinState?.spinsUsed ?? 0;
        double totalRoundWin = (serverResponse.payload != null && serverResponse.payload.totalRoundWin > 0)
            ? serverResponse.payload.totalRoundWin
            : (serverResponse.payload?.freeSpinState?.totalRoundWin ?? 0);
        bool isRoundOver = serverResponse.features?.freeSpins?.isRoundOver ?? (serverResponse.payload != null && serverResponse.payload.isRoundOver);

        var stickyWilds = serverResponse.payload?.freeSpinState?.stickyWilds;

        var reelsSource = serverResponse.matrix ?? serverResponse.payload?.reels;
        var winsSource = serverResponse.payload?.lineWins ?? serverResponse.payload?.winningLines;

        var result = new SpinResult
        {
            // Convert and transpose reels from server format to client format
            resultMatrix = ConvertReelsToMatrix(reelsSource, winsSource, stickyWilds, gameConfig),

            // Map totalWin to winAmount
            winAmount = totalWinAmount,

            // Convert winningLines to winLines
            winLines = ConvertWinningLines(winsSource, gameConfig),

            // Update player data — use server balance directly
            playerData = new PlayerData
            {
                balance = newBalance,
                currentBetIndex = 0 // Will be set by GameManager
            },

            // Convert free spin data
            freeSpinData = serverResponse.features?.freeSpins != null && serverResponse.features.freeSpins.triggered
                ? new FreeSpinData
                {
                    isTriggered = true,
                    spinsAwarded = serverResponse.features.freeSpins.spinsAwarded,
                    remainingSpins = 0,
                    isBought = serverResponse.payload?.freeSpinState?.isBought ?? false
                }
                : null,

            // Convert scatter data
            scatterData = (serverResponse.payload != null && serverResponse.payload.scatterTriggered)
                ? new ScatterData
                {
                    isTriggered = true,
                    scatterCount = serverResponse.payload.scatterCount,
                    winAmount = 0 // Calculate if needed
                }
                : null,

            overlayScatterData = serverResponse.features?.freeSpins?.overlayScatter != null && serverResponse.features.freeSpins.overlayScatter.isTriggered
                ? new OverlayScatterData
                {
                    isTriggered = true,
                    count = serverResponse.features.freeSpins.overlayScatter.count,
                    extraSpins = serverResponse.features.freeSpins.overlayScatter.extraSpins,
                    positions = serverResponse.features.freeSpins.overlayScatter.positions
                }
                : null,

            stickyWilds = serverResponse.payload?.freeSpinState?.stickyWilds,

            // Server-authoritative free spin state
            serverSpinsRemaining = spinsRemaining,
            serverSpinsUsed = spinsUsed,
            serverTotalRoundWin = totalRoundWin,
            isRoundOver = isRoundOver
        };

        return result;
    }


    private static List<List<int>> ConvertReelsToMatrix(List<List<string>> serverReels, List<ServerWinLine> winningLines, Dictionary<string, int> stickyWilds, GameConfig gameConfig)
    {
        int rows = gameConfig != null ? gameConfig.rowCount : 3;
        int cols = gameConfig != null ? gameConfig.reelCount : 3;

        if (serverReels == null || serverReels.Count != rows)
        {
            UnityEngine.Debug.LogError($"Invalid server reels: expected {rows} rows, got {serverReels?.Count}");
            return GenerateDefaultMatrix(rows, cols);
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
                    column.Add(0);
                    continue;
                }

                string symbolStr = serverReels[row][col];

                if (!int.TryParse(symbolStr, out int symbolId))
                {
                    UnityEngine.Debug.LogError($"Failed to parse symbol: {symbolStr}");
                    column.Add(0);
                    continue;
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
    private static int GetWildSymbolIdForMultiplier(int multiplier, GameConfig gameConfig)
    {
        if (gameConfig == null)
        {
            return 11;
        }

        int mappedSymbolId = multiplier switch
        {
            1 => gameConfig.wildSymbolId,
            2 => gameConfig.wild2xSymbolId,
            3 => gameConfig.wild3xSymbolId,
            5 => gameConfig.wild5xSymbolId,
            _ => gameConfig.wildSymbolId
        };

        return HasSymbolId(gameConfig, mappedSymbolId) ? mappedSymbolId : gameConfig.wildSymbolId;
    }

    private static bool HasSymbolId(GameConfig gameConfig, int symbolId)
    {
        return gameConfig.symbols != null && gameConfig.symbols.Any(symbol => symbol.id == symbolId);
    }


    private static List<List<int>> GenerateDefaultMatrix(int rows, int cols)
    {
        var matrix = new List<List<int>>();
        for (int col = 0; col < cols; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < rows; row++)
            {
                column.Add(0);
            }
            matrix.Add(column);
        }
        return matrix;
    }

    /// <summary>
    /// Converts server winningLines to client winLines.
    /// Diamond Rose format: positions are strings like "1,0" (col,row).
    /// Encodes as flat index = row * cols + col.
    /// </summary>
    private static List<WinLine> ConvertWinningLines(List<ServerWinLine> serverWinLines, GameConfig gameConfig)
    {
        var winLines = new List<WinLine>();

        if (serverWinLines == null) return winLines;

        int cols = gameConfig != null ? gameConfig.reelCount : 3;

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

            if (serverLine.positions != null && serverLine.positions.Count > 0)
            {
                // Diamond Rose format: positions are "row,col" strings
                foreach (var posStr in serverLine.positions)
                {
                    string[] parts = posStr.Split(',');
                    if (parts.Length >= 2 &&
                        int.TryParse(parts[0], out int row) &&
                        int.TryParse(parts[1], out int col))
                    {
                        int flatIndex = row * cols + col;
                        flatPositions.Add(flatIndex);
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

    private static double CalculateNewBalance(double currentBalance, double betAmount, double winAmount)
    {
        return currentBalance - betAmount + winAmount;
    }
}

#endregion
