import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  PlusIcon,
  CpuChipIcon,
  PlayIcon,
  PauseIcon,
  TrashIcon
} from '@heroicons/react/24/outline';
import StrategyCard from '../components/strategies/StrategyCard';
import StrategyModal from '../components/strategies/StrategyModal';

const Strategies = () => {
  const [strategies, setStrategies] = useState([
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
  ]);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedStrategy, setSelectedStrategy] = useState(null);

  const handleCreateStrategy = () => {
    setSelectedStrategy(null);
    setIsModalOpen(true);
  };

  const handleEditStrategy = (strategy) => {
    setSelectedStrategy(strategy);
    setIsModalOpen(true);
  };

  const handleSaveStrategy = (strategyData) => {
    if (selectedStrategy) {
      // Update existing
      setStrategies(strategies.map(s => 
        s.id === selectedStrategy.id ? { ...s, ...strategyData } : s
      ));
    } else {
      // Create new
      const newStrategy = {
        id: strategies.length + 1,
        ...strategyData,
        status: 'paused',
        profit: 0,
        trades: 0,
        winRate: 0,
        lastRun: 'Never'
      };
      setStrategies([...strategies, newStrategy]);
    }
    setIsModalOpen(false);
  };

  const handleToggleStatus = (id) => {
    setStrategies(strategies.map(s => 
      s.id === id ? { ...s, status: s.status === 'active' ? 'paused' : 'active' } : s
    ));
  };

  const handleDeleteStrategy = (id) => {
    if (window.confirm('Are you sure you want to delete this strategy?')) {
      setStrategies(strategies.filter(s => s.id !== id));
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Trading Strategies</h1>
          <p className="mt-1 text-sm text-slate-500">
            Manage and configure your trading algorithms
          </p>
        </div>
        <button
          onClick={handleCreateStrategy}
          className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          <PlusIcon className="-ml-1 mr-2 h-5 w-5" aria-hidden="true" />
          New Strategy
        </button>
      </div>

      {/* Strategies Grid */}
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {strategies.map((strategy) => (
          <StrategyCard
            key={strategy.id}
            strategy={strategy}
            onEdit={() => handleEditStrategy(strategy)}
            onToggleStatus={() => handleToggleStatus(strategy.id)}
            onDelete={() => handleDeleteStrategy(strategy.id)}
          />
        ))}
      </div>

      {/* Empty State */}
      {strategies.length === 0 && (
        <div className="text-center py-12">
          <CpuChipIcon className="mx-auto h-12 w-12 text-slate-400" />
          <h3 className="mt-2 text-sm font-medium text-slate-900">No strategies</h3>
          <p className="mt-1 text-sm text-slate-500">
            Get started by creating a new trading strategy.
          </p>
          <div className="mt-6">
            <button
              onClick={handleCreateStrategy}
              className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
            >
              <PlusIcon className="-ml-1 mr-2 h-5 w-5" aria-hidden="true" />
              New Strategy
            </button>
          </div>
        </div>
      )}

      {/* Strategy Modal */}
      {isModalOpen && (
        <StrategyModal
          strategy={selectedStrategy}
          onClose={() => setIsModalOpen(false)}
          onSave={handleSaveStrategy}
        />
      )}
    </div>
  );
};

export default Strategies;


