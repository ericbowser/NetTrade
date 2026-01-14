import React, { useState, useEffect } from "react";
import { getAlpacaAccount } from "../../../services/api";

const AlpacaAccountInfo = ({ isBotRunning = false }) => {
  const [account, setAccount] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadAccount();
    // Only poll continuously if bot is running, otherwise just load once
    if (isBotRunning) {
      const interval = setInterval(loadAccount, 30000); // Refresh every 30 seconds
      return () => clearInterval(interval);
    }
  }, [isBotRunning]);

  const loadAccount = async () => {
    try {
      setLoading(true);
      const data = await getAlpacaAccount();
      setAccount(data);
      setError(null);
    } catch (err) {
      console.error("Error loading account:", err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  if (loading && !account) {
    return (
      <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <div className="animate-pulse">
          <div className="h-4 bg-slate-200 dark:bg-slate-700 rounded w-1/4 mb-4"></div>
          <div className="space-y-3">
            <div className="h-4 bg-slate-200 dark:bg-slate-700 rounded"></div>
            <div className="h-4 bg-slate-200 dark:bg-slate-700 rounded w-5/6"></div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <div className="text-red-600 dark:text-red-400">
          <p className="font-medium">Error loading account</p>
          <p className="text-sm mt-1">{error}</p>
          <button
            onClick={loadAccount}
            className="mt-2 text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!account) {
    return null;
  }

  return (
    <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-6 border border-slate-200 dark:border-slate-700">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
          Alpaca Paper Account
        </h3>
        <button
          onClick={loadAccount}
          className="text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
        >
          Refresh
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white dark:bg-slate-700 p-4 rounded-lg shadow-md border border-slate-200 dark:border-slate-600">
          <div className="text-xs text-slate-500 dark:text-slate-400 uppercase mb-1">
            Equity
          </div>
          <div className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
            $
            {account.equity?.toLocaleString("en-US", {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            }) || "0.00"}
          </div>
        </div>

        <div className="bg-white dark:bg-slate-700 p-4 rounded-lg shadow-md border border-slate-200 dark:border-slate-600">
          <div className="text-xs text-slate-500 dark:text-slate-400 uppercase mb-1">
            Buying Power
          </div>
          <div className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
            $
            {account.buyingPower?.toLocaleString("en-US", {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            }) || "0.00"}
          </div>
        </div>

        <div className="bg-white dark:bg-slate-700 p-4 rounded-lg shadow-md border border-slate-200 dark:border-slate-600">
          <div className="text-xs text-slate-500 dark:text-slate-400 uppercase mb-1">
            Account Status
          </div>
          <div className="mt-2">
            {account.tradingBlocked ? (
              <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200">
                Trading Blocked
              </span>
            ) : (
              <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200">
                Active
              </span>
            )}
          </div>
        </div>
      </div>

      <div className="mt-4 pt-4 border-t border-slate-200 dark:border-slate-700">
        <div className="text-sm text-slate-600 dark:text-slate-300">
          <p>
            Account Number:{" "}
            <span className="font-medium">
              {account.accountNumber || "N/A"}
            </span>
          </p>
        </div>
      </div>
    </div>
  );
};

export default AlpacaAccountInfo;
