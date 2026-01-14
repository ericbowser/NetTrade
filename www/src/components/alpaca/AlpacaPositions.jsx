import React, { useState, useEffect } from "react";
import { getAlpacaPositions, getAlpacaOrders } from "../../../services/api";
import {
  ArrowTrendingUpIcon,
  ArrowTrendingDownIcon,
  ClockIcon,
} from "@heroicons/react/24/outline";

const AlpacaPositions = ({ isBotRunning = false }) => {
  const [positions, setPositions] = useState([]);
  const [pendingOrders, setPendingOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showPendingOrders, setShowPendingOrders] = useState(true);

  useEffect(() => {
    loadPositions();
    loadPendingOrders();
    // Only poll continuously if bot is running, otherwise just load once
    if (isBotRunning) {
      const interval = setInterval(() => {
        loadPositions();
        loadPendingOrders();
      }, 10000); // Refresh every 10 seconds
      return () => clearInterval(interval);
    }
  }, [isBotRunning]);

  const loadPositions = async () => {
    try {
      setLoading(true);
      const data = await getAlpacaPositions();
      setPositions(Array.isArray(data) ? data : []);
      setError(null);
    } catch (err) {
      console.error("Error loading positions:", err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const loadPendingOrders = async () => {
    try {
      const allOrders = await getAlpacaOrders();
      // Filter for pending orders (New, PartiallyFilled, or Accepted)
      const pending = Array.isArray(allOrders)
        ? allOrders.filter(
            (order) =>
              order.status === "New" ||
              order.status === "PartiallyFilled" ||
              order.status === "Accepted"
          )
        : [];
      setPendingOrders(pending);
    } catch (err) {
      console.error("Error loading pending orders:", err);
    }
  };

  if (loading && positions.length === 0) {
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
          <p className="font-medium">Error loading positions</p>
          <p className="text-sm mt-1">{error}</p>
          <button
            onClick={loadPositions}
            className="mt-2 text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg border border-slate-200 dark:border-slate-700">
      <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
            Open Positions & Pending Orders
          </h3>
          {pendingOrders.length > 0 && (
            <button
              onClick={() => setShowPendingOrders(!showPendingOrders)}
              className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
            >
              {showPendingOrders ? "Hide" : "Show"} Pending (
              {pendingOrders.length})
            </button>
          )}
        </div>
        <button
          onClick={() => {
            loadPositions();
            loadPendingOrders();
          }}
          className="text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
        >
          Refresh
        </button>
      </div>

      {positions.length === 0 && pendingOrders.length === 0 ? (
        <div className="p-12 text-center">
          <p className="text-slate-500 dark:text-slate-400">
            No open positions or pending orders
          </p>
        </div>
      ) : (
        <div className="divide-y divide-slate-200 dark:divide-slate-700">
          {/* Open Positions */}
          {positions.map((position, index) => (
            <div
              key={`pos-${index}`}
              className="px-6 py-4 hover:bg-white dark:hover:bg-slate-700 transition-colors"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-4">
                  <div className="flex-shrink-0">
                    {position.unrealizedProfitLoss >= 0 ? (
                      <ArrowTrendingUpIcon className="h-5 w-5 text-green-500 dark:text-green-400" />
                    ) : (
                      <ArrowTrendingDownIcon className="h-5 w-5 text-red-500 dark:text-red-400" />
                    )}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                        {position.symbol || "N/A"}
                      </p>
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200">
                        Position
                      </span>
                    </div>
                    <p className="text-xs text-slate-500 dark:text-slate-400">
                      Quantity: {position.quantity?.toFixed(8) || "0"} | Avg
                      Entry: ${position.averageEntryPrice?.toFixed(2) || "0.00"}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p
                    className={`text-sm font-semibold ${
                      position.unrealizedProfitLoss >= 0
                        ? "text-green-600 dark:text-green-400"
                        : "text-red-600 dark:text-red-400"
                    }`}
                  >
                    {position.unrealizedProfitLoss >= 0 ? "+" : ""}$
                    {position.unrealizedProfitLoss?.toFixed(2) || "0.00"}
                  </p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">
                    {position.unrealizedProfitLossPercent >= 0 ? "+" : ""}
                    {position.unrealizedProfitLossPercent?.toFixed(2) || "0.00"}
                    %
                  </p>
                  <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                    Current: ${position.currentPrice?.toFixed(2) || "0.00"} |
                    Value: ${position.marketValue?.toFixed(2) || "0.00"}
                  </p>
                </div>
              </div>
            </div>
          ))}

          {/* Pending Orders */}
          {showPendingOrders &&
            pendingOrders.map((order, index) => (
              <div
                key={`order-${order.orderId || index}`}
                className="px-6 py-4 hover:bg-white dark:hover:bg-slate-700 bg-yellow-50/50 dark:bg-yellow-900/20 transition-colors"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4">
                    <div className="flex-shrink-0">
                      <ClockIcon className="h-5 w-5 text-yellow-500 dark:text-yellow-400" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                          {order.symbol || "N/A"}
                        </p>
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200">
                          {order.status || "Pending"}
                        </span>
                      </div>
                      <p className="text-xs text-slate-500 dark:text-slate-400">
                        {order.side || "N/A"} • {order.type || "N/A"} • Qty:{" "}
                        {order.quantity?.toFixed(8) || "0"}
                        {order.filledQuantity > 0 &&
                          ` (${order.filledQuantity.toFixed(8)} filled)`}
                        {order.limitPrice &&
                          ` • Limit: $${order.limitPrice.toFixed(2)}`}
                      </p>
                      <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">
                        {order.submittedAt
                          ? new Date(order.submittedAt).toLocaleString()
                          : "N/A"}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            ))}
        </div>
      )}
    </div>
  );
};

export default AlpacaPositions;
