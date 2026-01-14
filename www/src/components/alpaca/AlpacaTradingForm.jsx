import React, { useState } from "react";
import { BACKEND_BASE_URL } from "../../../env.json";
import axios from "axios";

const AlpacaTradingForm = ({ onStart, onStop, isRunning, botId }) => {
  const [formData, setFormData] = useState({
    symbol: "BTC/USD",
    gridLevels: 6,
    gridRange: 7,
    orderSize: 250.0,
    timeframe: "1Min",
    initialCapital: 5000,
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
        gridLevels: formData.gridLevels,
        gridRange: formData.gridRange,
        orderSize: formData.orderSize,
        timeframe: formData.timeframe,
      };

      const request = {
        gridTradingConfiguration: config,
        initialCapital: formData.initialCapital,
      };

      const response = await axios.post(
        `${BACKEND_BASE_URL}/api/AlpacaPaperGridTrading/start`,
        request
      );

      if (onStart) {
        onStart(response.data);
      }
    } catch (error) {
      console.error("Error starting Alpaca grid bot:", error);
      alert(
        "Failed to start trading bot: " +
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
        `${BACKEND_BASE_URL}/api/AlpacaPaperGridTrading/${botId}/stop`
      );
      if (onStop) {
        onStop();
      }
    } catch (error) {
      console.error("Error stopping bot:", error);
      alert(
        "Failed to stop trading bot: " +
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
              Alpaca Grid Trading Active
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-300">
              Symbol: <span className="font-medium">{formData.symbol}</span> |
              Grid Levels:{" "}
              <span className="font-medium">{formData.gridLevels}</span> |
              Range: <span className="font-medium">{formData.gridRange}%</span>
            </p>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
              Bot ID: {botId}
            </p>
          </div>
          <button
            onClick={handleStop}
            disabled={loading}
            className="px-6 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? "Stopping..." : "Stop Trading"}
          </button>
        </div>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="bg-slate-50 dark:bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-200 dark:border-slate-700"
    >
      <h3 className="text-lg font-medium text-slate-800 dark:text-slate-100 mb-4">
        Start Alpaca Grid Trading Bot
      </h3>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {/* Symbol */}
        <div>
          <label
            htmlFor="symbol"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Symbol
          </label>
          <input
            type="text"
            id="symbol"
            name="symbol"
            value={formData.symbol}
            onChange={handleChange}
            required
            placeholder="BTC/USD"
            className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Grid Levels */}
        <div>
          <label
            htmlFor="gridLevels"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Grid Levels
          </label>
          <input
            type="number"
            id="gridLevels"
            name="gridLevels"
            value={formData.gridLevels}
            onChange={handleChange}
            step={1}
            min={2}
            max={20}
            required
            className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Grid Range */}
        <div>
          <label
            htmlFor="gridRange"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Grid Range (%)
          </label>
          <input
            type="number"
            id="gridRange"
            name="gridRange"
            value={formData.gridRange}
            onChange={handleChange}
            step={0.1}
            min={1}
            max={20}
            required
            className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Order Size */}
        <div>
          <label
            htmlFor="orderSize"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Order Size ($)
          </label>
          <input
            type="number"
            id="orderSize"
            name="orderSize"
            value={formData.orderSize}
            onChange={handleChange}
            step={0.01}
            min={1}
            required
            className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Timeframe */}
        <div>
          <label
            htmlFor="timeframe"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Timeframe
          </label>
          <select
            id="timeframe"
            name="timeframe"
            value={formData.timeframe}
            onChange={handleChange}
            className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="1Min">1 Minute</option>
            <option value="5Min">5 Minutes</option>
            <option value="15Min">15 Minutes</option>
            <option value="1Hour">1 Hour</option>
          </select>
        </div>

        {/* Initial Capital */}
        <div>
          <label
            htmlFor="initialCapital"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Initial Capital ($)
          </label>
          <input
            type="number"
            id="initialCapital"
            name="initialCapital"
            value={formData.initialCapital}
            onChange={handleChange}
            step={0.01}
            min={1}
            required
            className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>
      </div>

      {/* Submit Button */}
      <div className="mt-6">
        <button
          type="submit"
          disabled={loading}
          className={`w-full py-2 px-6 rounded-md text-white font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 ${
            loading
              ? "bg-slate-400 cursor-not-allowed"
              : "bg-green-600 hover:bg-green-700"
          }`}
        >
          {loading ? "Starting..." : "Start Grid Trading Bot"}
        </button>
      </div>
    </form>
  );
};

export default AlpacaTradingForm;
