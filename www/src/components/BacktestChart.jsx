// src/components/BacktestChart.js
import React from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";

const BacktestChart = ({ data }) => {
  if (!data || !data.trades || data.trades.length === 0) {
    return <div className="text-slate-500 p-4">No backtest data available</div>;
  }

  // Prepare data for the chart - create equity curve
  const chartData = data.trades.map((trade, index) => {
    // Handle both Trade and GridTrade formats
    const timestamp = trade.entryTime || trade.timestamp;
    const price = trade.entryPrice || trade.price;

    return {
      date: new Date(timestamp).toLocaleString(),
      equity: trade.equity || data.initialCapital, // Use equity from trade
      price: price,
      pnl: trade.pnL || 0,
      direction: trade.direction,
    };
  });

  return (
    <div className="bg-white rounded-md shadow-sm p-4">
      <h3 className="text-lg font-medium text-slate-800 mb-4">
        Backtest Results
      </h3>

      <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mb-6 bg-slate-50 p-4 rounded-md">
        <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
          <div className="text-xs text-slate-500 uppercase">
            Initial Capital
          </div>
          <div className="text-lg font-semibold">
            ${data.initialCapital.toFixed(2)}
          </div>
        </div>
        <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
          <div className="text-xs text-slate-500 uppercase">Final Equity</div>
          <div className="text-lg font-semibold">
            ${data?.finalEquity?.toFixed(2)}
          </div>
        </div>
        <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
          <div className="text-xs text-slate-500 uppercase">Total Profit</div>
          <div
            className={`text-lg font-semibold ${
              data?.totalProfit >= 0 ? "text-green-600" : "text-red-600"
            }`}
          >
            ${data?.totalProfit?.toFixed(2)} (
            {data?.totalProfitPercentage
              ? data.totalProfitPercentage.toFixed(2)
              : "0.00"}
            %)
          </div>
        </div>
        <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
          <div className="text-xs text-slate-500 uppercase">Win Rate</div>
          <div className="text-lg font-semibold">
            {data?.winRate ? data?.winRate?.toFixed(2) : "0.00"}%
          </div>
        </div>
        <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
          <div className="text-xs text-slate-500 uppercase">
            {data?.configuration?.gridRange || data?.configuration?.GridRange
              ? "Grid Range"
              : data?.totalTrades !== undefined
              ? "Total Trades"
              : "N/A"}
          </div>
          <div className="text-lg font-semibold">
            {data?.configuration?.gridRange || data?.configuration?.GridRange
              ? `${(
                  data.configuration.gridRange || data.configuration.GridRange
                ).toFixed(2)}%`
              : data?.totalTrades !== undefined
              ? data.totalTrades
              : "N/A"}
          </div>
        </div>
      </div>

      {/* Additional Metrics Row for Bollinger Bands and other strategies */}
      {(data?.totalTrades !== undefined ||
        data?.profitFactor !== undefined ||
        data?.maxDrawdownPercent !== undefined) && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6 bg-slate-50 p-4 rounded-md">
          {data?.totalTrades !== undefined && (
            <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
              <div className="text-xs text-slate-500 uppercase">
                Total Trades
              </div>
              <div className="text-lg font-semibold">{data.totalTrades}</div>
            </div>
          )}
          {data?.profitFactor !== undefined && (
            <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
              <div className="text-xs text-slate-500 uppercase">
                Profit Factor
              </div>
              <div className="text-lg font-semibold">
                {data.profitFactor.toFixed(2)}
              </div>
            </div>
          )}
          {data?.maxDrawdownPercent !== undefined && (
            <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
              <div className="text-xs text-slate-500 uppercase">
                Max Drawdown
              </div>
              <div className="text-lg font-semibold text-red-600">
                {data.maxDrawdownPercent.toFixed(2)}%
              </div>
            </div>
          )}
          {data?.averageWin !== undefined &&
            data?.averageLoss !== undefined && (
              <div className="bg-white p-3 rounded shadow-sm border border-slate-100">
                <div className="text-xs text-slate-500 uppercase">
                  Avg Win/Loss
                </div>
                <div className="text-lg font-semibold">
                  ${data.averageWin.toFixed(2)} / ${data.averageLoss.toFixed(2)}
                </div>
              </div>
            )}
        </div>
      )}

      <div className="h-96">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart
            data={chartData}
            margin={{
              top: 5,
              right: 30,
              left: 20,
              bottom: 5,
            }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
            <XAxis
              dataKey="date"
              tick={{ fontSize: 12 }}
              tickFormatter={(value) => {
                // Shorten the date format to make it more readable
                const date = new Date(value);
                return date.toLocaleDateString();
              }}
            />
            <YAxis
              yAxisId="left"
              tick={{ fontSize: 12 }}
              domain={["auto", "auto"]}
            />
            <YAxis
              yAxisId="right"
              orientation="right"
              tick={{ fontSize: 12 }}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "rgba(255, 255, 255, 0.9)",
                border: "1px solid #e2e8f0",
                borderRadius: "4px",
                fontSize: "12px",
              }}
            />
            <Legend wrapperStyle={{ fontSize: "12px" }} />
            <Line
              yAxisId="left"
              type="monotone"
              dataKey="equity"
              stroke="#6366f1"
              strokeWidth={2}
              activeDot={{ r: 6 }}
              name="Equity"
              dot={false}
            />
            <Line
              yAxisId="right"
              type="monotone"
              dataKey="price"
              stroke="#22c55e"
              strokeWidth={2}
              name="Price"
              dot={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default BacktestChart;
