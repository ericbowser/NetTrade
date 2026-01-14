import React, { useState, useEffect } from "react";
import {
  getAlpacaOrders,
  cancelAlpacaOrder,
  cancelAllAlpacaOrders,
} from "../../../services/api";
import {
  ClockIcon,
  CheckCircleIcon,
  XCircleIcon,
  XMarkIcon,
} from "@heroicons/react/24/outline";

const AlpacaOrders = ({ isBotRunning = false }) => {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState(null);
  const [cancellingAll, setCancellingAll] = useState(false);

  useEffect(() => {
    loadOrders();
    // Only poll continuously if bot is running, otherwise just load once
    if (isBotRunning) {
      const interval = setInterval(loadOrders, 5000); // Refresh every 5 seconds
      return () => clearInterval(interval);
    }
  }, [statusFilter, isBotRunning]);

  const loadOrders = async () => {
    try {
      setLoading(true);
      const data = await getAlpacaOrders(statusFilter);
      setOrders(Array.isArray(data) ? data : []);
      setError(null);
    } catch (err) {
      console.error("Error loading orders:", err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = async (orderId) => {
    if (!confirm("Are you sure you want to cancel this order?")) {
      return;
    }

    try {
      await cancelAlpacaOrder(orderId);
      loadOrders();
    } catch (err) {
      console.error("Error cancelling order:", err);
      alert("Failed to cancel order: " + err.message);
    }
  };

  const handleCancelAll = async () => {
    // Get count of cancellable orders
    const cancellableOrders = orders.filter(
      (order) =>
        order.status === "New" ||
        order.status === "PartiallyFilled" ||
        order.status === "PendingNew" ||
        order.status === "AcceptedForBidding"
    );

    if (cancellableOrders.length === 0) {
      alert("No open orders to cancel");
      return;
    }

    if (
      !confirm(
        `Are you sure you want to cancel all ${cancellableOrders.length} open order(s)?`
      )
    ) {
      return;
    }

    setCancellingAll(true);
    try {
      const result = await cancelAllAlpacaOrders();
      if (result.errors && result.errors.length > 0) {
        alert(
          `Cancelled ${result.cancelledCount} of ${
            result.totalCount
          } orders. Some errors occurred:\n${result.errors.join("\n")}`
        );
      } else {
        alert(`Successfully cancelled ${result.cancelledCount} order(s)`);
      }
      loadOrders();
    } catch (err) {
      console.error("Error cancelling all orders:", err);
      alert(
        "Failed to cancel all orders: " +
          (err.response?.data?.error || err.message)
      );
    } finally {
      setCancellingAll(false);
    }
  };

  const getStatusIcon = (status) => {
    switch (status) {
      case "Filled":
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case "Canceled":
        return <XCircleIcon className="h-5 w-5 text-red-500" />;
      default:
        return <ClockIcon className="h-5 w-5 text-yellow-500" />;
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case "Filled":
        return "bg-green-100 text-green-800";
      case "Canceled":
        return "bg-red-100 text-red-800";
      case "PartiallyFilled":
        return "bg-yellow-100 text-yellow-800";
      default:
        return "bg-blue-100 text-blue-800";
    }
  };

  if (loading && orders.length === 0) {
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
      <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
            Recent Orders
          </h3>
          <div className="flex items-center gap-3">
            {orders.filter(
              (order) =>
                order.status === "New" ||
                order.status === "PartiallyFilled" ||
                order.status === "PendingNew" ||
                order.status === "AcceptedForBidding"
            ).length > 0 && (
              <button
                onClick={handleCancelAll}
                disabled={cancellingAll}
                className="inline-flex items-center px-3 py-1.5 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700 disabled:bg-slate-400 disabled:cursor-not-allowed"
              >
                <XMarkIcon className="h-4 w-4 mr-1.5" />
                {cancellingAll ? "Cancelling..." : "Cancel All Open"}
              </button>
            )}
            <button
              onClick={loadOrders}
              className="text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
            >
              Refresh
            </button>
          </div>
        </div>

        {/* Status Filter */}
        <div className="flex gap-2">
          <button
            onClick={() => setStatusFilter(null)}
            className={`px-3 py-1 text-xs rounded-md ${
              statusFilter === null
                ? "bg-blue-600 text-white"
                : "bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600"
            }`}
          >
            All
          </button>
          <button
            onClick={() => setStatusFilter("New")}
            className={`px-3 py-1 text-xs rounded-md ${
              statusFilter === "New"
                ? "bg-blue-600 text-white"
                : "bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600"
            }`}
          >
            New
          </button>
          <button
            onClick={() => setStatusFilter("Filled")}
            className={`px-3 py-1 text-xs rounded-md ${
              statusFilter === "Filled"
                ? "bg-blue-600 text-white"
                : "bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600"
            }`}
          >
            Filled
          </button>
        </div>
      </div>

      {error ? (
        <div className="p-6 text-red-600 dark:text-red-400">
          <p className="font-medium">Error loading orders</p>
          <p className="text-sm mt-1">{error}</p>
        </div>
      ) : orders.length === 0 ? (
        <div className="p-12 text-center">
          <p className="text-slate-500 dark:text-slate-400">No orders found</p>
        </div>
      ) : (
        <div className="divide-y divide-slate-200 dark:divide-slate-700">
          {orders.map((order, index) => (
            <div
              key={order.orderId || index}
              className="px-6 py-4 hover:bg-slate-50 dark:hover:bg-slate-700"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-4">
                  <div className="flex-shrink-0">
                    {getStatusIcon(order.status)}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                        {order.symbol || "N/A"}
                      </p>
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getStatusColor(
                          order.status
                        )}`}
                      >
                        {order.status || "Unknown"}
                      </span>
                    </div>
                    <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                      {order.side || "N/A"} • {order.type || "N/A"} • Qty:{" "}
                      {order.quantity?.toFixed(8) || "0"} /{" "}
                      {order.filledQuantity?.toFixed(8) || "0"} filled
                      {order.limitPrice &&
                        ` • Limit: $${order.limitPrice.toFixed(2)}`}
                    </p>
                    <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">
                      {order.submittedAt
                        ? new Date(order.submittedAt).toLocaleString()
                        : "N/A"}
                      {order.filledAt &&
                        ` • Filled: ${new Date(
                          order.filledAt
                        ).toLocaleString()}`}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  {order.status === "New" ||
                  order.status === "PartiallyFilled" ? (
                    <button
                      onClick={() => handleCancel(order.orderId)}
                      className="px-3 py-1 text-xs bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300 rounded-md hover:bg-red-200 dark:hover:bg-red-800"
                    >
                      Cancel
                    </button>
                  ) : null}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default AlpacaOrders;
