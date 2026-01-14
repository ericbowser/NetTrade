import React, { useState } from "react";
import {
  placeCoinbaseBuyOrder,
  placeCoinbaseSellOrder,
  placeCoinbaseLimitOrder,
  getCoinbaseOrders,
  cancelCoinbaseOrder,
} from "../../services/api";

const CoinbaseTradingForm = () => {
  const [formData, setFormData] = useState({
    productId: "BTC-USD",
    size: 0.001,
    limitPrice: 0,
    side: "BUY",
  });
  const [orderType, setOrderType] = useState("market"); // 'market' or 'limit'
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const [orders, setOrders] = useState([]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value,
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);
    setSuccess(null);

    if (orderType === "market") {
      // Market order
      try {
        const result =
          formData.side === "BUY"
            ? await placeCoinbaseBuyOrder({
                productId: formData.productId,
                size: parseFloat(formData.size),
              })
            : await placeCoinbaseSellOrder({
                productId: formData.productId,
                size: parseFloat(formData.size),
              });
        setSuccess(
          `${formData.side} order placed: ${result.orderId || result}`
        );
        await loadOrders();
      } catch (err) {
        setError(
          err.response?.data?.error ||
            err.message ||
            `Error placing ${formData.side.toLowerCase()} order`
        );
      } finally {
        setIsLoading(false);
      }
    } else {
      // Limit order
      if (!formData.limitPrice || formData.limitPrice <= 0) {
        setError("Limit price must be greater than 0");
        setIsLoading(false);
        return;
      }

      try {
        const result = await placeCoinbaseLimitOrder({
          productId: formData.productId,
          side: formData.side || "BUY",
          size: parseFloat(formData.size),
          limitPrice: parseFloat(formData.limitPrice),
        });
        setSuccess(`Limit order placed: ${result.orderId || result}`);
        await loadOrders();
      } catch (err) {
        setError(
          err.response?.data?.error ||
            err.message ||
            "Error placing limit order"
        );
      } finally {
        setIsLoading(false);
      }
    }
  };

  const loadOrders = async () => {
    try {
      const result = await getCoinbaseOrders();
      setOrders(Array.isArray(result) ? result : []);
      setError(null); // Clear any previous errors
      setSuccess(null); // Clear success messages
    } catch (err) {
      console.error("Error loading orders:", err);
      console.error("Error response:", err.response);
      console.error("Error data:", err.response?.data);

      // Extract error message from various possible locations
      const errorMessage =
        err.response?.data?.Error ||
        err.response?.data?.error ||
        err.response?.data?.message ||
        err.message ||
        "Error loading orders from Coinbase";

      const errorDetails =
        err.response?.data?.Details || err.response?.data?.details;
      const fullErrorMessage = errorDetails
        ? `${errorMessage}${errorDetails ? `: ${errorDetails}` : ""}`
        : errorMessage;

      setError(fullErrorMessage);
      setOrders([]); // Clear orders on error
    }
  };

  const handleCancelOrder = async (orderId) => {
    if (!window.confirm("Are you sure you want to cancel this order?")) {
      return;
    }

    try {
      await cancelCoinbaseOrder(orderId);
      setSuccess("Order cancelled successfully");
      await loadOrders();
    } catch (err) {
      setError(
        err.response?.data?.error || err.message || "Error cancelling order"
      );
    }
  };

  React.useEffect(() => {
    loadOrders();
    const interval = setInterval(loadOrders, 10000); // Refresh every 10 seconds
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="space-y-6">
      <form
        onSubmit={handleSubmit}
        className="bg-slate-50 dark:bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-200 dark:border-slate-700"
      >
        <h2 className="text-xl font-semibold text-slate-800 dark:text-slate-100 mb-4">
          Coinbase Trading
        </h2>

        {/* Error/Success Messages */}
        {error && (
          <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-md">
            {error}
          </div>
        )}
        {success && (
          <div className="mb-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-md">
            {success}
          </div>
        )}

        {/* Order Form */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Product ID
            </label>
            <input
              type="text"
              name="productId"
              value={formData.productId}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              placeholder="BTC-USD"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Size
            </label>
            <input
              type="number"
              name="size"
              value={formData.size}
              onChange={handleChange}
              step="0.0001"
              min="0"
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Side
            </label>
            <select
              name="side"
              value={formData.side || "BUY"}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="BUY">BUY</option>
              <option value="SELL">SELL</option>
            </select>
          </div>

          {orderType === "limit" && (
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Limit Price
              </label>
              <input
                type="number"
                name="limitPrice"
                value={formData.limitPrice}
                onChange={handleChange}
                step="0.01"
                min="0"
                className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          )}
        </div>

        {/* Order Type Toggle */}
        <div className="flex gap-4 mb-4">
          <button
            type="button"
            onClick={() => setOrderType("market")}
            className={`px-4 py-2 rounded-md transition-colors ${
              orderType === "market"
                ? "bg-blue-600 dark:bg-blue-500 text-white"
                : "bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-300 dark:hover:bg-slate-600"
            }`}
          >
            Market Order
          </button>
          <button
            type="button"
            onClick={() => setOrderType("limit")}
            className={`px-4 py-2 rounded-md ${
              orderType === "limit"
                ? "bg-blue-600 text-white"
                : "bg-slate-200 text-slate-700 hover:bg-slate-300"
            }`}
          >
            Limit Order
          </button>
        </div>

        {/* Submit Button */}
        <button
          type="submit"
          disabled={isLoading}
          className={`px-6 py-2 text-white rounded-md focus:outline-none focus:ring-2 disabled:opacity-50 disabled:cursor-not-allowed ${
            orderType === "market"
              ? formData.side === "BUY"
                ? "bg-green-600 hover:bg-green-700 focus:ring-green-500"
                : "bg-red-600 hover:bg-red-700 focus:ring-red-500"
              : "bg-blue-600 hover:bg-blue-700 focus:ring-blue-500"
          }`}
        >
          {isLoading
            ? "Processing..."
            : orderType === "market"
            ? `${formData.side} Market Order`
            : "Place Limit Order"}
        </button>
      </form>

      {/* Orders List */}
      <div className="bg-slate-50 dark:bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-200 dark:border-slate-700">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-semibold text-slate-800 dark:text-slate-100">
            Recent Orders
          </h3>
          <button
            onClick={loadOrders}
            className="px-4 py-2 bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-200 rounded-md hover:bg-slate-300 dark:hover:bg-slate-600"
          >
            Refresh
          </button>
        </div>
        {orders.length === 0 ? (
          <p className="text-slate-500 dark:text-slate-400">No orders found</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
              <thead className="bg-slate-100 dark:bg-slate-700">
                <tr>
                  <th className="px-4 py-2 text-left text-xs font-medium text-slate-700 dark:text-slate-300 uppercase">
                    Order ID
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-slate-700 dark:text-slate-300 uppercase">
                    Product
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-slate-700 dark:text-slate-300 uppercase">
                    Side
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-slate-700 dark:text-slate-300 uppercase">
                    Type
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-slate-700 dark:text-slate-300 uppercase">
                    Size
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-slate-700 dark:text-slate-300 uppercase">
                    Status
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-slate-700 dark:text-slate-300 uppercase">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-slate-800 divide-y divide-slate-200 dark:divide-slate-700">
                {orders.map((order) => (
                  <tr
                    key={order.orderId || order.order_id || order.id}
                    className="hover:bg-slate-50 dark:hover:bg-slate-700"
                  >
                    <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">
                      <span className="font-mono text-xs">
                        {order.orderId || order.order_id || order.id || "N/A"}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">
                      {order.productId ||
                        order.product_id ||
                        order.symbol ||
                        "N/A"}
                    </td>
                    <td className="px-4 py-2 text-sm">
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                          order.side === "BUY"
                            ? "bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200"
                            : "bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200"
                        }`}
                      >
                        {order.side || "N/A"}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">
                      {order.orderType || "N/A"}
                    </td>
                    <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">
                      {order.filledSize || order.size || "N/A"}
                      {order.filledSize &&
                        order.averageFilledPrice &&
                        parseFloat(order.averageFilledPrice) > 0 && (
                          <span className="text-xs text-slate-500 dark:text-slate-400 ml-1 block">
                            @ ${parseFloat(order.averageFilledPrice).toFixed(2)}
                          </span>
                        )}
                    </td>
                    <td className="px-4 py-2 text-sm">
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                          order.status === "FILLED" || order.status === "FILL"
                            ? "bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200"
                            : order.status === "OPEN" ||
                              order.status === "PENDING"
                            ? "bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200"
                            : order.status === "CANCELLED" ||
                              order.status === "CANCEL"
                            ? "bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                            : "bg-slate-100 dark:bg-slate-700 text-slate-800 dark:text-slate-200"
                        }`}
                      >
                        {order.status || "N/A"}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-sm">
                      {(order.status === "OPEN" ||
                        order.status === "PENDING" ||
                        order.status === "UNKNOWN") && (
                        <button
                          onClick={() =>
                            handleCancelOrder(
                              order.orderId || order.order_id || order.id
                            )
                          }
                          className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
                        >
                          Cancel
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default CoinbaseTradingForm;
