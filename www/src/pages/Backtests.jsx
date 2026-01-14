import React, { useState } from "react";
import { useSearchParams } from "react-router-dom";
import BacktestForm from "../components/BacktestForm";
import BacktestChart from "../components/BacktestChart";
import TradeList from "../components/TradeList";
import getBacktestResults from "../../services/api.js";

const Backtests = () => {
  const [searchParams] = useSearchParams();
  const [backtestResults, setBacktestResults] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState("chart");

  const handleRunBacktest = async (backtestRequest, strategyUrl) => {
    setLoading(true);
    setError(null);

    try {
      const results = await getBacktestResults(backtestRequest, strategyUrl);
      setBacktestResults(results);
      setActiveTab("chart");
    } catch (err) {
      setError(
        "Error running backtest. Please check your inputs and try again."
      );
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
          Backtest Analysis
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Test your trading strategies against historical data
        </p>
      </div>

      {/* Backtest Form */}
      <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <BacktestForm onSubmit={handleRunBacktest} isLoading={loading} />
      </div>

      {/* Results */}
      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 px-4 py-3 rounded-md">
          {error}
        </div>
      )}

      {loading ? (
        <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-12 border border-slate-200 dark:border-slate-700">
          <div className="flex items-center justify-center">
            <svg
              className="animate-spin h-8 w-8 text-blue-500 dark:text-blue-400"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              ></circle>
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              ></path>
            </svg>
            <span className="ml-3 text-slate-600 dark:text-slate-300">
              Running backtest...
            </span>
          </div>
        </div>
      ) : backtestResults ? (
        <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg border border-slate-200 dark:border-slate-700">
          {/* Tabs */}
          <div className="border-b border-slate-200 dark:border-slate-700">
            <nav className="-mb-px flex space-x-8 px-6">
              <button
                onClick={() => setActiveTab("chart")}
                className={`${
                  activeTab === "chart"
                    ? "border-blue-500 dark:border-blue-400 text-blue-600 dark:text-blue-400"
                    : "border-transparent text-slate-500 dark:text-slate-400 hover:border-slate-300 dark:hover:border-slate-600 hover:text-slate-700 dark:hover:text-slate-300"
                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}
              >
                Performance Chart
              </button>
              <button
                onClick={() => setActiveTab("trades")}
                className={`${
                  activeTab === "trades"
                    ? "border-blue-500 dark:border-blue-400 text-blue-600 dark:text-blue-400"
                    : "border-transparent text-slate-500 dark:text-slate-400 hover:border-slate-300 dark:hover:border-slate-600 hover:text-slate-700 dark:hover:text-slate-300"
                } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}
              >
                Trade History
              </button>
            </nav>
          </div>

          {/* Tab Content */}
          <div className="p-6">
            {activeTab === "chart" && <BacktestChart data={backtestResults} />}
            {activeTab === "trades" && (
              <TradeList trades={backtestResults.trades} />
            )}
          </div>
        </div>
      ) : (
        <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-12 text-center border border-slate-200 dark:border-slate-700">
          <p className="text-slate-500 dark:text-slate-400">
            Configure and run a backtest to see results
          </p>
        </div>
      )}
    </div>
  );
};

export default Backtests;
