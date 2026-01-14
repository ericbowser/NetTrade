import React from 'react';
import AlpacaTradingForm from '../components/AlpacaTradingForm';

const Trading = () => {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
          Trading
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Place market and limit orders on Coinbase
        </p>
      </div>

      <AlpacaTradingForm />
    </div>
  );
};

export default Trading;


