import React from "react";
import { Link } from "react-router-dom";
import {
  ChartBarIcon,
  CpuChipIcon,
  ArrowTrendingUpIcon,
  ClockIcon,
} from "@heroicons/react/24/outline";

const Dashboard = () => {
  const stats = [
    {
      name: "Active Strategies",
      value: "3",
      icon: CpuChipIcon,
      color: "bg-blue-500",
    },
    {
      name: "Running Backtests",
      value: "0",
      icon: ChartBarIcon,
      color: "bg-green-500",
    },
    {
      name: "Live Trades",
      value: "5",
      icon: ArrowTrendingUpIcon,
      color: "bg-purple-500",
    },
    {
      name: "Total Profit",
      value: "$1,234.56",
      icon: ClockIcon,
      color: "bg-yellow-500",
    },
  ];

  const recentStrategies = [
    {
      id: 1,
      name: "Grid Trading",
      status: "Active",
      profit: "+$234.56",
      lastRun: "2 hours ago",
    },
    {
      id: 2,
      name: "Scalping Strategy",
      status: "Active",
      profit: "+$189.23",
      lastRun: "5 hours ago",
    },
    {
      id: 3,
      name: "DCA Strategy",
      status: "Paused",
      profit: "+$45.12",
      lastRun: "1 day ago",
    },
  ];

  return (
    <div className="space-y-6">
      {/* Stats Grid */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <div
            key={stat.name}
            className="bg-slate-50 dark:bg-slate-800 overflow-hidden shadow-lg rounded-lg border border-slate-200 dark:border-slate-700"
          >
            <div className="p-5">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <stat.icon
                    className="h-6 w-6 text-slate-400 dark:text-slate-500"
                    aria-hidden="true"
                  />
                </div>
                <div className="ml-5 w-0 flex-1">
                  <dl>
                    <dt className="text-sm font-medium text-slate-500 dark:text-slate-400 truncate">
                      {stat.name}
                    </dt>
                    <dd className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                      {stat.value}
                    </dd>
                  </dl>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Quick Actions */}
      <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100 mb-4">
          Quick Actions
        </h3>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <Link
            to="/strategies"
            className="relative rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 px-6 py-5 shadow-md flex items-center space-x-3 hover:border-slate-400 dark:hover:border-slate-500 focus-within:ring-2 focus-within:ring-blue-500 focus-within:ring-offset-2 transition-colors"
          >
            <div className="flex-shrink-0">
              <CpuChipIcon className="h-6 w-6 text-slate-400 dark:text-slate-500" />
            </div>
            <div className="flex-1 min-w-0">
              <span className="absolute inset-0" aria-hidden="true" />
              <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                New Strategy
              </p>
              <p className="text-sm text-slate-500 dark:text-slate-400 truncate">
                Create a new trading algorithm
              </p>
            </div>
          </Link>

          <Link
            to="/backtests"
            className="relative rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 px-6 py-5 shadow-md flex items-center space-x-3 hover:border-slate-400 dark:hover:border-slate-500 focus-within:ring-2 focus-within:ring-blue-500 focus-within:ring-offset-2 transition-colors"
          >
            <div className="flex-shrink-0">
              <ChartBarIcon className="h-6 w-6 text-slate-400 dark:text-slate-500" />
            </div>
            <div className="flex-1 min-w-0">
              <span className="absolute inset-0" aria-hidden="true" />
              <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                Run Backtest
              </p>
              <p className="text-sm text-slate-500 dark:text-slate-400 truncate">
                Test strategy performance
              </p>
            </div>
          </Link>

          <Link
            to="/trading"
            className="relative rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 px-6 py-5 shadow-md flex items-center space-x-3 hover:border-slate-400 dark:hover:border-slate-500 focus-within:ring-2 focus-within:ring-blue-500 focus-within:ring-offset-2 transition-colors"
          >
            <div className="flex-shrink-0">
              <ArrowTrendingUpIcon className="h-6 w-6 text-slate-400 dark:text-slate-500" />
            </div>
            <div className="flex-1 min-w-0">
              <span className="absolute inset-0" aria-hidden="true" />
              <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                Live Trading
              </p>
              <p className="text-sm text-slate-500 dark:text-slate-400 truncate">
                Monitor active trades
              </p>
            </div>
          </Link>
        </div>
      </div>

      {/* Recent Strategies */}
      <div className="bg-slate-50 dark:bg-slate-800 shadow-lg rounded-lg border border-slate-200 dark:border-slate-700">
        <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700">
          <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
            Recent Strategies
          </h3>
        </div>
        <div className="divide-y divide-slate-200 dark:divide-slate-700">
          {recentStrategies.map((strategy) => (
            <div
              key={strategy.id}
              className="px-6 py-4 hover:bg-white dark:hover:bg-slate-700 transition-colors"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center">
                  <div className="flex-shrink-0">
                    <CpuChipIcon className="h-5 w-5 text-slate-400 dark:text-slate-500" />
                  </div>
                  <div className="ml-4">
                    <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                      {strategy.name}
                    </p>
                    <p className="text-sm text-slate-500 dark:text-slate-400">
                      Last run: {strategy.lastRun}
                    </p>
                  </div>
                </div>
                <div className="flex items-center space-x-4">
                  <span
                    className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                      strategy.status === "Active"
                        ? "bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200"
                        : "bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200"
                    }`}
                  >
                    {strategy.status}
                  </span>
                  <span className="text-sm font-medium text-green-600 dark:text-green-400">
                    {strategy.profit}
                  </span>
                  <Link
                    to={`/strategies/${strategy.id}`}
                    className="text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
                  >
                    View →
                  </Link>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
