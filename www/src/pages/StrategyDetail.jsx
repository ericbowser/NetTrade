import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import {
  ArrowLeftIcon,
  CpuChipIcon,
  ChartBarIcon,
  PencilIcon,
  TrashIcon,
  PlayIcon,
  PauseIcon
} from '@heroicons/react/24/outline';

const StrategyDetail = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [strategy, setStrategy] = useState(null);
  const [loading, setLoading] = useState(true);

  // Mock data - in production, this would fetch from an API
  const mockStrategies = [
    {
      id: 1,
      name: 'Grid Trading',
      type: 'Grid',
      status: 'active',
      description: 'Automated grid trading strategy',
      profit: 234.56,
      trades: 45,
      winRate: 78.5,
      lastRun: '2 hours ago'
    },
    {
      id: 2,
      name: 'Scalping Strategy',
      type: 'Scalping',
      status: 'active',
      description: 'High-frequency scalping algorithm',
      profit: 189.23,
      trades: 120,
      winRate: 65.2,
      lastRun: '5 hours ago'
    },
    {
      id: 3,
      name: 'DCA Strategy',
      type: 'DCA',
      status: 'paused',
      description: 'Dollar cost averaging approach',
      profit: 45.12,
      trades: 12,
      winRate: 58.3,
      lastRun: '1 day ago'
    },
  ];

  useEffect(() => {
    // Simulate API call
    const foundStrategy = mockStrategies.find(s => s.id === parseInt(id));
    setStrategy(foundStrategy);
    setLoading(false);
  }, [id]);

  const handleToggleStatus = () => {
    setStrategy({
      ...strategy,
      status: strategy.status === 'active' ? 'paused' : 'active'
    });
  };

  const handleDelete = () => {
    if (window.confirm('Are you sure you want to delete this strategy?')) {
      navigate('/strategies');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-slate-500">Loading...</div>
      </div>
    );
  }

  if (!strategy) {
    return (
      <div className="text-center py-12">
        <CpuChipIcon className="mx-auto h-12 w-12 text-slate-400" />
        <h3 className="mt-2 text-sm font-medium text-slate-900">Strategy not found</h3>
        <p className="mt-1 text-sm text-slate-500">
          The strategy you're looking for doesn't exist.
        </p>
        <div className="mt-6">
          <Link
            to="/strategies"
            className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
          >
            <ArrowLeftIcon className="-ml-1 mr-2 h-5 w-5" aria-hidden="true" />
            Back to Strategies
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Link
            to="/strategies"
            className="text-slate-600 hover:text-slate-900"
          >
            <ArrowLeftIcon className="h-6 w-6" />
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-slate-900">{strategy.name}</h1>
            <p className="mt-1 text-sm text-slate-500">
              Strategy Details
            </p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={handleToggleStatus}
            className={`inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md ${
              strategy.status === 'active'
                ? 'text-yellow-700 bg-yellow-100 hover:bg-yellow-200'
                : 'text-green-700 bg-green-100 hover:bg-green-200'
            }`}
          >
            {strategy.status === 'active' ? (
              <>
                <PauseIcon className="-ml-1 mr-2 h-5 w-5" />
                Pause
              </>
            ) : (
              <>
                <PlayIcon className="-ml-1 mr-2 h-5 w-5" />
                Activate
              </>
            )}
          </button>
          <button
            className="inline-flex items-center px-4 py-2 border border-slate-300 shadow-sm text-sm font-medium rounded-md text-slate-700 bg-white hover:bg-slate-50"
          >
            <PencilIcon className="-ml-1 mr-2 h-5 w-5" />
            Edit
          </button>
          <button
            onClick={handleDelete}
            className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-red-700 bg-red-100 hover:bg-red-200"
          >
            <TrashIcon className="-ml-1 mr-2 h-5 w-5" />
            Delete
          </button>
        </div>
      </div>

      {/* Status Badge */}
      <div className="flex items-center space-x-4">
        <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${
          strategy.status === 'active'
            ? 'bg-green-100 text-green-800'
            : 'bg-yellow-100 text-yellow-800'
        }`}>
          {strategy.status === 'active' ? 'Active' : 'Paused'}
        </span>
        <span className="text-sm text-slate-500">
          Type: {strategy.type}
        </span>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <ChartBarIcon className="h-6 w-6 text-slate-400" />
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-slate-500 truncate">
                    Total Profit
                  </dt>
                  <dd className="text-lg font-semibold text-green-600">
                    ${strategy.profit.toFixed(2)}
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <CpuChipIcon className="h-6 w-6 text-slate-400" />
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-slate-500 truncate">
                    Total Trades
                  </dt>
                  <dd className="text-lg font-semibold text-slate-900">
                    {strategy.trades}
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <ChartBarIcon className="h-6 w-6 text-slate-400" />
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-slate-500 truncate">
                    Win Rate
                  </dt>
                  <dd className="text-lg font-semibold text-slate-900">
                    {strategy.winRate}%
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <CpuChipIcon className="h-6 w-6 text-slate-400" />
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-slate-500 truncate">
                    Last Run
                  </dt>
                  <dd className="text-lg font-semibold text-slate-900">
                    {strategy.lastRun}
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Description */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-4 border-b border-slate-200">
          <h3 className="text-lg font-medium text-slate-900">Description</h3>
        </div>
        <div className="px-6 py-4">
          <p className="text-sm text-slate-600">{strategy.description}</p>
        </div>
      </div>

      {/* Actions */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-4 border-b border-slate-200">
          <h3 className="text-lg font-medium text-slate-900">Actions</h3>
        </div>
        <div className="px-6 py-4">
          <div className="flex space-x-4">
            <Link
              to={`/backtests?strategy=${strategy.id}`}
              className="inline-flex items-center px-4 py-2 border border-slate-300 shadow-sm text-sm font-medium rounded-md text-slate-700 bg-white hover:bg-slate-50"
            >
              <ChartBarIcon className="-ml-1 mr-2 h-5 w-5" />
              Run Backtest
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default StrategyDetail;

