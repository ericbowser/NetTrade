import React, { useState } from 'react';
import Strategy from '../../util/Strategy';
import { BACKEND_BASE_URL, COINBASE_GRID_LIVE_REL, COINBASE_SCALP_LIVE_REL } from '../config/env';

const LiveTradingForm = ({ onSubmit, isLoading, onStop, isRunning, sessionId }) => {
  const [formData, setFormData] = useState({
    exchange: 'Coinbase',
    symbol: 'BTC-USD',
    strategy: Strategy[0],
    timeframe: '1Min',
    gridLevels: 10,
    gridRange: 7,
    orderSize: 250.00,
    useHeikinAshi: true,
    initialCapital: 5000
  });

  const [strategy, setStrategy] = useState(Strategy[0]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    if (name === 'strategy') {
      setStrategy(value);
    }
    setFormData({
      ...formData,
      [name]: type === 'checkbox' ? checked : value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const url = decideStrategyUrl();
    if (!url) {
      console.error('No strategy URL determined');
      return;
    }

    // Convert form data to the format expected by the API
    const config = {
      symbol: formData.symbol,
      exchange: formData.exchange,
      useHeikinAshi: formData.useHeikinAshi,
      timeframe: formData.timeframe,
      gridLevels: parseInt(formData.gridLevels),
      gridRange: parseFloat(formData.gridRange),
      orderSize: parseFloat(formData.orderSize)
    };

    const tradingRequest = {
      configuration: config,
      initialCapital: parseFloat(formData.initialCapital)
    };

    console.log('Submitting live trading request:', tradingRequest);
    onSubmit(tradingRequest, url);
  };

  function decideStrategyUrl() {
    switch (strategy) {
      case "Grid":
        return `${BACKEND_BASE_URL}${COINBASE_GRID_LIVE_REL}`;
      case "OneMinuteScalp":
        return `${BACKEND_BASE_URL}${COINBASE_SCALP_LIVE_REL}`;
      default:
        return null;
    }
  }

  if (isRunning) {
    return (
      <div className="bg-white p-4 rounded-md shadow-sm">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-medium text-slate-800 mb-2">Live Trading Active</h3>
            <p className="text-sm text-slate-600">
              Strategy: <span className="font-medium">{strategy}</span> | 
              Symbol: <span className="font-medium">{formData.symbol}</span> | 
              Exchange: <span className="font-medium">{formData.exchange}</span>
            </p>
            <p className="text-xs text-slate-500 mt-1">Session ID: {sessionId}</p>
          </div>
          <button
            onClick={onStop}
            disabled={isLoading}
            className="px-6 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? 'Stopping...' : 'Stop Trading'}
          </button>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="bg-white p-4 rounded-md shadow-sm">
      <h3 className="text-lg font-medium text-slate-800 mb-4">Live Trading Configuration</h3>

      <div className="flex flex-wrap gap-4 items-end">
        {/* Exchange */}
        <div className="flex-shrink-0">
          <label htmlFor="exchange" className="block text-sm font-medium text-slate-700 mb-1">
            Exchange
          </label>
          <select
            id="exchange"
            name="exchange"
            value={formData.exchange}
            onChange={handleChange}
            required
            className="px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="Coinbase">Coinbase</option>
          </select>
        </div>

        {/* Symbol */}
        <div className="flex-shrink-0">
          <label htmlFor="symbol" className="block text-sm font-medium text-slate-700 mb-1">
            Symbol
          </label>
          <input
            type="text"
            id="symbol"
            name="symbol"
            value={formData.symbol}
            onChange={handleChange}
            required
            placeholder="BTC-USD"
            className="w-32 px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Strategy */}
        <div className="flex-shrink-0">
          <label className="block text-sm font-medium text-slate-700 mb-1">
            Strategy
          </label>
          <div className="flex gap-4">
            {Strategy.map((strat, index) => (
              <label
                key={`${strat}${index}`}
                className="flex items-center cursor-pointer group"
              >
                <input
                  type="radio"
                  name="strategy"
                  value={strat}
                  checked={formData.strategy === strat}
                  onChange={handleChange}
                  className="h-4 w-4 text-blue-600 border-slate-300 focus:ring-blue-500"
                />
                <span className="ml-2 text-sm text-slate-700 group-hover:text-slate-900">
                  {strat}
                </span>
              </label>
            ))}
          </div>
        </div>

        {/* Timeframe */}
        <div className="flex-shrink-0">
          <label htmlFor="timeframe" className="block text-sm font-medium text-slate-700 mb-1">
            Timeframe
          </label>
          <select
            id="timeframe"
            name="timeframe"
            value={formData.timeframe}
            onChange={handleChange}
            className="px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="1Min">1 Minute</option>
            <option value="5Min">5 Minutes</option>
            <option value="15Min">15 Minutes</option>
            <option value="1Hour">1 Hour</option>
            <option value="1Day">1 Day</option>
          </select>
        </div>

        {/* Grid Levels */}
        <div className="flex-shrink-0">
          <label htmlFor="gridLevels" className="block text-sm font-medium text-slate-700 mb-1">
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
            className="w-24 px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Grid Range */}
        <div className="flex-shrink-0">
          <label htmlFor="gridRange" className="block text-sm font-medium text-slate-700 mb-1">
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
            className="w-32 px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Initial Capital */}
        <div className="flex-shrink-0">
          <label htmlFor="initialCapital" className="block text-sm font-medium text-slate-700 mb-1">
            Initial Capital
          </label>
          <input
            type="number"
            id="initialCapital"
            name="initialCapital"
            value={formData.initialCapital}
            onChange={handleChange}
            required
            className="w-32 px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Order Size */}
        <div className="flex-shrink-0">
          <label htmlFor="orderSize" className="block text-sm font-medium text-slate-700 mb-1">
            Order Size
          </label>
          <input
            type="number"
            id="orderSize"
            name="orderSize"
            value={formData.orderSize}
            onChange={handleChange}
            required
            className="w-32 px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Heikin Ashi Checkbox */}
        <div className="flex-shrink-0 flex items-center pb-2">
          <input
            type="checkbox"
            id="useHeikinAshi"
            name="useHeikinAshi"
            checked={formData.useHeikinAshi}
            onChange={handleChange}
            className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-slate-300 rounded"
          />
          <label htmlFor="useHeikinAshi" className="ml-2 text-sm text-slate-700">
            Heikin Ashi
          </label>
        </div>

        {/* Submit Button */}
        <div className="flex-shrink-0 ml-auto">
          <button
            type="submit"
            disabled={isLoading}
            className={`py-2 px-6 rounded-md text-white font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 ${
              isLoading
                ? 'bg-slate-400 cursor-not-allowed'
                : 'bg-green-600 hover:bg-green-700'
            }`}
          >
            {isLoading ? 'Starting...' : 'Start Live Trading'}
          </button>
        </div>
      </div>
    </form>
  );
};

export default LiveTradingForm;



