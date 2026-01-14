# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CoinApi is a cryptocurrency trading RESTful API that integrates with multiple trading platforms (Alpaca Paper Trading, Coinbase, Binance) for backtesting and executing trading strategies. The system includes:

- **Backend**: ASP.NET Core 9.0 Web API (C#)
- **Frontend**: React application with Webpack, TailwindCSS
- **Testing**: xUnit test framework with Moq

## Architecture

### Backend Structure (CoinAPI/)

The backend follows a layered service-oriented architecture:

1. **Controllers** - API endpoints for different trading platforms and strategies
   - `AlpacaPaperBacktestController` - Backtesting with Alpaca Paper Trading
   - `GridBacktestController` - Grid trading strategy backtests
   - `CoinbaseController` - Coinbase integration endpoints
   - `TradingController` - General trading operations

2. **Services** - Business logic and external API integration
   - `AlpacaCryptoDataService` - Fetches historical crypto data from Alpaca
   - `ScalpingStrategyService` - Implements scalping strategy with SMA/MACD indicators
   - `GridStrategyService` - Grid trading strategy logic
   - `AlpacaGridTradingService` - Live grid trading bot (IHostedService)
   - `CoinbaseTradingService` - Coinbase trading operations

3. **Clients** - External API wrappers
   - `AlpacaPaperClient` - Alpaca Paper Trading API client
   - `CoinbaseClient` - Coinbase API client
   - `BinanceApiClient` - Binance API client

4. **Models** - Data structures
   - `ScalpingStrategyConfiguration` - Configuration for scalping strategies
   - `GridTradingConfiguration` - Configuration for grid trading
   - `TradingBot` - Trading bot base model
   - `BacktestResponse` - Backtest result data

5. **BackTest** - Background services for automated trading
   - `ScalpingBot` - One-minute scalping bot (BackgroundService)

### Key Design Patterns

- **Dependency Injection**: All services registered in `Program.cs` using ASP.NET Core DI
- **Strategy Pattern**: Multiple trading strategies (scalping, grid) implement similar backtesting interfaces
- **Background Services**: `IHostedService` implementations for automated trading bots
- **Repository Pattern**: Clients abstract external API calls

### Trading Strategy Implementation

#### Scalping Strategy
- Uses SMA (Simple Moving Average) and MACD (Moving Average Convergence Divergence) indicators
- Supports Heikin-Ashi candlestick transformation
- Generates buy/sell signals based on trend and MACD histogram crossovers
- Date range chunking for large historical data requests (7-day chunks)
- Implements take profit/stop loss exit conditions

#### Grid Trading Strategy
- Creates buy/sell orders at predefined price levels
- Automatically places counter-orders when levels are filled
- Tracks portfolio value and P&L in real-time
- Uses limit orders with GTC (Good Till Cancelled) time in force

### Frontend Structure (www/)

React SPA with the following components:
- `Dashboard` - Main application view
- `BacktestForm` - Input form for backtest parameters
- `BacktestChart` - Recharts visualization of backtest results
- `TradeList` - Display of individual trades
- `Header` - Navigation/header component

Configuration is managed through `env.js` with backend URL and API endpoints.

## Development Commands

### Backend (.NET)

```bash
# Build the solution
dotnet build CoinAPI.sln

# Run the API (from CoinAPI/ directory)
dotnet run --project CoinAPI/ProviderApi.csproj

# Run tests
dotnet test CoinApi-Tests/CoinApi-Tests.csproj

# Run a specific test
dotnet test CoinApi-Tests/CoinApi-Tests.csproj --filter "FullyQualifiedName~AlpacaCoryptoDataServiceTests"

# Restore packages
dotnet restore
```

The API runs on HTTPS at `https://localhost:44394` (configured in launch settings). Swagger UI is available at the root path when running in development mode.

### Frontend (React)

```bash
# Install dependencies (from www/ directory)
cd www
npm install

# Run development server (HTTPS on port 3002)
npm run dev

# Build for production
npm run build

# Build Tailwind CSS
npm run tail

# Watch Tailwind CSS changes
npm run tail:watch

# Clean node_modules
npm run clean
```

The dev server runs on `https://localhost:3002` with HTTPS enabled.

## Configuration

### Backend Configuration (appsettings.json)

The backend uses `appsettings.json` for configuration with sections for:
- Alpaca Paper Trading API credentials and endpoints
- Coinbase API credentials
- Binance API credentials
- Database connection strings (PostgreSQL, SQL Server)

**IMPORTANT**: API keys and secrets are currently in appsettings.json. Be careful not to commit sensitive credentials. Consider using User Secrets for local development.

### Frontend Configuration (www/env.js)

Frontend configuration exported as ES6 module:
- `BACKEND_BASE_URL` - Backend API base URL
- Strategy-specific API endpoints (Grid, Scalping, OneMinute)
- Port and host configuration

## Testing

Tests are located in `CoinApi-Tests/` using xUnit and Moq for mocking:
- `AlpacaCoryptoDataServiceTests.cs` - Tests for Alpaca data service

When writing new tests:
- Use xUnit `[Fact]` and `[Theory]` attributes
- Use Moq for mocking dependencies
- Test project targets .NET 9.0

## Important Notes

### Symbol Formatting
- Alpaca uses format: "BTC/USD" (slash separator)
- Extension method `FormatSymbolForAlpaca()` handles conversion

### Date Handling
- Historical data requests are chunked into 7-day segments to avoid API limits
- Pagination is supported with a max of 10 pages per chunk
- All timestamps are in UTC

### Logging
- Backend uses NLog for structured logging
- Log files configuration should be in NLog.config (schema available in NLog.xsd)

### CORS
- Backend allows origins: `https://localhost:8004`, `https://localhost:3002`, and `https://*`
- Configured in `Program.cs` with "AllowLocalhost" policy

### Dependency Versions
- .NET 9.0 with C# 10 (backend), C# 13 (tests)
- React 18.2.0
- Alpaca.Markets 7.2.0
- ImplicitUsings disabled in main project but enabled in tests
