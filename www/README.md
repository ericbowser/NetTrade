# CryptoTrading Dashboard

A full-stack cryptocurrency trading dashboard that connects to various exchanges and provides visualization tools for backtesting trading strategies.

## Project Overview

This project consists of:

1. **Backend**: .NET Core API for connecting to cryptocurrency exchanges and running trading strategies
2. **Frontend**: React dashboard with Tailwind CSS for visualization and user interaction

## Features

- Connect to various cryptocurrency exchanges (Coinbase, Alpaca, Binance)
- Backtest trading strategies against historical data
- Visualize backtest results with interactive charts
- View detailed trade history and performance metrics
- Support for multiple trading strategies:
   - One-Minute Scalping
   - Grid Trading

## Tech Stack

### Backend
- .NET 9.0
- NLog for logging
- Alpaca.Markets SDK
- Coinbase.AdvancedTrade SDK
- Binance.Client

### Frontend
- React 18
- Tailwind CSS for styling
- Recharts for data visualization
- Axios for API communication

## Setup Instructions

### Prerequisites

- Node.js 18+ and npm
- .NET 9.0 SDK
- API keys for the exchanges you plan to use

### Backend Setup

1. Clone the repository
2. Open the solution file `CoinAPI.sln` in Visual Studio
3. Update the `appsettings.json` file with your API keys:

```json
{
  "Alpaca": {
    "ApiKey": "YOUR_ALPACA_API_KEY",
    "Secret": "YOUR_ALPACA_SECRET_KEY"
  },
  "Coinbase": {
    "ApiKey": "YOUR_COINBASE_API_KEY",
    "ApiSecret": "YOUR_COINBASE_API_SECRET"
  },
  "Binance": {
    "ApiKey": "YOUR_BINANCE_API_KEY",
    "ApiSecret": "YOUR_BINANCE_API_SECRET"
  }
}
```

4. Restore NuGet packages
5. Build and run the project

### Frontend Setup

1. Navigate to the `www` directory
2. Install dependencies:

```bash
npm install
```

3. Start the development server:

```bash
npm start
```

4. Open [http://localhost:3000](http://localhost:3000) in your browser

## Usage

1. Configure your backtest parameters in the sidebar
2. Run the backtest and wait for results
3. View the performance chart showing equity curve and trade entries/exits
4. Switch to the Trade History tab to see detailed information about individual trades
5. Analyze performance metrics like total profit, win rate, and more

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Commit your changes: `git commit -m 'Add some feature'`
4. Push to the branch: `git push origin feature/your-feature-name`
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Alpaca Markets](https://alpaca.markets/) for their trading API
- [Coinbase](https://www.coinbase.com/) for their exchange API
- [Binance](https://www.binance.com/) for their exchange API
- [Recharts](https://recharts.org/) for the charting library
- [Tailwind CSS](https://tailwindcss.com/) for the styling framework