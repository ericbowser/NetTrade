#!/usr/bin/env python3
"""
MCP Server for CoinAPI - Alpaca Paper Trading and Backtest Analysis
This server provides tools to analyze backtest results and market data.
"""

import json
import asyncio
from typing import Any, Dict, List, Optional
import http.client
from mcp.server import Server
from mcp.types import Tool, TextContent

# Configuration
API_BASE_URL = "http://localhost:5229/api"

# Initialize MCP Server
server = Server("alpaca-coinapi")

@server.list_tools()
async def list_tools() -> List[Tool]:
    """List available tools for analyzing backtests and market data."""
    return [
        Tool(
            name="test_alpaca_connection",
            description="Test connection to Alpaca Paper Trading API",
            inputSchema={
                "type": "object",
                "properties": {},
                "required": []
            }
        ),
        Tool(
            name="get_alpaca_account",
            description="Get Alpaca Paper Trading account information",
            inputSchema={
                "type": "object",
                "properties": {},
                "required": []
            }
        ),
        Tool(
            name="get_market_quote",
            description="Get latest market quote for a cryptocurrency symbol",
            inputSchema={
                "type": "object",
                "properties": {
                    "symbol": {
                        "type": "string",
                        "description": "Cryptocurrency symbol (e.g., BTC/USD)",
                        "default": "BTC/USD"
                    }
                },
                "required": []
            }
        ),
        Tool(
            name="run_grid_backtest",
            description="Run a grid trading strategy backtest",
            inputSchema={
                "type": "object",
                "properties": {
                    "symbol": {
                        "type": "string",
                        "description": "Trading symbol (e.g., BTC/USD)",
                        "default": "BTC/USD"
                    },
                    "initialCapital": {
                        "type": "number",
                        "description": "Initial capital in USD",
                        "default": 1000
                    },
                    "gridLevels": {
                        "type": "integer",
                        "description": "Number of grid levels",
                        "default": 10
                    },
                    "gridRange": {
                        "type": "number",
                        "description": "Grid range as percentage",
                        "default": 5
                    },
                    "orderSize": {
                        "type": "number",
                        "description": "Order size in USD per grid level",
                        "default": 100
                    },
                    "startDate": {
                        "type": "string",
                        "description": "Start date (ISO format: YYYY-MM-DD)",
                        "default": None
                    },
                    "endDate": {
                        "type": "string",
                        "description": "End date (ISO format: YYYY-MM-DD)",
                        "default": None
                    }
                },
                "required": []
            }
        ),
        Tool(
            name="run_scalping_backtest",
            description="Run a scalping strategy backtest",
            inputSchema={
                "type": "object",
                "properties": {
                    "symbol": {
                        "type": "string",
                        "description": "Trading symbol (e.g., BTC/USD)",
                        "default": "BTC/USD"
                    },
                    "initialCapital": {
                        "type": "number",
                        "description": "Initial capital in USD",
                        "default": 1000
                    },
                    "riskPerTrade": {
                        "type": "number",
                        "description": "Risk per trade as decimal (e.g., 0.02 for 2%)",
                        "default": 0.02
                    },
                    "takeProfitPips": {
                        "type": "number",
                        "description": "Take profit in pips",
                        "default": 8
                    },
                    "stopLossPips": {
                        "type": "number",
                        "description": "Stop loss in pips",
                        "default": 8
                    },
                    "startDate": {
                        "type": "string",
                        "description": "Start date (ISO format: YYYY-MM-DD)",
                        "default": None
                    },
                    "endDate": {
                        "type": "string",
                        "description": "End date (ISO format: YYYY-MM-DD)",
                        "default": None
                    }
                },
                "required": []
            }
        ),
        Tool(
            name="analyze_backtest_results",
            description="Analyze backtest results and provide insights",
            inputSchema={
                "type": "object",
                "properties": {
                    "backtestResult": {
                        "type": "object",
                        "description": "Backtest result object to analyze"
                    }
                },
                "required": ["backtestResult"]
            }
        )
    ]

async def make_api_request(method: str, endpoint: str, data: Optional[Dict] = None) -> Dict:
    """Make HTTP request to the API."""
    async with httpx.AsyncClient(timeout=60.0) as client:
        url = f"{API_BASE_URL}/{endpoint}"
        try:
            if method == "GET":
                response = await client.get(url)
            elif method == "POST":
                response = await client.post(url, json=data)
            else:
                raise ValueError(f"Unsupported method: {method}")
            
            response.raise_for_status()
            return response.json()
        except httpx.HTTPError as e:
            return {"error": str(e), "status_code": e.response.status_code if hasattr(e, 'response') else None}

@server.call_tool()
async def call_tool(name: str, arguments: Dict[str, Any]) -> List[TextContent]:
    """Handle tool calls."""
    
    if name == "test_alpaca_connection":
        result = await make_api_request("GET", "AlpacaTest/connection-test")
        analysis = ""
        if result.get("Success"):
            analysis = f"✅ Connection successful! Account: {result.get('AccountNumber')}, Equity: ${result.get('Equity', 0):,.2f}"
        else:
            analysis = f"❌ Connection failed: {result.get('Message', 'Unknown error')}"
        
        return [TextContent(type="text", text=json.dumps(result, indent=2) + "\n\n" + analysis)]
    
    elif name == "get_alpaca_account":
        result = await make_api_request("GET", "AlpacaTest/account")
        if "error" not in result:
            analysis = f"""
📊 Account Summary:
- Account Number: {result.get('AccountNumber')}
- Equity: ${result.get('Equity', 0):,.2f}
- Buying Power: ${result.get('BuyingPower', 0):,.2f}
- Cash: ${result.get('Cash', 0):,.2f}
- Portfolio Value: ${result.get('PortfolioValue', 0):,.2f}
- Pattern Day Trader: {'Yes' if result.get('PatternDayTrader') else 'No'}
- Trading Blocked: {'Yes' if result.get('TradingBlocked') else 'No'}
"""
        else:
            analysis = "❌ Error retrieving account information"
        
        return [TextContent(type="text", text=json.dumps(result, indent=2) + analysis)]
    
    elif name == "get_market_quote":
        symbol = arguments.get("symbol", "BTC/USD")
        result = await make_api_request("GET", f"AlpacaTest/quotes/{symbol}")
        if "error" not in result:
            bid_ask_spread = result.get('AskPrice', 0) - result.get('BidPrice', 0)
            spread_pct = (bid_ask_spread / result.get('BidPrice', 1)) * 100 if result.get('BidPrice') else 0
            analysis = f"""
📈 Market Quote for {symbol}:
- Bid: ${result.get('BidPrice', 0):,.2f} (Size: {result.get('BidSize', 0)})
- Ask: ${result.get('AskPrice', 0):,.2f} (Size: {result.get('AskSize', 0)})
- Spread: ${bid_ask_spread:,.2f} ({spread_pct:.4f}%)
- Timestamp: {result.get('Timestamp')}
"""
        else:
            analysis = f"❌ Error getting quote for {symbol}"
        
        return [TextContent(type="text", text=json.dumps(result, indent=2) + analysis)]
    
    elif name == "run_grid_backtest":
        from datetime import datetime, timedelta
        
        config = {
            "Configuration": {
                "Symbol": arguments.get("symbol", "BTC/USD"),
                "Timeframe": "1Min",
                "GridLevels": arguments.get("gridLevels", 10),
                "GridRange": arguments.get("gridRange", 5),
                "OrderSize": arguments.get("orderSize", 100),
                "StartDate": arguments.get("startDate") or (datetime.utcnow() - timedelta(days=7)).isoformat() + "Z",
                "EndDate": arguments.get("endDate") or datetime.utcnow().isoformat() + "Z"
            },
            "InitialCapital": arguments.get("initialCapital", 1000)
        }
        
        result = await make_api_request("POST", "GridBacktest/backtest", config)
        
        if "error" not in result:
            analysis = analyze_grid_backtest(result)
        else:
            analysis = "❌ Error running grid backtest"
        
        return [TextContent(type="text", text=json.dumps(result, indent=2) + "\n\n" + analysis)]
    
    elif name == "run_scalping_backtest":
        from datetime import datetime, timedelta
        
        config = {
            "Configuration": {
                "Symbol": arguments.get("symbol", "BTC/USD"),
                "Timeframe": "1Min",
                "RiskPerTrade": arguments.get("riskPerTrade", 0.02),
                "TakeProfitPips": arguments.get("takeProfitPips", 8),
                "StopLossPips": arguments.get("stopLossPips", 8),
                "UseHeikinAshi": True,
                "StartDate": arguments.get("startDate") or (datetime.utcnow() - timedelta(days=2)).isoformat() + "Z",
                "EndDate": arguments.get("endDate") or datetime.utcnow().isoformat() + "Z"
            },
            "InitialCapital": arguments.get("initialCapital", 1000)
        }
        
        result = await make_api_request("POST", "alpaca/scalping/backtest", config)
        
        if "error" not in result:
            analysis = analyze_scalping_backtest(result)
        else:
            analysis = "❌ Error running scalping backtest"
        
        return [TextContent(type="text", text=json.dumps(result, indent=2) + "\n\n" + analysis)]
    
    elif name == "analyze_backtest_results":
        backtest_result = arguments.get("backtestResult")
        if backtest_result:
            if "GridLevels" in backtest_result:
                analysis = analyze_grid_backtest(backtest_result)
            else:
                analysis = analyze_scalping_backtest(backtest_result)
        else:
            analysis = "❌ No backtest result provided"
        
        return [TextContent(type="text", analysis)]
    
    else:
        return [TextContent(type="text", text=f"Unknown tool: {name}")]

def analyze_grid_backtest(result: Dict) -> str:
    """Analyze grid backtest results."""
    total_profit = result.get("totalProfit", 0)
    total_profit_pct = result.get("totalProfitPercentage", 0)
    win_rate = result.get("winRate", 0)
    total_trades = result.get("totalTrades", 0)
    winning_trades = result.get("winningTrades", 0)
    losing_trades = result.get("losingTrades", 0)
    profit_factor = result.get("profitFactor", 0)
    initial_capital = result.get("initialCapital", 0)
    final_equity = result.get("finalEquity", 0)
    
    analysis = f"""
📊 Grid Trading Backtest Analysis:

💰 Performance:
- Initial Capital: ${initial_capital:,.2f}
- Final Equity: ${final_equity:,.2f}
- Total Profit: ${total_profit:,.2f} ({total_profit_pct:.2f}%)

📈 Trade Statistics:
- Total Trades: {total_trades}
- Winning Trades: {winning_trades}
- Losing Trades: {losing_trades}
- Win Rate: {win_rate:.2f}%

📊 Metrics:
- Profit Factor: {profit_factor:.2f}
- Average Win: ${result.get('averageWin', 0):,.2f}
- Average Loss: ${result.get('averageLoss', 0):,.2f}

"""
    
    # Add recommendations
    if win_rate > 80:
        analysis += "✅ Excellent win rate! Strategy performs well in ranging markets.\n"
    elif win_rate < 50:
        analysis += "⚠️ Low win rate. Consider adjusting grid parameters or date range.\n"
    
    if profit_factor > 2:
        analysis += "✅ Strong profit factor. Strategy has good risk/reward ratio.\n"
    elif profit_factor < 1:
        analysis += "⚠️ Profit factor below 1. Strategy may not be profitable long-term.\n"
    
    if total_profit_pct > 10:
        analysis += "✅ Strong returns! Consider paper trading to validate.\n"
    elif total_profit_pct < 0:
        analysis += "❌ Negative returns. Review strategy parameters.\n"
    
    return analysis

def analyze_scalping_backtest(result: Dict) -> str:
    """Analyze scalping backtest results."""
    total_profit = result.get("totalProfit", 0)
    total_profit_pct = result.get("totalProfitPercentage", 0)
    win_rate = result.get("winRate", 0)
    total_trades = result.get("totalTrades", 0)
    winning_trades = result.get("winningTrades", 0)
    losing_trades = result.get("losingTrades", 0)
    profit_factor = result.get("profitFactor", 0)
    initial_capital = result.get("initialCapital", 0)
    final_equity = result.get("finalEquity", 0)
    
    analysis = f"""
📊 Scalping Strategy Backtest Analysis:

💰 Performance:
- Initial Capital: ${initial_capital:,.2f}
- Final Equity: ${final_equity:,.2f}
- Total Profit: ${total_profit:,.2f} ({total_profit_pct:.2f}%)

📈 Trade Statistics:
- Total Trades: {total_trades}
- Winning Trades: {winning_trades}
- Losing Trades: {losing_trades}
- Win Rate: {win_rate:.2f}%

📊 Metrics:
- Profit Factor: {profit_factor:.2f}
- Average Win: ${result.get('averageWin', 0):,.2f}
- Average Loss: ${result.get('averageLoss', 0):,.2f}

"""
    
    # Add recommendations
    if win_rate > 60:
        analysis += "✅ Good win rate for scalping strategy.\n"
    elif win_rate < 40:
        analysis += "⚠️ Low win rate. Review entry/exit signals and indicators.\n"
    
    if profit_factor > 1.5:
        analysis += "✅ Positive profit factor. Strategy shows promise.\n"
    elif profit_factor < 1:
        analysis += "❌ Profit factor below 1. Adjust risk parameters.\n"
    
    trades_per_day = total_trades / max(1, (result.get('endDate') - result.get('startDate')).days) if result.get('endDate') and result.get('startDate') else 0
    if trades_per_day > 20:
        analysis += "⚠️ High trade frequency. Consider transaction costs.\n"
    
    return analysis

async def main():
    """Run the MCP server."""
    from mcp.server.stdio import stdio_server
    
    async with stdio_server() as (read_stream, write_stream):
        await server.run(read_stream, write_stream, server.create_initialization_options())

if __name__ == "__main__":
    asyncio.run(main())

