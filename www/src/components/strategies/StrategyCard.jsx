import React from 'react';
import {
  CpuChipIcon,
  PencilIcon,
  PlayIcon,
  PauseIcon,
  TrashIcon,
  ChartBarIcon
} from '@heroicons/react/24/outline';
import { Link } from 'react-router-dom';

const StrategyCard = ({ strategy, onEdit, onToggleStatus, onDelete }) => {
  const getStatusColor = (status) => {
    return status === 'active' 
      ? 'bg-green-100 text-green-800' 
      : 'bg-yellow-100 text-yellow-800';
  };

  const getTypeColor = (type) => {
    const colors = {
      'Grid': 'bg-blue-100 text-blue-800',
      'Scalping': 'bg-purple-100 text-purple-800',
      'DCA': 'bg-orange-100 text-orange-800',
    };
    return colors[type] || 'bg-slate-100 text-slate-800';
  };

  return (
    <div className="bg-white overflow-hidden shadow rounded-lg hover:shadow-md transition-shadow">
      <div className="p-5">
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center">
            <CpuChipIcon className="h-5 w-5 text-slate-400 mr-2" />
            <h3 className="text-lg font-medium text-slate-900">{strategy.name}</h3>
          </div>
          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(strategy.status)}`}>
            {strategy.status}
          </span>
        </div>

        <p className="text-sm text-slate-500 mb-4">{strategy.description}</p>

        <div className="flex items-center space-x-2 mb-4">
          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getTypeColor(strategy.type)}`}>
            {strategy.type}
          </span>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-3 gap-4 mb-4 pt-4 border-t border-slate-200">
          <div>
            <p className="text-xs text-slate-500">Profit</p>
            <p className={`text-sm font-semibold ${strategy.profit >= 0 ? 'text-green-600' : 'text-red-600'}`}>
              ${strategy.profit.toFixed(2)}
            </p>
          </div>
          <div>
            <p className="text-xs text-slate-500">Trades</p>
            <p className="text-sm font-semibold text-slate-900">{strategy.trades}</p>
          </div>
          <div>
            <p className="text-xs text-slate-500">Win Rate</p>
            <p className="text-sm font-semibold text-slate-900">{strategy.winRate.toFixed(1)}%</p>
          </div>
        </div>

        {/* Actions */}
        <div className="flex items-center justify-between pt-4 border-t border-slate-200">
          <div className="flex items-center space-x-2">
            <button
              onClick={onToggleStatus}
              className={`p-2 rounded-md ${
                strategy.status === 'active'
                  ? 'text-yellow-600 hover:bg-yellow-50'
                  : 'text-green-600 hover:bg-green-50'
              }`}
              title={strategy.status === 'active' ? 'Pause' : 'Activate'}
            >
              {strategy.status === 'active' ? (
                <PauseIcon className="h-5 w-5" />
              ) : (
                <PlayIcon className="h-5 w-5" />
              )}
            </button>
            <button
              onClick={onEdit}
              className="p-2 text-slate-600 hover:bg-slate-100 rounded-md"
              title="Edit"
            >
              <PencilIcon className="h-5 w-5" />
            </button>
            <Link
              to={`/backtests?strategy=${strategy.id}`}
              className="p-2 text-slate-600 hover:bg-slate-100 rounded-md"
              title="Backtest"
            >
              <ChartBarIcon className="h-5 w-5" />
            </Link>
          </div>
          <button
            onClick={onDelete}
            className="p-2 text-red-600 hover:bg-red-50 rounded-md"
            title="Delete"
          >
            <TrashIcon className="h-5 w-5" />
          </button>
        </div>
      </div>
    </div>
  );
};

export default StrategyCard;


