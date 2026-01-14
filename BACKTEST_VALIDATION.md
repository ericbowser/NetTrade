# Backtest Validation Guide

## Overview
This document provides test scenarios to validate backtest accuracy before connecting to live/paper trading accounts.

## Critical Items to Validate

### 1. Grid Trading Strategy

#### Test Case 1: Simple Buy-Sell Round Trip
**Setup:**
- Initial Capital: $1,000
- Grid Levels: 3
- Grid Range: 5%
- Order Size: $100
- Symbol: BTC/USD
- Date Range: Recent 7 days

**Expected Behavior:**
- If price oscillates within grid range, should execute buy-sell pairs
- Each sell should be at a higher price than corresponding buy
- PnL per trade = (SellPrice - BuyPrice) × Quantity
- Final Equity = Initial Capital + Sum of all PnLs + (UnrealizedAssets × CurrentPrice)

**Validation Steps:**
1. Run backtest with above parameters
2. Check trade history - verify alternating buy/sell orders
3. For each sell trade with PnL:
   - Verify: Sell Price > Buy Price
   - Calculate expected PnL = (Sell - Buy) × (OrderSize / BuyPrice)
   - Compare with displayed PnL (should match within $0.01)
4. Verify Win Rate:
   - Count sells with PnL > 0
   - Win Rate = (Profitable Sells / Total Sells) × 100
5. Verify Final Equity:
   - Sum all sell trade PnLs
   - Add value of any remaining assets
   - Should equal: InitialCapital + TotalProfit

#### Test Case 2: Trending Market (Up)
**Setup:**
- Same as Test Case 1
- Date range with known upward trend

**Expected Behavior:**
- Mostly buy orders at lower levels
- Few sell orders (price doesn't retrace)
- Win Rate close to 100% (all sells above buys)
- Final Equity includes significant unrealized gains in held assets

**Validation:**
- Check that Buy trades > Sell trades
- Unrealized P&L should be positive
- Win Rate should be high (90-100%)

#### Test Case 3: Ranging Market
**Setup:**
- Same as Test Case 1
- Date range with sideways price action

**Expected Behavior:**
- Multiple buy-sell pairs
- Win Rate should be 100% (grid always profits on range)
- Total Trades should be higher than trending market
- Profit should come from multiple small wins

**Validation:**
- Buy count ≈ Sell count
- All sells should have PnL > 0
- Win Rate = 100%

### 2. Scalping Strategy

#### Test Case 4: Basic Long Trade
**Setup:**
- Initial Capital: $1,000
- Risk Per Trade: 2%
- Take Profit: 2%
- Stop Loss: 1%
- Symbol: BTC/USD
- Timeframe: 1Min
- Date Range: Recent 30 days

**Expected Behavior:**
- Trades trigger on MACD/SMA signals
- Each trade risks 2% of capital ($20 position size)
- Win: Close at +2% from entry
- Loss: Close at -1% from entry

**Validation Steps:**
1. For each completed trade:
   - Entry Price should match close price at signal candle
   - Position Size = Capital × 0.02 / EntryPrice
   - If WIN: ExitPrice ≥ EntryPrice × 1.02
   - If LOSS: ExitPrice ≤ EntryPrice × 0.99
2. Verify PnL calculation:
   - For LONG: PnL = (ExitPrice - EntryPrice) × Size
   - For SHORT: PnL = (EntryPrice - ExitPrice) × Size
3. Verify Win Rate:
   - Count trades with PnL > 0
   - Win Rate = (Wins / TotalTrades) × 100
4. Verify Equity Curve:
   - Should be monotonic (never go negative)
   - Each trade's equity should equal previous equity + PnL

#### Test Case 5: Risk Management
**Setup:**
- Initial Capital: $1,000
- Risk Per Trade: 1%
- Run backtest with known loss scenario

**Expected Behavior:**
- Each losing trade should lose exactly 1% of current capital
- After 10 consecutive losses at 1% each:
  - Final Capital ≈ $1,000 × (0.99)^10 ≈ $904.38
- Account should never go below $0

**Validation:**
- Verify no single trade loses more than Risk Per Trade %
- Verify equity never goes negative
- Check that position sizing adjusts with capital changes

### 3. Manual Calculation Verification

#### Example Trade Walkthrough (Grid)
```
Initial Capital: $1,000
Grid Level 0: Buy at $95 for $100
Grid Level 1: Sell at $100 for proceeds

Step 1: Buy at $95
- Spent: $100
- Assets Acquired: $100 / $95 = 1.0526 BTC
- Capital: $1,000 - $100 = $900
- Equity: $900 + (1.0526 × $95) = $1,000

Step 2: Sell at $100
- Assets Sold: 1.0526 BTC
- Proceeds: 1.0526 × $100 = $105.26
- PnL: $105.26 - $100 = $5.26
- Capital: $900 + $105.26 = $1,005.26
- Assets: 0 BTC
- Equity: $1,005.26

Verify in backtest:
- Trade 1 (Buy): PnL = $0, Equity ≈ $1,000
- Trade 2 (Sell): PnL = $5.26, Equity = $1,005.26
- Win Rate: 100% (1 winning sell / 1 total sell)
```

#### Example Trade Walkthrough (Scalping)
```
Initial Capital: $1,000
Risk Per Trade: 2% = $20 position
Entry: $100
Take Profit: 2% = $102
Stop Loss: 1% = $99

Winning Trade:
- Entry Price: $100
- Position Size: $20 / $100 = 0.2 BTC
- Exit Price: $102 (take profit hit)
- PnL: ($102 - $100) × 0.2 = $0.40
- PnL %: 2%
- New Capital: $1,000 + $0.40 = $1,000.40

Losing Trade:
- Entry Price: $100
- Position Size: $20 / $100 = 0.2 BTC
- Exit Price: $99 (stop loss hit)
- PnL: ($99 - $100) × 0.2 = -$0.20
- PnL %: -1%
- New Capital: $1,000 - $0.20 = $999.80

Verify in backtest:
- Winning trade should show PnL ≈ $0.40, Result = "WIN"
- Losing trade should show PnL ≈ -$0.20, Result = "LOSS"
- Equity curve should match cumulative PnL
```

## Red Flags to Watch For

### Critical Issues (DO NOT TRADE if you see these):
1. **Negative Equity**: Equity should never go below $0
2. **PnL Mismatch**: Manual calculation ≠ displayed PnL (off by > $0.10)
3. **Win Rate > 100%** or **< 0%**: Mathematical impossibility
4. **Trades Without Prices**: Entry/Exit prices missing or zero
5. **Infinite Profits**: Unrealistic returns (e.g., 1000%+ in a day)
6. **Equity Jumps**: Large unexplained jumps in equity curve

### Warning Signs (Investigate before trading):
1. **Win Rate Seems Too High**: 95%+ win rate may indicate optimization bias
2. **No Losing Trades**: Every strategy has losses
3. **Equity Not Smooth**: Chart should be relatively continuous
4. **Total Trades = 0**: Strategy may not be triggering
5. **Final Equity = Initial Capital**: No trades executed

## Pre-Trading Checklist

Before connecting to paper/live account:

- [ ] Run Grid strategy backtest - verify at least 3 test cases above
- [ ] Run Scalping strategy backtest - verify at least 2 test cases above
- [ ] Manually calculate PnL for 3 random trades - matches displayed PnL
- [ ] Verify Win Rate calculation matches manual count
- [ ] Check equity curve looks reasonable (no huge jumps)
- [ ] Verify Final Equity = Initial Capital + Sum(All PnLs) + Unrealized Gains
- [ ] Test with small capital first ($100) to limit risk
- [ ] Run 7-day backtest to verify recent performance
- [ ] Run 30-day backtest to verify longer-term performance
- [ ] Compare backtest results with actual market data if available

## Known Limitations

1. **No Slippage**: Backtest assumes perfect fills at exact grid prices
2. **No Fees**: Real trading has exchange fees (~0.1-0.5% per trade)
3. **No Latency**: Assumes instant order execution
4. **Perfect Data**: Uses clean historical data without gaps
5. **Grid Assumptions**: Assumes grid levels can always be filled

**Adjust Expectations for Live Trading:**
- Reduce expected profits by 10-20% for fees and slippage
- Win rate may be slightly lower due to market conditions
- Use paper trading for at least 1 week before going live

## Recommended Starting Parameters (Paper Trading)

### Grid Strategy:
- Initial Capital: $100-500
- Grid Levels: 5-7
- Grid Range: 3-5%
- Order Size: $50-100
- Monitor for: 3-7 days

### Scalping Strategy:
- Initial Capital: $100-500
- Risk Per Trade: 0.5-1% (conservative)
- Take Profit: 1.5-2%
- Stop Loss: 0.5-1%
- Monitor for: 7-14 days

## Questions to Ask Before Going Live

1. Do I understand why each trade was made?
2. Can I explain the PnL for any given trade?
3. Have I verified the calculations manually?
4. Am I comfortable with the risk per trade?
5. Do I have a plan for when things go wrong?
6. Have I tested with paper trading first?

---

**IMPORTANT**: If you cannot verify the backtest calculations manually or see any red flags, DO NOT proceed with live/paper trading. Debug the issues first or seek additional review.
