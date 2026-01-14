// src/components/TradeList.js
import React, { useState } from "react";

const TradeList = ({ trades }) => {
  const [currentPage, setCurrentPage] = useState(1);
  const tradesPerPage = 10;

  if (!trades || trades.length === 0) {
    return (
      <div className="text-slate-500 dark:text-slate-400 p-4">
        No trades to display
      </div>
    );
  }

  // Pagination
  const indexOfLastTrade = currentPage * tradesPerPage;
  const indexOfFirstTrade = indexOfLastTrade - tradesPerPage;
  const currentTrades = trades.slice(indexOfFirstTrade, indexOfLastTrade);
  const totalPages = Math.ceil(trades.length / tradesPerPage);

  // Change page
  const paginate = (pageNumber) => setCurrentPage(pageNumber);

  return (
    <div className="bg-transparent">
      <h3 className="text-lg font-medium text-slate-800 dark:text-slate-100 mb-4">
        Trade History
      </h3>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
          <thead className="bg-slate-100 dark:bg-slate-700">
            <tr>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-300 uppercase tracking-wider"
              >
                Entry Time
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-300 uppercase tracking-wider"
              >
                Direction
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-300 uppercase tracking-wider"
              >
                Entry Price
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-300 uppercase tracking-wider"
              >
                Exit Price
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-300 uppercase tracking-wider"
              >
                P&L
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-300 uppercase tracking-wider"
              >
                Result
              </th>
            </tr>
          </thead>
          <tbody className="bg-white dark:bg-slate-800 divide-y divide-slate-200 dark:divide-slate-700">
            {currentTrades.map((trade, index) => {
              // Handle both Trade and GridTrade formats
              const entryTime = trade.entryTime || trade.timestamp;
              const entryPrice = trade.entryPrice || trade.price;
              const exitPrice = trade.exitPrice || null;

              // Handle direction - can be string ("LONG"/"SHORT") or enum (0/1)
              let direction = trade.direction;
              if (typeof direction === "number") {
                // OrderSide enum: 0 = Buy, 1 = Sell
                direction = direction === 0 ? "BUY" : "SELL";
              } else if (typeof direction === "string") {
                // Convert "LONG"/"SHORT" to "BUY"/"SELL" for consistency
                direction = direction.toUpperCase();
                if (direction === "LONG") direction = "BUY";
                if (direction === "SHORT") direction = "SELL";
              }

              // Determine result based on PnL if result field not available (for GridTrade)
              let result = trade.result;
              if (!result && trade.pnL !== undefined && trade.pnL !== null) {
                result =
                  trade.pnL > 0 ? "WIN" : trade.pnL < 0 ? "LOSS" : "OPEN";
              }

              // Format entry time
              let formattedTime = "Invalid Date";
              try {
                if (entryTime) {
                  const date = new Date(entryTime);
                  if (!isNaN(date.getTime())) {
                    formattedTime = date.toLocaleString();
                  }
                }
              } catch (e) {
                console.error("Error formatting date:", entryTime, e);
              }

              return (
                <tr
                  key={index}
                  className={`${
                    index % 2 === 0
                      ? "bg-white dark:bg-slate-800"
                      : "bg-slate-50 dark:bg-slate-700/50"
                  } hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors`}
                >
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-800 dark:text-slate-100">
                    {formattedTime}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">
                    <span
                      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        direction === "BUY" || direction === "LONG"
                          ? "bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200"
                          : "bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200"
                      }`}
                    >
                      {direction || "N/A"}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-800 dark:text-slate-100">
                    {entryPrice ? `$${entryPrice.toFixed(2)}` : "N/A"}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-800 dark:text-slate-100">
                    {exitPrice ? `$${exitPrice.toFixed(2)}` : "Open"}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <span
                      className={
                        trade.pnL >= 0
                          ? "text-green-600 dark:text-green-400"
                          : "text-red-600 dark:text-red-400"
                      }
                    >
                      $
                      {trade.pnL !== undefined && trade.pnL !== null
                        ? trade.pnL.toFixed(2)
                        : "0.00"}
                      {trade.pnLPct ? ` (${trade.pnLPct.toFixed(2)}%)` : ""}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">
                    {result ? (
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                          result === "WIN"
                            ? "bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200"
                            : result === "LOSS"
                            ? "bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200"
                            : "bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                        }`}
                      >
                        {result}
                      </span>
                    ) : (
                      "OPEN"
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="px-4 py-3 flex items-center justify-between border-t border-slate-200 dark:border-slate-700 sm:px-6">
          <div className="flex-1 flex justify-between sm:hidden">
            <button
              onClick={() => paginate(currentPage - 1)}
              disabled={currentPage === 1}
              className={`relative inline-flex items-center px-4 py-2 border border-slate-300 dark:border-slate-600 text-sm font-medium rounded-md ${
                currentPage === 1
                  ? "bg-slate-100 dark:bg-slate-700 text-slate-400 dark:text-slate-500 cursor-not-allowed"
                  : "bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-600"
              }`}
            >
              Previous
            </button>
            <button
              onClick={() => paginate(currentPage + 1)}
              disabled={currentPage === totalPages}
              className={`relative inline-flex items-center px-4 py-2 border border-slate-300 dark:border-slate-600 text-sm font-medium rounded-md ${
                currentPage === totalPages
                  ? "bg-slate-100 dark:bg-slate-700 text-slate-400 dark:text-slate-500 cursor-not-allowed"
                  : "bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-600"
              }`}
            >
              Next
            </button>
          </div>
          <div className="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-slate-700 dark:text-slate-300">
                Showing{" "}
                <span className="font-medium">{indexOfFirstTrade + 1}</span> to{" "}
                <span className="font-medium">
                  {indexOfLastTrade > trades.length
                    ? trades.length
                    : indexOfLastTrade}
                </span>{" "}
                of <span className="font-medium">{trades.length}</span> trades
              </p>
            </div>
            <div>
              <nav
                className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px"
                aria-label="Pagination"
              >
                <button
                  onClick={() => paginate(currentPage - 1)}
                  disabled={currentPage === 1}
                  className={`relative inline-flex items-center px-2 py-2 rounded-l-md border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-sm font-medium ${
                    currentPage === 1
                      ? "text-slate-300 dark:text-slate-600 cursor-not-allowed"
                      : "text-slate-500 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600"
                  }`}
                >
                  <span className="sr-only">Previous</span>
                  <svg
                    className="h-5 w-5"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z"
                      clipRule="evenodd"
                    />
                  </svg>
                </button>

                {[...Array(totalPages).keys()].map((number) => (
                  <button
                    key={number + 1}
                    onClick={() => paginate(number + 1)}
                    className={`relative inline-flex items-center px-4 py-2 border text-sm font-medium ${
                      currentPage === number + 1
                        ? "z-10 bg-blue-50 dark:bg-blue-900 border-blue-500 dark:border-blue-400 text-blue-600 dark:text-blue-300"
                        : "bg-white dark:bg-slate-700 border-slate-300 dark:border-slate-600 text-slate-500 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600"
                    }`}
                  >
                    {number + 1}
                  </button>
                ))}

                <button
                  onClick={() => paginate(currentPage + 1)}
                  disabled={currentPage === totalPages}
                  className={`relative inline-flex items-center px-2 py-2 rounded-r-md border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-sm font-medium ${
                    currentPage === totalPages
                      ? "text-slate-300 dark:text-slate-600 cursor-not-allowed"
                      : "text-slate-500 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600"
                  }`}
                >
                  <span className="sr-only">Next</span>
                  <svg
                    className="h-5 w-5"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
                      clipRule="evenodd"
                    />
                  </svg>
                </button>
              </nav>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default TradeList;
