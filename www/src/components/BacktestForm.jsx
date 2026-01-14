// src/components/BacktestForm.js
import React, { useState, useEffect } from "react";
import Strategy from "../../util/Strategy";
import {
  BACKEND_BASE_URL,
  GRID_STRATEGY_REL,
  SCALP_STRATEGY_REL,
  RSI_STRATEGY_REL,
  BOLLINGER_BANDS_STRATEGY_REL,
  MOVING_AVERAGE_CROSSOVER_STRATEGY_REL,
} from "../../env.json";

const BacktestForm = ({ onSubmit, isLoading }) => {
  const [formData, setFormData] = useState({
    symbol: "BTC/USD",
    startDate: new Date(new Date().setDate(new Date().getDate() - 30))
      .toISOString()
      .split("T")[0], // Default to 30 days ago
    endDate: new Date().toISOString().split("T")[0], // Default to today
    initialCapital: 5000,
    strategy: Strategy[0],
    useHeikinAshi: true,
    timeframe: "15Min", // Default to 15Min for RSI, will be overridden per strategy
    gridLevels: 10,
    gridRange: 7,
    orderSize: 250.0,
    // RSI-specific fields
    rsiPeriod: 14,
    oversoldLevel: 30,
    overboughtLevel: 70,
    riskPerTrade: 2.0,
    takeProfitPercent: 2.0,
    stopLossPercent: 1.5,
    // Bollinger Bands-specific fields
    bbPeriod: 20,
    standardDeviations: 2.0,
    // Moving Average Crossover-specific fields
    fastPeriod: 50,
    slowPeriod: 200,
  });

  const [strategyUrl, setStrategyUrl] = useState(null);
  const [strategy, setStrategy] = useState(Strategy[0]);

  useEffect(() => {}, [strategyUrl, strategy]);

  const handleChange = async (e) => {
    const { name, value, type, checked } = e.target;
    if (name === "strategy") {
      console.log("setting strategy to ", value);
      setStrategy(value);
    }
    console.log(e.target.value);
    setFormData({
      ...formData,
      [name]: type === "checkbox" ? checked : value,
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const url = decideStrategyUrl();
    if (!url) {
      console.error("No strategy URL determined");
      return;
    }

    // Convert form data to the format expected by the API
    const config = {
      symbol: formData.symbol,
      startDate: new Date(formData.startDate),
      endDate: new Date(formData.endDate),
      timeframe: formData.timeframe,
    };

    // Add strategy-specific fields
    if (strategy === "Grid") {
      config.gridLevels = parseInt(formData.gridLevels);
      config.gridRange = parseFloat(formData.gridRange);
      config.orderSize = parseFloat(formData.orderSize);
    } else if (strategy === "OneMinuteScalp") {
      config.useHeikinAshi = formData.useHeikinAshi;
    } else if (strategy === "RSI") {
      config.rsiPeriod = parseInt(formData.rsiPeriod);
      config.oversoldLevel = parseFloat(formData.oversoldLevel);
      config.overboughtLevel = parseFloat(formData.overboughtLevel);
      config.riskPerTrade = parseFloat(formData.riskPerTrade) / 100; // Convert percentage to decimal
      config.takeProfitPercent = parseFloat(formData.takeProfitPercent);
      config.stopLossPercent = parseFloat(formData.stopLossPercent);
    } else if (strategy === "BollingerBands") {
      config.period = parseInt(formData.bbPeriod);
      config.standardDeviations = parseFloat(formData.standardDeviations);
      config.riskPerTrade = parseFloat(formData.riskPerTrade) / 100; // Convert percentage to decimal
      config.takeProfitPercent = parseFloat(formData.takeProfitPercent);
      config.stopLossPercent = parseFloat(formData.stopLossPercent);
    } else if (strategy === "MovingAverageCrossover") {
      config.fastPeriod = parseInt(formData.fastPeriod);
      config.slowPeriod = parseInt(formData.slowPeriod);
      config.riskPerTrade = parseFloat(formData.riskPerTrade) / 100; // Convert percentage to decimal
      config.takeProfitPercent = parseFloat(formData.takeProfitPercent);
      config.stopLossPercent = parseFloat(formData.stopLossPercent);
    }

    const backtestRequest = {
      configuration: config,
      initialCapital: parseFloat(formData.initialCapital),
    };

    console.log("Submitting backtest request:", backtestRequest);
    onSubmit(backtestRequest, url);
  };

  function decideStrategyUrl() {
    switch (strategy) {
      case "Grid":
        const gridStratUrl = `${BACKEND_BASE_URL}${GRID_STRATEGY_REL}`;
        console.log("returning ", gridStratUrl);
        setStrategyUrl(gridStratUrl);
        return gridStratUrl;
      case "OneMinuteScalp":
        const oneMinStratUrl = `${BACKEND_BASE_URL}${SCALP_STRATEGY_REL}`;
        console.log("returning ", oneMinStratUrl);
        setStrategyUrl(oneMinStratUrl);
        return oneMinStratUrl;
      case "RSI":
        const rsiStratUrl = `${BACKEND_BASE_URL}${RSI_STRATEGY_REL}`;
        console.log("returning ", rsiStratUrl);
        setStrategyUrl(rsiStratUrl);
        return rsiStratUrl;
      case "BollingerBands":
        const bbStratUrl = `${BACKEND_BASE_URL}${BOLLINGER_BANDS_STRATEGY_REL}`;
        console.log("returning ", bbStratUrl);
        setStrategyUrl(bbStratUrl);
        return bbStratUrl;
      case "MovingAverageCrossover":
        const maCrossoverStratUrl = `${BACKEND_BASE_URL}${MOVING_AVERAGE_CROSSOVER_STRATEGY_REL}`;
        console.log("returning ", maCrossoverStratUrl);
        setStrategyUrl(maCrossoverStratUrl);
        return maCrossoverStratUrl;
      default:
        return null;
    }
  }

  return (
    <form onSubmit={handleSubmit} className="bg-transparent">
      <h3 className="text-lg font-medium text-slate-800 dark:text-slate-100 mb-4">
        Backtest Configuration
      </h3>

      <div className="flex flex-wrap gap-4 items-end">
        {/* Symbol */}
        <div className="flex-shrink-0">
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
            className="w-32 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Strategy */}
        <div className="flex-shrink-0">
          <label
            htmlFor="strategy"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Strategy
          </label>
          <select
            id="strategy"
            name="strategy"
            value={formData.strategy}
            onChange={handleChange}
            className="px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            {Strategy.map((strategy, index) => (
              <option key={`${strategy}${index}`} value={strategy}>
                {strategy}
              </option>
            ))}
          </select>
        </div>

        {/* Start Date */}
        <div className="flex-shrink-0">
          <label
            htmlFor="startDate"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Start Date
          </label>
          <input
            type="date"
            id="startDate"
            name="startDate"
            value={formData.startDate}
            onChange={handleChange}
            required
            className="px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* End Date */}
        <div className="flex-shrink-0">
          <label
            htmlFor="endDate"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            End Date
          </label>
          <input
            type="date"
            id="endDate"
            name="endDate"
            value={formData.endDate}
            onChange={handleChange}
            required
            className="px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Timeframe */}
        <div className="flex-shrink-0">
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
            className="px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="1Min">1 Minute</option>
            <option value="5Min">5 Minutes</option>
            <option value="15Min">15 Minutes</option>
            <option value="1Hour">1 Hour</option>
            <option value="1Day">1 Day</option>
          </select>
        </div>

        {/* Grid Levels - Grid only */}
        {strategy === "Grid" && (
          <div className="flex-shrink-0">
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
              min={1}
              max={12}
              required
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Grid Range - Grid only */}
        {strategy === "Grid" && (
          <div className="flex-shrink-0">
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
              className="w-32 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Initial Capital */}
        <div className="flex-shrink-0">
          <label
            htmlFor="initialCapital"
            className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
          >
            Initial Capital
          </label>
          <input
            type="number"
            id="initialCapital"
            name="initialCapital"
            value={formData.initialCapital}
            onChange={handleChange}
            required
            className="w-32 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Order Size - Grid only */}
        {strategy === "Grid" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="orderSize"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Order Size
            </label>
            <input
              type="number"
              id="orderSize"
              name="orderSize"
              value={formData.orderSize}
              onChange={handleChange}
              required
              className="w-32 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* RSI Period - RSI only */}
        {strategy === "RSI" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="rsiPeriod"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              RSI Period
            </label>
            <input
              type="number"
              id="rsiPeriod"
              name="rsiPeriod"
              value={formData.rsiPeriod}
              onChange={handleChange}
              required
              min={2}
              max={50}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Oversold Level - RSI only */}
        {strategy === "RSI" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="oversoldLevel"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Oversold Level
            </label>
            <input
              type="number"
              id="oversoldLevel"
              name="oversoldLevel"
              value={formData.oversoldLevel}
              onChange={handleChange}
              required
              min={0}
              max={50}
              step={1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Overbought Level - RSI only */}
        {strategy === "RSI" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="overboughtLevel"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Overbought Level
            </label>
            <input
              type="number"
              id="overboughtLevel"
              name="overboughtLevel"
              value={formData.overboughtLevel}
              onChange={handleChange}
              required
              min={50}
              max={100}
              step={1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Risk Per Trade - RSI and Bollinger Bands */}
        {(strategy === "RSI" || strategy === "BollingerBands") && (
          <div className="flex-shrink-0">
            <label
              htmlFor="riskPerTrade"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Risk Per Trade (%)
            </label>
            <input
              type="number"
              id="riskPerTrade"
              name="riskPerTrade"
              value={formData.riskPerTrade}
              onChange={handleChange}
              required
              min={0.1}
              max={10}
              step={0.1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Take Profit - RSI and Bollinger Bands */}
        {(strategy === "RSI" || strategy === "BollingerBands") && (
          <div className="flex-shrink-0">
            <label
              htmlFor="takeProfitPercent"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Take Profit (%)
            </label>
            <input
              type="number"
              id="takeProfitPercent"
              name="takeProfitPercent"
              value={formData.takeProfitPercent}
              onChange={handleChange}
              required
              min={0.1}
              max={20}
              step={0.1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Stop Loss - RSI and Bollinger Bands */}
        {(strategy === "RSI" || strategy === "BollingerBands") && (
          <div className="flex-shrink-0">
            <label
              htmlFor="stopLossPercent"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Stop Loss (%)
            </label>
            <input
              type="number"
              id="stopLossPercent"
              name="stopLossPercent"
              value={formData.stopLossPercent}
              onChange={handleChange}
              required
              min={0.1}
              max={20}
              step={0.1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Bollinger Bands Period - Bollinger Bands only */}
        {strategy === "BollingerBands" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="bbPeriod"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              BB Period
            </label>
            <input
              type="number"
              id="bbPeriod"
              name="bbPeriod"
              value={formData.bbPeriod}
              onChange={handleChange}
              required
              min={2}
              max={50}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Standard Deviations - Bollinger Bands only */}
        {strategy === "BollingerBands" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="standardDeviations"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Std Dev
            </label>
            <input
              type="number"
              id="standardDeviations"
              name="standardDeviations"
              value={formData.standardDeviations}
              onChange={handleChange}
              required
              min={1.0}
              max={3.0}
              step={0.1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Fast Period - Moving Average Crossover only */}
        {strategy === "MovingAverageCrossover" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="fastPeriod"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Fast Period
            </label>
            <input
              type="number"
              id="fastPeriod"
              name="fastPeriod"
              value={formData.fastPeriod}
              onChange={handleChange}
              required
              min={2}
              max={200}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Slow Period - Moving Average Crossover only */}
        {strategy === "MovingAverageCrossover" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="slowPeriod"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Slow Period
            </label>
            <input
              type="number"
              id="slowPeriod"
              name="slowPeriod"
              value={formData.slowPeriod}
              onChange={handleChange}
              required
              min={2}
              max={500}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Risk Per Trade - Moving Average Crossover */}
        {strategy === "MovingAverageCrossover" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="riskPerTrade"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Risk Per Trade (%)
            </label>
            <input
              type="number"
              id="riskPerTrade"
              name="riskPerTrade"
              value={formData.riskPerTrade}
              onChange={handleChange}
              required
              min={0.1}
              max={10}
              step={0.1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Take Profit - Moving Average Crossover */}
        {strategy === "MovingAverageCrossover" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="takeProfitPercent"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Take Profit (%)
            </label>
            <input
              type="number"
              id="takeProfitPercent"
              name="takeProfitPercent"
              value={formData.takeProfitPercent}
              onChange={handleChange}
              required
              min={0.1}
              max={20}
              step={0.1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Stop Loss - Moving Average Crossover */}
        {strategy === "MovingAverageCrossover" && (
          <div className="flex-shrink-0">
            <label
              htmlFor="stopLossPercent"
              className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1"
            >
              Stop Loss (%)
            </label>
            <input
              type="number"
              id="stopLossPercent"
              name="stopLossPercent"
              value={formData.stopLossPercent}
              onChange={handleChange}
              required
              min={0.1}
              max={20}
              step={0.1}
              className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {/* Heikin Ashi Checkbox - Scalping only */}
        {strategy === "OneMinuteScalp" && (
          <div className="flex-shrink-0 flex items-center pb-2">
            <input
              type="checkbox"
              id="useHeikinAshi"
              name="useHeikinAshi"
              checked={formData.useHeikinAshi}
              onChange={handleChange}
              className="h-4 w-4 text-blue-600 dark:text-blue-400 focus:ring-blue-500 border-slate-300 dark:border-slate-600 rounded"
            />
            <label
              htmlFor="useHeikinAshi"
              className="ml-2 text-sm text-slate-700 dark:text-slate-300"
            >
              Heikin Ashi
            </label>
          </div>
        )}

        {/* Submit Button */}
        <div className="flex-shrink-0 ml-auto">
          <button
            type="submit"
            disabled={isLoading}
            className={`py-2 px-6 rounded-md text-white font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 ${
              isLoading
                ? "bg-slate-400 cursor-not-allowed"
                : "bg-blue-600 hover:bg-blue-700"
            }`}
          >
            {isLoading ? "Running..." : "Run Backtest"}
          </button>
        </div>
      </div>
    </form>
  );
};

export default BacktestForm;
