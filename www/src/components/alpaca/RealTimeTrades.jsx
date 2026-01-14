import React, { useState, useEffect } from "react";
import { getAlpacaOrders } from "../../../services/api";
import {
  ArrowTrendingUpIcon,
  ArrowTrendingDownIcon,
  ClockIcon,
} from "@heroicons/react/24/outline";

const RealTimeTrades = ({ isBotRunning = false }) => {
  const [recentTrades, setRecentTrades] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadRecentTrades();
    // Only poll continuously if bot is running, otherwise just load once
    if (isBotRunning) {
      const interval = setInterval(loadRecentTrades, 3000); // Refresh every 3 seconds for real-time feel
      return () => clearInterval(interval);
    }
  }, [isBotRunning]);

  const loadRecentTrades = async () => {
    try {
      const orders = await getAlpacaOrders("Filled");
      // Sort by filled time, most recent first
      const sorted = (Array.isArray(orders) ? orders : [])
        .filter((o) => o.status === "Filled" && o.filledAt)
        .sort((a, b) => new Date(b.filledAt) - new Date(a.filledAt))
        .slice(0, 20); // Show last 20 trades

      setRecentTrades(sorted);
      setLoading(false);
    } catch (error) {
      console.error("Error loading recent trades:", error);
      setLoading(false);
    }
  };

  const formatTime = (dateString) => {
    if (!dateString) return "N/A";
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffSecs = Math.floor(diffMs / 1000);
    const diffMins = Math.floor(diffSecs / 60);

    if (diffSecs < 60) {
      return `${diffSecs}s ago`;
    } else if (diffMins < 60) {
      return `${diffMins}m ago`;
    } else {
      return date.toLocaleTimeString();
    }
  };

  if (loading && recentTrades.length === 0) {
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

  return (
    <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg border border-slate-200 dark:border-slate-700">
      <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between">
        <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
          Recent Trades
        </h3>
        <div className="flex items-center gap-2">
          <div className="h-2 w-2 bg-green-500 dark:bg-green-400 rounded-full animate-pulse"></div>
          <span className="text-xs text-slate-500 dark:text-slate-400">
            Live
          </span>
        </div>
      </div>

      {recentTrades.length === 0 ? (
        <div className="p-12 text-center">
          <ClockIcon className="h-12 w-12 text-slate-300 dark:text-slate-600 mx-auto mb-2" />
          <p className="text-slate-500 dark:text-slate-400">No recent trades</p>
          <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">
            Trades will appear here as they execute
          </p>
        </div>
      ) : (
        <div className="divide-y divide-slate-200 dark:divide-slate-700 max-h-96 overflow-y-auto">
          {recentTrades.map((trade, index) => (
            <div
              key={trade.orderId || index}
              className="px-6 py-3 hover:bg-white dark:hover:bg-slate-700 transition-colors animate-fade-in"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div className="flex-shrink-0">
                    {trade.side === "Buy" ? (
                      <ArrowTrendingUpIcon className="h-5 w-5 text-green-500 dark:text-green-400" />
                    ) : (
                      <ArrowTrendingDownIcon className="h-5 w-5 text-red-500 dark:text-red-400" />
                    )}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                        {trade.symbol || "N/A"}
                      </p>
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                          trade.side === "Buy"
                            ? "bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200"
                            : "bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200"
                        }`}
                      >
                        {trade.side || "N/A"}
                      </span>
                    </div>
                    <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                      {trade.filledQuantity?.toFixed(8) || "0"} @ $
                      {trade.limitPrice?.toFixed(2) || trade.filledAt
                        ? "Market"
                        : "N/A"}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-xs text-slate-500 dark:text-slate-400">
                    {formatTime(trade.filledAt)}
                  </p>
                  <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">
                    {trade.filledAt
                      ? new Date(trade.filledAt).toLocaleTimeString()
                      : ""}
                  </p>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default RealTimeTrades;
