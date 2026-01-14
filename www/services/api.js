import axios from "axios";
import {
  BACKEND_BASE_URL,
  GRID_STRATEGY_REL,
  SCALP_STRATEGY_REL,
  RSI_STRATEGY_REL,
  BOLLINGER_BANDS_STRATEGY_REL,
  MOVING_AVERAGE_CROSSOVER_STRATEGY_REL,
  ALPACA_PAPER_ACCOUNT_REL,
  ALPACA_PAPER_POSITIONS_REL,
  ALPACA_PAPER_ORDERS_REL,
  ALPACA_PAPER_BUY_REL,
  ALPACA_PAPER_SELL_REL,
  ALPACA_PAPER_LIMIT_REL,
} from "../env.json";

async function getBacktestResults(backtestRequest = {}, strategyUrl = null) {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };

    // Use the provided strategyUrl, or default to GRID_STRATEGY_REL if not provided
    const url = strategyUrl || `${BACKEND_BASE_URL}${GRID_STRATEGY_REL}`;

    console.log("Sending backtest request to:", url);
    const response = await axios.post(url, backtestRequest, { ...headers });
    return response.data;
  } catch (error) {
    console.error("Error calling backtest API:", error);
    throw error;
  }
}

// Alpaca Paper Trading API functions
async function getAlpacaAccount() {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_ACCOUNT_REL}`;
    const response = await axios.get(url, { headers });
    return response.data;
  } catch (error) {
    console.error("Error getting Alpaca account:", error);
    throw error;
  }
}

async function getAlpacaPositions() {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_POSITIONS_REL}`;
    const response = await axios.get(url, { headers });
    return response.data;
  } catch (error) {
    console.error("Error getting Alpaca positions:", error);
    throw error;
  }
}

async function getAlpacaOrders(status = null) {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_ORDERS_REL}${
      status ? `?status=${status}` : ""
    }`;
    const response = await axios.get(url, { headers });
    return response.data;
  } catch (error) {
    console.error("Error getting Alpaca orders:", error);
    throw error;
  }
}

async function placeAlpacaBuyOrder(orderRequest) {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_BUY_REL}`;
    const response = await axios.post(url, orderRequest, { headers });
    return response.data;
  } catch (error) {
    console.error("Error placing Alpaca buy order:", error);
    throw error;
  }
}

async function placeAlpacaSellOrder(orderRequest) {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_SELL_REL}`;
    const response = await axios.post(url, orderRequest, { headers });
    return response.data;
  } catch (error) {
    console.error("Error placing Alpaca sell order:", error);
    throw error;
  }
}

async function placeAlpacaLimitOrder(orderRequest) {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_LIMIT_REL}`;
    const response = await axios.post(url, orderRequest, { headers });
    return response.data;
  } catch (error) {
    console.error("Error placing Alpaca limit order:", error);
    throw error;
  }
}

async function cancelAlpacaOrder(orderId) {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_ORDERS_REL}/${orderId}`;
    const response = await axios.delete(url, { headers });
    return response.data;
  } catch (error) {
    console.error("Error cancelling Alpaca order:", error);
    throw error;
  }
}

async function cancelAllAlpacaOrders() {
  try {
    const headers = {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    };
    const url = `${BACKEND_BASE_URL}${ALPACA_PAPER_ORDERS_REL}`;
    const response = await axios.delete(url, { headers });
    return response.data;
  } catch (error) {
    console.error("Error cancelling all Alpaca orders:", error);
    throw error;
  }
}

export default getBacktestResults;
export {
  getAlpacaAccount,
  getAlpacaPositions,
  getAlpacaOrders,
  placeAlpacaBuyOrder,
  placeAlpacaSellOrder,
  placeAlpacaLimitOrder,
  cancelAlpacaOrder,
  cancelAllAlpacaOrders,
};
