import React, { useState, useEffect } from "react";
import AlpacaTradingForm from "../components/alpaca/AlpacaTradingForm";
import RSITradingForm from "../components/alpaca/RSITradingForm";
import MovingAverageCrossoverTradingForm from "../components/alpaca/MovingAverageCrossoverTradingForm";
import BollingerBandsTradingForm from "../components/alpaca/BollingerBandsTradingForm";
import AlpacaAccountInfo from "../components/alpaca/AlpacaAccountInfo";
import AlpacaPositions from "../components/alpaca/AlpacaPositions";
import AlpacaOrders from "../components/alpaca/AlpacaOrders";
import RealTimeTrades from "../components/alpaca/RealTimeTrades";
import { BACKEND_BASE_URL } from "../../env.json";
import axios from "axios";
import { StopIcon } from "@heroicons/react/24/outline";

const AlpacaTrading = () => {
  const [isRunning, setIsRunning] = useState(false);
  const [botId, setBotId] = useState(null);
  const [runningBots, setRunningBots] = useState([]);
  const [strategy, setStrategy] = useState("Grid"); // "Grid", "RSI", "MA", or "BB"

  useEffect(() => {
    loadRunningBots();
    const interval = setInterval(loadRunningBots, 10000); // Check every 10 seconds
    return () => clearInterval(interval);
  }, []);

  const loadRunningBots = async () => {
    try {
      // Load Grid, RSI, MA, and BB bots
      const [gridResponse, rsiResponse, maResponse, bbResponse] = await Promise.all([
        axios
          .get(`${BACKEND_BASE_URL}/api/AlpacaPaperGridTrading`)
          .catch(() => ({ data: [] })),
        axios
          .get(`${BACKEND_BASE_URL}/api/AlpacaPaperRSITrading`)
          .catch(() => ({ data: [] })),
        axios
          .get(
            `${BACKEND_BASE_URL}/api/AlpacaPaperMovingAverageCrossoverTrading`
          )
          .catch(() => ({ data: [] })),
        axios
          .get(`${BACKEND_BASE_URL}/api/AlpacaPaperBollingerBandsTrading`)
          .catch(() => ({ data: [] })),
      ]);

      const gridBots = (gridResponse.data || []).map((bot) => ({
        ...bot,
        type: "Grid",
      }));
      const rsiBots = (rsiResponse.data || []).map((bot) => ({
        ...bot,
        type: "RSI",
      }));
      const maBots = (maResponse.data || []).map((bot) => ({
        ...bot,
        type: "MA",
      }));
      const bbBots = (bbResponse.data || []).map((bot) => ({
        ...bot,
        type: "BB",
      }));
      const allBots = [...gridBots, ...rsiBots, ...maBots, ...bbBots];

      setRunningBots(allBots);

      // If we have a bot ID but it's not in the list, it was stopped
      if (botId && !allBots.find((b) => b.botId === botId)) {
        setIsRunning(false);
        setBotId(null);
      } else if (allBots.length > 0 && !botId) {
        // If we have bots but no botId, set the first one
        const firstBot = allBots[0];
        setBotId(firstBot.botId);
        setIsRunning(true);
      }
    } catch (error) {
      console.error("Error loading running bots:", error);
    }
  };

  const handleStart = (data) => {
    setBotId(data.botId);
    setIsRunning(true);
    loadRunningBots();
  };

  const handleStop = async (bot) => {
    if (
      !confirm(`Are you sure you want to stop ${bot.type} Bot #${bot.botId}?`)
    ) {
      return;
    }

    try {
      const apiPath =
        bot.type === "Grid"
          ? "AlpacaPaperGridTrading"
          : bot.type === "RSI"
          ? "AlpacaPaperRSITrading"
          : bot.type === "MA"
          ? "AlpacaPaperMovingAverageCrossoverTrading"
          : "AlpacaPaperBollingerBandsTrading";
      await axios.post(`${BACKEND_BASE_URL}/api/${apiPath}/${bot.botId}/stop`);

      // If the stopped bot was the current one, clear it
      if (bot.botId === botId) {
        setIsRunning(false);
        setBotId(null);
      }

      loadRunningBots();
    } catch (error) {
      console.error("Error stopping bot:", error);
      alert(
        "Failed to stop bot: " + (error.response?.data?.error || error.message)
      );
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
          Alpaca Paper Trading
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Trade with fake money on Alpaca Paper Trading. Monitor your positions
          and orders in real-time.
        </p>
      </div>

      {/* Account Info */}
      <AlpacaAccountInfo isBotRunning={isRunning} />

      {/* Strategy Selector */}
      <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100 mb-4">
          Select Trading Strategy
        </h3>
        <div className="flex gap-4">
          <label className="flex items-center cursor-pointer">
            <input
              type="radio"
              name="strategy"
              value="Grid"
              checked={strategy === "Grid"}
              onChange={(e) => setStrategy(e.target.value)}
              className="h-4 w-4 text-blue-600 border-slate-300 dark:border-slate-600 focus:ring-blue-500"
            />
            <span className="ml-2 text-sm text-slate-700 dark:text-slate-300">
              Grid Trading
            </span>
          </label>
          <label className="flex items-center cursor-pointer">
            <input
              type="radio"
              name="strategy"
              value="RSI"
              checked={strategy === "RSI"}
              onChange={(e) => setStrategy(e.target.value)}
              className="h-4 w-4 text-blue-600 border-slate-300 dark:border-slate-600 focus:ring-blue-500"
            />
            <span className="ml-2 text-sm text-slate-700 dark:text-slate-300">
              RSI Mean Reversion
            </span>
          </label>
          <label className="flex items-center cursor-pointer">
            <input
              type="radio"
              name="strategy"
              value="MA"
              checked={strategy === "MA"}
              onChange={(e) => setStrategy(e.target.value)}
              className="h-4 w-4 text-blue-600 border-slate-300 dark:border-slate-600 focus:ring-blue-500"
            />
            <span className="ml-2 text-sm text-slate-700 dark:text-slate-300">
              Moving Average Crossover
            </span>
          </label>
          <label className="flex items-center cursor-pointer">
            <input
              type="radio"
              name="strategy"
              value="BB"
              checked={strategy === "BB"}
              onChange={(e) => setStrategy(e.target.value)}
              className="h-4 w-4 text-blue-600 border-slate-300 dark:border-slate-600 focus:ring-blue-500"
            />
            <span className="ml-2 text-sm text-slate-700 dark:text-slate-300">
              Bollinger Bands
            </span>
          </label>
        </div>
      </div>

      {/* Trading Form */}
      {strategy === "Grid" ? (
        <AlpacaTradingForm
          onStart={handleStart}
          onStop={handleStop}
          isRunning={
            isRunning &&
            runningBots.find((b) => b.botId === botId && b.type === "Grid")
          }
          botId={botId}
        />
      ) : strategy === "RSI" ? (
        <RSITradingForm
          onStart={handleStart}
          onStop={handleStop}
          isRunning={
            isRunning &&
            runningBots.find((b) => b.botId === botId && b.type === "RSI")
          }
          botId={botId}
        />
      ) : strategy === "MA" ? (
        <MovingAverageCrossoverTradingForm
          onStart={handleStart}
          onStop={handleStop}
          isRunning={
            isRunning &&
            runningBots.find((b) => b.botId === botId && b.type === "MA")
          }
          botId={botId}
        />
      ) : (
        <BollingerBandsTradingForm
          onStart={handleStart}
          onStop={handleStop}
          isRunning={
            isRunning &&
            runningBots.find((b) => b.botId === botId && b.type === "BB")
          }
          botId={botId}
        />
      )}

      {/* Running Bots Management */}
      {runningBots.length > 0 && (
        <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg border border-slate-200 dark:border-slate-700">
          <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between">
            <div>
              <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
                Running Trading Bots
              </h3>
              <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
                Manage all active trading bots from one place
              </p>
            </div>
            <button
              onClick={loadRunningBots}
              className="text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
            >
              Refresh
            </button>
          </div>
          <div className="divide-y divide-slate-200 dark:divide-slate-700">
            {runningBots.map((bot) => (
              <div
                key={`${bot.type}-${bot.botId}`}
                className="px-6 py-4 hover:bg-white dark:hover:bg-slate-700 transition-colors"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4">
                    <div className="flex-shrink-0">
                      <div className="h-10 w-10 rounded-full bg-blue-100 dark:bg-blue-900 flex items-center justify-center">
                        <span className="text-blue-600 dark:text-blue-300 font-semibold text-sm">
                          {bot.type === "Grid"
                            ? "G"
                            : bot.type === "RSI"
                            ? "R"
                            : bot.type === "MA"
                            ? "M"
                            : "B"}
                        </span>
                      </div>
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                          {bot.type} Trading Bot #{bot.botId}
                        </p>
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200">
                          {bot.status || "Running"}
                        </span>
                      </div>
                      <div className="mt-1 flex items-center gap-4 text-xs text-slate-500 dark:text-slate-400">
                        {bot.symbol && <span>Symbol: {bot.symbol}</span>}
                        {bot.startTime && (
                          <span>
                            Started: {new Date(bot.startTime).toLocaleString()}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => handleStop(bot)}
                      className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                    >
                      <StopIcon className="h-4 w-4 mr-1" />
                      Stop Bot
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Real-time Trades */}
      <RealTimeTrades isBotRunning={isRunning} />

      {/* Positions and Orders Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <AlpacaPositions isBotRunning={isRunning} />
        <AlpacaOrders isBotRunning={isRunning} />
      </div>
    </div>
  );
};

export default AlpacaTrading;
