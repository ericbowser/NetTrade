import React from "react";
import { createRoot } from "react-dom/client";
import dotenv from "dotenv";
import App from "./App";
import "./assets/styles/output.css";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import { DarkModeProvider } from "./contexts/DarkModeContext";

// Load environment variables
dotenv.config();
import Dashboard from "./pages/Dashboard";
import Strategies from "./pages/Strategies";
import StrategyDetail from "./pages/StrategyDetail";
import Backtests from "./pages/Backtests";
import Trading from "./pages/Trading";
import AlpacaTrading from "./pages/AlpacaTrading";
import Settings from "./pages/Settings";

const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      {
        index: true,
        element: <Dashboard />,
      },
      {
        path: "strategies",
        element: <Strategies />,
      },
      {
        path: "strategies/:id",
        element: <StrategyDetail />,
      },
      {
        path: "backtests",
        element: <Backtests />,
      },
      {
        path: "trading",
        element: <Trading />,
      },
      {
        path: "alpaca-trading",
        element: <AlpacaTrading />,
      },
      {
        path: "settings",
        element: <Settings />,
      },
    ],
  },
]);

const container = document.getElementById("root");
const root = createRoot(container);

root.render(
  <React.StrictMode>
    <DarkModeProvider>
      <RouterProvider router={router} />
    </DarkModeProvider>
  </React.StrictMode>
);
