import React from 'react';
import DarkModeToggle from './DarkModeToggle';

const Header = () => {
  return (
    <header className="bg-white dark:bg-slate-800 shadow-sm border-b border-slate-200 dark:border-slate-700">
      <div className="px-6 py-4">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-semibold text-slate-800 dark:text-slate-100">
            Trading Dashboard
          </h2>
          <div className="flex items-center space-x-4">
            <div className="text-sm text-slate-600 dark:text-slate-300">
              <span className="font-medium">Status:</span>
              <span className="ml-2 text-green-600 dark:text-green-400">● Connected</span>
            </div>
            <DarkModeToggle />
          </div>
        </div>
      </div>
    </header>
  );
};

export default Header;


