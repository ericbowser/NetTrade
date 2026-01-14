import React, { useState, useEffect } from 'react';
import { XMarkIcon } from '@heroicons/react/24/outline';
import Strategy from '../../../util/Strategy';

const StrategyModal = ({ strategy, onClose, onSave }) => {
  const [formData, setFormData] = useState({
    name: '',
    type: 'Grid',
    description: '',
    symbol: 'BTC/USD',
    timeframe: '1Min',
    initialCapital: 5000,
    gridLevels: 10,
    gridRange: 7,
    orderSize: 250,
    riskPerTrade: 0.02,
    takeProfitPips: 8,
    stopLossPips: 8,
    useHeikinAshi: true,
  });

  useEffect(() => {
    if (strategy) {
      setFormData({
        name: strategy.name || '',
        type: strategy.type || 'Grid',
        description: strategy.description || '',
        ...strategy.config || {}
      });
    }
  }, [strategy]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData({
      ...formData,
      [name]: type === 'checkbox' ? checked : value
    });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    onSave(formData);
  };

  const renderStrategyFields = () => {
    switch (formData.type) {
      case 'Grid':
        return (
          <>
            <div>
              <label className="block text-sm font-medium text-slate-700">Grid Levels</label>
              <input
                type="number"
                name="gridLevels"
                value={formData.gridLevels}
                onChange={handleChange}
                className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                min="1"
                max="20"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700">Grid Range (%)</label>
              <input
                type="number"
                name="gridRange"
                value={formData.gridRange}
                onChange={handleChange}
                className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                step="0.1"
                min="1"
                max="20"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700">Order Size (USD)</label>
              <input
                type="number"
                name="orderSize"
                value={formData.orderSize}
                onChange={handleChange}
                className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                step="0.01"
                min="1"
              />
            </div>
          </>
        );
      case 'Scalping':
      case 'OneMinuteScalp':
        return (
          <>
            <div>
              <label className="block text-sm font-medium text-slate-700">Risk Per Trade</label>
              <input
                type="number"
                name="riskPerTrade"
                value={formData.riskPerTrade}
                onChange={handleChange}
                className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                step="0.01"
                min="0.01"
                max="0.1"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700">Take Profit (Pips)</label>
              <input
                type="number"
                name="takeProfitPips"
                value={formData.takeProfitPips}
                onChange={handleChange}
                className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                min="1"
                max="50"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700">Stop Loss (Pips)</label>
              <input
                type="number"
                name="stopLossPips"
                value={formData.stopLossPips}
                onChange={handleChange}
                className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                min="1"
                max="50"
              />
            </div>
            <div className="flex items-center">
              <input
                type="checkbox"
                name="useHeikinAshi"
                checked={formData.useHeikinAshi}
                onChange={handleChange}
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-slate-300 rounded"
              />
              <label className="ml-2 block text-sm text-slate-700">Use Heikin Ashi</label>
            </div>
          </>
        );
      default:
        return null;
    }
  };

  return (
    <div className="fixed z-10 inset-0 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div className="fixed inset-0 bg-slate-500 bg-opacity-75 transition-opacity" onClick={onClose}></div>

        <div className="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full">
          <form onSubmit={handleSubmit}>
            <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-medium text-slate-900">
                  {strategy ? 'Edit Strategy' : 'Create New Strategy'}
                </h3>
                <button
                  type="button"
                  onClick={onClose}
                  className="text-slate-400 hover:text-slate-500"
                >
                  <XMarkIcon className="h-6 w-6" />
                </button>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700">Strategy Name</label>
                  <input
                    type="text"
                    name="name"
                    value={formData.name}
                    onChange={handleChange}
                    required
                    className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">Strategy Type</label>
                  <select
                    name="type"
                    value={formData.type}
                    onChange={handleChange}
                    className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                  >
                    {Strategy.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">Description</label>
                  <textarea
                    name="description"
                    value={formData.description}
                    onChange={handleChange}
                    rows="2"
                    className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700">Symbol</label>
                    <input
                      type="text"
                      name="symbol"
                      value={formData.symbol}
                      onChange={handleChange}
                      className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700">Timeframe</label>
                    <select
                      name="timeframe"
                      value={formData.timeframe}
                      onChange={handleChange}
                      className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    >
                      <option value="1Min">1 Minute</option>
                      <option value="5Min">5 Minutes</option>
                      <option value="15Min">15 Minutes</option>
                      <option value="1Hour">1 Hour</option>
                      <option value="1Day">1 Day</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700">Initial Capital (USD)</label>
                  <input
                    type="number"
                    name="initialCapital"
                    value={formData.initialCapital}
                    onChange={handleChange}
                    className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    min="100"
                    step="100"
                  />
                </div>

                <div className="border-t border-slate-200 pt-4">
                  <h4 className="text-sm font-medium text-slate-900 mb-3">Strategy Parameters</h4>
                  <div className="grid grid-cols-2 gap-4">
                    {renderStrategyFields()}
                  </div>
                </div>
              </div>
            </div>

            <div className="bg-slate-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button
                type="submit"
                className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm"
              >
                {strategy ? 'Update Strategy' : 'Create Strategy'}
              </button>
              <button
                type="button"
                onClick={onClose}
                className="mt-3 w-full inline-flex justify-center rounded-md border border-slate-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-slate-700 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default StrategyModal;


