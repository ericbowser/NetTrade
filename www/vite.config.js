import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react';
import dotenv from 'dotenv';
import fs from 'fs';
import path from 'path';

// Load environment variables
dotenv.config();

const PORT = process.env.PORT || 3000;
const NODE_ENV = process.env.NODE_ENV || 'development';

// Check if SSL certificate files exist
const sslCertPath = path.resolve(__dirname, 'ssl/server.crt');
const sslKeyPath = path.resolve(__dirname, 'ssl/server.key');
const hasSslCert = fs.existsSync(sslCertPath) && fs.existsSync(sslKeyPath);

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'dist',
    sourcemap: NODE_ENV === 'development'
  },
  server: {
    port: PORT,
    host: 'localhost',
    // Only use HTTPS if certificate files exist
    ...(hasSslCert ? {
      https: {
        cert: sslCertPath,
        key: sslKeyPath
      }
    } : {})
  },
  css: {
    postcss: {
      plugins: [
        require('tailwindcss'),
      ]
    }
  },
  // Expose environment variables to client code
  define: {
    'process.env.BACKEND_BASE_URL': JSON.stringify(process.env.BACKEND_BASE_URL),
    'process.env.ALPACA_PAPER_GRID_LIVE_REL': JSON.stringify(process.env.ALPACA_PAPER_GRID_LIVE_REL),
    'process.env.ALPACA_PAPER_SCALP_LIVE_REL': JSON.stringify(process.env.ALPACA_PAPER_SCALP_LIVE_REL),
    'process.env.COINBASE_GRID_LIVE_REL': JSON.stringify(process.env.COINBASE_GRID_LIVE_REL),
    'process.env.COINBASE_SCALP_LIVE_REL': JSON.stringify(process.env.COINBASE_SCALP_LIVE_REL),
  }
})