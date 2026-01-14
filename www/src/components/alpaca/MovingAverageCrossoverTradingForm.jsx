import React, { useState } from "react";
import { BACKEND_BASE_URL } from "../../../env.json";
import axios from "axios";

const MovingAverageCrossoverTradingForm = ({
  onStart,
  onStop,
  isRunning,
  botId,
}) => {
  const [formData, setFormData] = useState({
    symbol: "BTC/USD",
    fastPeriod: 50,
    slowPeriod: 200,
    riskPerTrade: 2.0,
    takeProfitPercent: 3.0,
    stopLossPercent: 2.0,
    timeframe: "1Hour",
    initialCapital: 1000,
    checkIntervalSeconds: 300,
  });

  const [loading, setLoading] = useState(false);

  const handleChange = (e) => {
    const { name, value, type } = e.target;
    setFormData({
      ...formData,
      [name]: type === "number" ? parseFloat(value) || 0 : value,
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      const config = {
        symbol: formData.symbol,
        fastPeriod: formData.fastPeriod,
        slowPeriod: formData.slowPeriod,
        riskPerTrade: formData.riskPerTrade / 100, // Convert percentage to decimal
        takeProfitPercent: formData.takeProfitPercent,
        stopLossPercent: formData.stopLossPercent,
        timeframe: formData.timeframe,
      };

      const request = {
        movingAverageCrossoverStrategyConfiguration: config,
        initialCapital: formData.initialCapital,
        checkIntervalSeconds: formData.checkIntervalSeconds,
      };

      const response = await axios.post(
        `${BACKEND_BASE_URL}/api/AlpacaPaperMovingAverageCrossoverTrading/start`,
        request
      );

      if (onStart) {
        onStart(response.data);
      }
    } catch (error) {
      console.error(
        "Error starting Alpaca Moving Average Crossover bot:",
        error
      );
      alert(
        "Failed to start Moving Average Crossover trading bot: " +
          (error.response?.data?.error || error.message)
      );
    } finally {
      setLoading(false);
    }
  };

  const handleStop = async () => {
    if (!botId) return;

    setLoading(true);
    try {
      await axios.post(
        `${BACKEND_BASE_URL}/api/AlpacaPaperMovingAverageCrossoverTrading/${botId}/stop`
      );
      if (onStop) {
        onStop();
      }
    } catch (error) {
      console.error("Error stopping Moving Average Crossover bot:", error);
      alert(
        "Failed to stop Moving Average Crossover trading bot: " +
          (error.response?.data?.error || error.message)
      );
    } finally {
      setLoading(false);
    }
  };

  if (isRunning) {
    return (
      <div className="bg-slate-50 dark:bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-200 dark:border-slate-700">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-medium text-slate-800 dark:text-slate-100 mb-2">
              Moving Average Crossover Trading Bot Active
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-300">
              Symbol: <span className="font-medium">{formData.symbol}</span> |
              Fast MA:{" "}
              <span className="font-medium">{formData.fastPeriod}</span> | Slow
              MA: <span className="font-medium">{formData.slowPeriod}</span>
            </p>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
              Bot ID: {botId}
            </p>
          </div>
          <button
            onClick={handleStop}
            disabled={loading}
            className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:bg-slate-400 disabled:cursor-not-allowed"
          >
            {loading ? "Stopping..." : "Stop Bot"}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-slate-50 dark:bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-200 dark:border-slate-700">
      <h3 className="text-lg font-medium text-slate-800 dark:text-slate-100 mb-4">
        Moving Average Crossover Trading Bot Configuration
      </h3>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {/* Symbol */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Symbol
            </label>
            <input
              type="text"
              name="symbol"
              value={formData.symbol}
              onChange={handleChange}
              required
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Fast Period */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Fast MA Period
            </label>
            <input
              type="number"
              name="fastPeriod"
              value={formData.fastPeriod}
              onChange={handleChange}
              required
              min={5}
              max={200}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Slow Period */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Slow MA Period
            </label>
            <input
              type="number"
              name="slowPeriod"
              value={formData.slowPeriod}
              onChange={handleChange}
              required
              min={10}
              max={500}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Risk Per Trade */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Risk Per Trade (%)
            </label>
            <input
              type="number"
              name="riskPerTrade"
              value={formData.riskPerTrade}
              onChange={handleChange}
              required
              min={0.1}
              max={10}
              step={0.1}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Take Profit */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Take Profit (%)
            </label>
            <input
              type="number"
              name="takeProfitPercent"
              value={formData.takeProfitPercent}
              onChange={handleChange}
              required
              min={0.1}
              max={20}
              step={0.1}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Stop Loss */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Stop Loss (%)
            </label>
            <input
              type="number"
              name="stopLossPercent"
              value={formData.stopLossPercent}
              onChange={handleChange}
              required
              min={0.1}
              max={20}
              step={0.1}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Timeframe */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Timeframe
            </label>
            <select
              name="timeframe"
              value={formData.timeframe}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="1Min">1 Minute</option>
              <option value="5Min">5 Minutes</option>
              <option value="15Min">15 Minutes</option>
              <option value="1Hour">1 Hour</option>
              <option value="1Day">1 Day</option>
            </select>
          </div>

          {/* Initial Capital */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Initial Capital ($)
            </label>
            <input
              type="number"
              name="initialCapital"
              value={formData.initialCapital}
              onChange={handleChange}
              required
              min={100}
              step={100}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Check Interval */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Check Interval (seconds)
            </label>
            <input
              type="number"
              name="checkIntervalSeconds"
              value={formData.checkIntervalSeconds}
              onChange={handleChange}
              required
              min={60}
              max={3600}
              step={60}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={loading}
            className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:bg-slate-400 disabled:cursor-not-allowed"
          >
            {loading ? "Starting..." : "Start MA Crossover Bot"}
          </button>
        </div>
      </form>
    </div>
  );
};

export default MovingAverageCrossoverTradingForm;
