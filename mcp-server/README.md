# Alpaca CoinAPI MCP Server

MCP (Model Context Protocol) server for analyzing Alpaca Paper Trading backtests and market data.

## Setup

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Ensure the CoinAPI is running:
```bash
cd CoinAPI
dotnet run
```

3. Configure MCP in your editor (VS Code, Cursor, etc.)

## Configuration

Add to your MCP settings (`.cursor/mcp.json` or similar):

```json
{
  "mcpServers": {
    "alpaca-coinapi": {
      "command": "python",
      "args": ["path/to/mcp-server/alpaca_mcp_server.py"],
      "env": {}
    }
  }
}
```

## Available Tools

- `test_alpaca_connection` - Test Alpaca Paper Trading connection
- `get_alpaca_account` - Get account information
- `get_market_quote` - Get latest market quote
- `run_grid_backtest` - Run grid trading backtest
- `run_scalping_backtest` - Run scalping strategy backtest
- `analyze_backtest_results` - Analyze backtest results





