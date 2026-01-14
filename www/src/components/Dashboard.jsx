// src/components/Dashboard.js
import React, { useState } from 'react';
import BacktestForm from './BacktestForm';
import BacktestChart from './BacktestChart';
import TradeList from './TradeList';
import getBacktestResults from '../../services/api';

const Dashboard = () => {
  const [backtestResults, setBacktestResults] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState('chart');

  const handleRunBacktest = async (backtestRequest, strategyUrl) => {
    setLoading(true);
    setError(null);

    try {
      const results = await getBacktestResults(backtestRequest, strategyUrl);
      setBacktestResults(results);
      setActiveTab('chart'); // Switch to chart tab when new results arrive
    } catch (err) {
      setError('Error running backtest. Please check your inputs and try again.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col h-screen">
      {/* Header */}
      <header className="bg-slate-800 text-white p-4 shadow-md">
        <h1 className="text-xl font-semibold">CoinAPI Trading Dashboard</h1>
      </header>

      {/* Form Section - Top */}
      <div className="bg-slate-50 border-b border-slate-200 p-4">
        <BacktestForm onSubmit={handleRunBacktest} isLoading={loading} />
      </div>

      {/* Results Section - Bottom */}
      <div className="flex-1 p-4 overflow-y-auto">
        {error && (
          <div className="bg-red-50 text-red-700 p-4 rounded-md mb-4">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex items-center justify-center h-96 bg-slate-50 rounded-md text-slate-600">
            <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Running backtest...
          </div>
        ) : (
          backtestResults ? (
            <div>
              {/* Tabs */}
              <div className="border-b border-slate-200 mb-4">
                <nav className="-mb-px flex space-x-8">
                  <button
                    onClick={() => setActiveTab('chart')}
                    className={`${
                      activeTab === 'chart'
                        ? 'border-blue-500 text-blue-600'
                        : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
                    } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}
                  >
                    Performance Chart
                  </button>
                  <button
                    onClick={() => setActiveTab('trades')}
                    className={`${
                      activeTab === 'trades'
                        ? 'border-blue-500 text-blue-600'
                        : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
                    } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}
                  >
                    Trade History
                  </button>
                </nav>
              </div>

              {/* Tab Content */}
              <div>
                {activeTab === 'chart' && <BacktestChart data={backtestResults} />}
                {activeTab === 'trades' && <TradeList trades={backtestResults.trades} />}
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center h-96 bg-slate-50 rounded-md text-slate-600">
              <p>Configure and run a backtest to see results</p>
            </div>
          )
        )}
      </div>
    </div>
  );
};

export default Dashboard;