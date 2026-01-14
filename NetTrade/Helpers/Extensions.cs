using System;
using System.Collections.Generic;
using System.Linq;
using Alpaca.Markets;
using NetTrade.Models;

namespace NetTrade.Helpers;

public static class Extensions
{
    public static List<(DateTime Start, DateTime End)> CreateDateChunks(DateTime start, DateTime end, TimeSpan chunkSize)
    {
        var chunks = new List<(DateTime Start, DateTime End)>();
        DateTime chunkStart = start;

        while (chunkStart < end)
        {
            DateTime chunkEnd = chunkStart.Add(chunkSize);
            if (chunkEnd > end)
                chunkEnd = end;

            chunks.Add((chunkStart, chunkEnd));
            chunkStart = chunkEnd;
        }

        return chunks;
    }

    // Helper method to clone configuration
    public static GridTradingConfiguration CloneConfiguration(this GridTradingConfiguration original)
    {
        return new GridTradingConfiguration
        {
            Symbol = original.Symbol,
            Timeframe = original.Timeframe,
            GridLevels = original.GridLevels,
            GridRange = original.GridRange,
            OrderSize = original.OrderSize,
            // Don't copy dates as we'll set those per chunk
        };
    }


    // Modified backtest method to work with chunks
    public static (List<GridTrade> Trades, decimal Capital, decimal AssetHolding) BacktestGridStrategyChunk(
        this List<CandleData> candles,
        List<GridLevel> gridLevels,
        decimal initialCapital,
        decimal initialAssetHolding)
    {
        // Simulation variables
        decimal capital = initialCapital;
        decimal assetHolding = initialAssetHolding;
        var trades = new List<GridTrade>();

        // Track open positions: each buy creates a position that can be sold
        // Key: buy level, Value: (assets, buyPrice)
        var openPositions = new Dictionary<int, List<(decimal assets, decimal buyPrice)>>();
        
        // Track active buy orders per level (can have multiple if price oscillates)
        var activeBuyOrders = new Dictionary<int, int>(); // level -> count of active orders
        
        // Initialize active buy orders for all buy levels
        foreach (var level in gridLevels.Where(g => g.OrderSide == OrderSide.Buy))
        {
            activeBuyOrders[level.Level] = 1; // Start with one active order per buy level
        }

        // Run through each candle
        foreach (var candle in candles)
        {
            decimal currentPrice = candle.Close;

            // Check if any grid buy orders were filled (price went down crossing a grid level)
            foreach (var level in gridLevels.Where(g => g.OrderSide == OrderSide.Buy).ToList())
            {
                // Check if price crossed this buy level and we have an active order
                if (candle.Low <= level.Price && activeBuyOrders.ContainsKey(level.Level) && activeBuyOrders[level.Level] > 0)
                {
                    // Check if we have enough capital
                    if (capital < level.OrderSize)
                    {
                        continue; // Skip if insufficient capital
                    }

                    // Buy order executed
                    decimal spentCapital = level.OrderSize;
                    decimal boughtAssets = level.OrderSize / level.Price;

                    // Update balances
                    capital -= spentCapital;
                    assetHolding += boughtAssets;

                    // Track this position
                    if (!openPositions.ContainsKey(level.Level))
                    {
                        openPositions[level.Level] = new List<(decimal assets, decimal buyPrice)>();
                    }
                    openPositions[level.Level].Add((boughtAssets, level.Price));

                    // Deactivate this buy order (will be reactivated after a sell)
                    activeBuyOrders[level.Level] = 0;

                    // Record the trade
                    var trade = new GridTrade
                    {
                        GridLevel = level.Level,
                        Price = level.Price,
                        EntryPrice = level.Price,
                        ExitPrice = null, // Not yet sold
                        Size = level.OrderSize,
                        Direction = OrderSide.Buy,
                        Timestamp = candle.Timestamp,
                        PnL = 0, // Buy trades have no realized PnL yet
                        Equity = capital + (assetHolding * currentPrice)
                    };
                    trades.Add(trade);
                }
            }

            // Check if any grid sell orders were filled (price went up crossing a grid level)
            foreach (var level in gridLevels.Where(g => g.OrderSide == OrderSide.Sell).ToList())
            {
                // Check if price crossed this sell level
                if (candle.High >= level.Price)
                {
                    // Find matching buy positions at lower levels (lower prices)
                    // In a grid strategy, we sell at a higher level than where we bought
                    var matchingPositions = openPositions
                        .Where(kvp => kvp.Key < level.Level && kvp.Value.Count > 0)
                        .OrderBy(kvp => kvp.Key) // Sell oldest positions first (FIFO)
                        .ToList();

                    if (matchingPositions.Any())
                    {
                        // Sell one position per sell level per candle (most realistic)
                        var posEntry = matchingPositions.First();
                        var buyLevel = posEntry.Key;
                        var positions = posEntry.Value;
                        
                        if (positions.Count > 0)
                        {
                            var position = positions[0]; // Take first position (FIFO)
                            
                            // Check if we have enough assets to sell
                            if (assetHolding >= position.assets)
                            {
                                // Sell order executed
                                decimal soldAssets = position.assets;
                                decimal receivedCapital = soldAssets * level.Price;

                                // Calculate PnL for this sell
                                decimal pnl = (level.Price - position.buyPrice) * soldAssets;

                                // Update balances
                                capital += receivedCapital;
                                assetHolding -= soldAssets;

                                // Remove this position
                                positions.RemoveAt(0);
                                if (positions.Count == 0)
                                {
                                    openPositions.Remove(buyLevel);
                                }

                                // Reactivate the buy order at the buy level (grid can cycle)
                                if (activeBuyOrders.ContainsKey(buyLevel))
                                {
                                    activeBuyOrders[buyLevel] = 1;
                                }

                                // Determine result based on PnL
                                string result = pnl > 0 ? "WIN" : (pnl < 0 ? "LOSS" : string.Empty);
                                
                                // Record the trade
                                var trade = new GridTrade
                                {
                                    GridLevel = level.Level,
                                    Price = level.Price,
                                    EntryPrice = position.buyPrice, // The buy price from the matched position
                                    ExitPrice = level.Price, // The sell price
                                    Size = receivedCapital,
                                    Direction = OrderSide.Sell,
                                    Timestamp = candle.Timestamp,
                                    PnL = pnl,
                                    Equity = capital + (assetHolding * currentPrice),
                                    Result = result
                                };
                                trades.Add(trade);
                            }
                        }
                    }
                }
            }

            // Check for stop-loss exits: Force exit positions at a loss if price drops significantly
            // This makes the win rate more realistic by allowing losses
            const decimal stopLossPercent = 0.15m; // 15% stop-loss
            var positionsToClose = new List<(int buyLevel, int positionIndex, decimal exitPrice)>();
            
            foreach (var posEntry in openPositions.ToList())
            {
                var buyLevel = posEntry.Key;
                var positions = posEntry.Value;
                
                for (int posIdx = 0; posIdx < positions.Count; posIdx++)
                {
                    var position = positions[posIdx];
                    decimal lossPercent = (position.buyPrice - currentPrice) / position.buyPrice;
                    
                    // If price dropped more than stop-loss threshold, force exit at loss
                    if (lossPercent >= stopLossPercent && assetHolding >= position.assets)
                    {
                        positionsToClose.Add((buyLevel, posIdx, currentPrice));
                    }
                }
            }
            
            // Execute stop-loss exits (process in reverse order to avoid index issues)
            foreach (var (buyLevel, posIdx, exitPrice) in positionsToClose.OrderByDescending(x => x.buyLevel).ThenByDescending(x => x.positionIndex))
            {
                if (openPositions.ContainsKey(buyLevel) && openPositions[buyLevel].Count > posIdx)
                {
                    var position = openPositions[buyLevel][posIdx];
                    
                    if (assetHolding >= position.assets)
                    {
                        // Force exit at loss
                        decimal soldAssets = position.assets;
                        decimal receivedCapital = soldAssets * exitPrice;
                        decimal pnl = (exitPrice - position.buyPrice) * soldAssets;
                        
                        // Update balances
                        capital += receivedCapital;
                        assetHolding -= soldAssets;
                        
                        // Remove this position
                        openPositions[buyLevel].RemoveAt(posIdx);
                        if (openPositions[buyLevel].Count == 0)
                        {
                            openPositions.Remove(buyLevel);
                        }
                        
                        // Reactivate the buy order at the buy level
                        if (activeBuyOrders.ContainsKey(buyLevel))
                        {
                            activeBuyOrders[buyLevel] = 1;
                        }
                        
                        // Record the trade as a loss
                        var trade = new GridTrade
                        {
                            GridLevel = buyLevel,
                            Price = exitPrice,
                            EntryPrice = position.buyPrice,
                            ExitPrice = exitPrice,
                            Size = receivedCapital,
                            Direction = OrderSide.Sell,
                            Timestamp = candle.Timestamp,
                            PnL = pnl,
                            Equity = capital + (assetHolding * currentPrice),
                            Result = pnl > 0 ? "WIN" : (pnl < 0 ? "LOSS" : string.Empty)
                        };
                        trades.Add(trade);
                    }
                }
            }
            
            // Handle price moving outside grid range
            var buyLevels = gridLevels.Where(g => g.OrderSide == OrderSide.Buy).ToList();
            var sellLevels = gridLevels.Where(g => g.OrderSide == OrderSide.Sell).ToList();
            
            if (buyLevels.Any() && sellLevels.Any())
            {
                var lowestBuyLevel = buyLevels.OrderBy(g => g.Price).First();
                var highestSellLevel = sellLevels.OrderByDescending(g => g.Price).First();
                
                if (currentPrice < lowestBuyLevel.Price)
                {
                    // Price is below grid - ensure buy levels can still trigger if price recovers
                    // This is already handled by the activeBuyOrders logic
                }
                
                if (currentPrice > highestSellLevel.Price)
                {
                    // Price is above grid - positions can still be sold if price comes back down
                    // This is already handled by the openPositions logic
                }
            }
        }

        // Close all remaining open positions at the end of the backtest
        // This ensures we account for unrealized losses/gains
        if (candles.Any() && openPositions.Any())
        {
            decimal finalPrice = candles.Last().Close;
            
            foreach (var posEntry in openPositions.ToList())
            {
                var buyLevel = posEntry.Key;
                var positions = posEntry.Value;
                
                foreach (var position in positions.ToList())
                {
                    if (assetHolding >= position.assets)
                    {
                        // Close position at final price
                        decimal soldAssets = position.assets;
                        decimal receivedCapital = soldAssets * finalPrice;
                        decimal pnl = (finalPrice - position.buyPrice) * soldAssets;
                        
                        // Update balances
                        capital += receivedCapital;
                        assetHolding -= soldAssets;
                        
                        // Remove this position
                        positions.Remove(position);
                        if (positions.Count == 0)
                        {
                            openPositions.Remove(buyLevel);
                        }
                        
                        // Record the trade
                        var trade = new GridTrade
                        {
                            GridLevel = buyLevel,
                            Price = finalPrice,
                            EntryPrice = position.buyPrice,
                            ExitPrice = finalPrice,
                            Size = receivedCapital,
                            Direction = OrderSide.Sell,
                            Timestamp = candles.Last().Timestamp,
                            PnL = pnl,
                            Equity = capital + (assetHolding * finalPrice),
                            Result = pnl > 0 ? "WIN" : (pnl < 0 ? "LOSS" : string.Empty)
                        };
                        trades.Add(trade);
                    }
                }
            }
        }

        return (trades, capital, assetHolding);
    }

    public static string FormatSymbolForAlpaca(this string symbol)
    {
        // Alpaca API expects "BTC/USD" format (with slash)
        // Ensure the symbol has the correct format
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return symbol;
        }
        
        // If symbol doesn't have a slash, try to add one (e.g., "BTCUSD" -> "BTC/USD")
        if (!symbol.Contains("/"))
        {
            // Try to split into base and quote (assuming 3-letter base and 3-letter quote)
            if (symbol.Length >= 6)
            {
                string baseCurrency = symbol.Substring(0, 3);
                string quoteCurrency = symbol.Substring(3);
                return $"{baseCurrency}/{quoteCurrency}";
            }
        }
        
        // Return as-is if it already has the correct format
        return symbol;
    }
}