import React from 'react';
import {
  ArrowTrendingUpIcon,
  ArrowTrendingDownIcon,
  ClockIcon
} from '@heroicons/react/24/outline';

const ActiveTrades = ({ trades = [], status }) => {
  if (!trades || trades.length === 0) {
    return (
      <div className="bg-white shadow rounded-lg p-12 text-center">
        <p className="text-slate-500">No active trades yet</p>
      </div>
    );
  }

  return (
    <div className="bg-white shadow rounded-lg">
      <div className="px-6 py-4 border-b border-slate-200">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-medium text-slate-900">Active Trades</h3>
          {status && (
            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
              status === 'Running'
                ? 'bg-green-100 text-green-800'
                : 'bg-yellow-100 text-yellow-800'
            }`}>
              {status}
            </span>
          )}
        </div>
      </div>
      <div className="divide-y divide-slate-200">
        {trades.map((trade, index) => (
          <div key={index} className="px-6 py-4 hover:bg-slate-50">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-4">
                <div className="flex-shrink-0">
                  {trade.profit >= 0 ? (
                    <ArrowTrendingUpIcon className="h-5 w-5 text-green-500" />
                  ) : (
                    <ArrowTrendingDownIcon className="h-5 w-5 text-red-500" />
                  )}
                </div>
                <div>
                  <p className="text-sm font-medium text-slate-900">
                    {trade.symbol || 'N/A'}
                  </p>
                  <p className="text-xs text-slate-500">
                    {trade.type || 'Trade'} • {trade.time || 'N/A'}
                  </p>
                </div>
              </div>
              <div className="text-right">
                <p className={`text-sm font-semibold ${
                  trade.profit >= 0 ? 'text-green-600' : 'text-red-600'
                }`}>
                  {trade.profit >= 0 ? '+' : ''}${trade.profit?.toFixed(2) || '0.00'}
                </p>
                <p className="text-xs text-slate-500">
                  Price: ${trade.price?.toFixed(2) || 'N/A'}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default ActiveTrades;



