import React from "react";
import { NavLink } from "react-router-dom";
import {
  ChartBarIcon,
  CpuChipIcon,
  Cog6ToothIcon,
  HomeIcon,
  ArrowTrendingUpIcon,
  BanknotesIcon,
} from "@heroicons/react/24/outline";

const navigation = [
  { name: "Dashboard", href: "/", icon: HomeIcon },
  { name: "Strategies", href: "/strategies", icon: CpuChipIcon },
  { name: "Backtests", href: "/backtests", icon: ChartBarIcon },
  { name: "Live Trading", href: "/trading", icon: ArrowTrendingUpIcon },
  { name: "Alpaca Paper", href: "/alpaca-trading", icon: BanknotesIcon },
  { name: "Settings", href: "/settings", icon: Cog6ToothIcon },
];

const Sidebar = () => {
  return (
    <div className="hidden md:flex md:flex-shrink-0">
      <div className="flex flex-col w-64">
        <div className="flex flex-col flex-grow pt-5 pb-4 overflow-y-auto bg-slate-800 dark:bg-slate-900">
          <div className="flex items-center flex-shrink-0 px-4">
            <h1 className="text-xl font-bold text-white">CoinAPI</h1>
          </div>
          <div className="mt-5 flex-grow flex flex-col">
            <nav className="flex-1 px-2 space-y-1">
              {navigation.map((item) => (
                <NavLink
                  key={item.name}
                  to={item.href}
                  className={({ isActive }) =>
                    `group flex items-center px-3 py-2 text-sm font-medium rounded-md transition-colors ${
                      isActive
                        ? "bg-slate-900 dark:bg-slate-700 text-white"
                        : "text-slate-300 hover:bg-slate-700 dark:hover:bg-slate-800 hover:text-white"
                    }`
                  }
                >
                  <item.icon
                    className="mr-3 flex-shrink-0 h-6 w-6"
                    aria-hidden="true"
                  />
                  {item.name}
                </NavLink>
              ))}
            </nav>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Sidebar;
