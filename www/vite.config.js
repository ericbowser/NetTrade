import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react';
import {PORT, NODE_ENV} from './env.json';
import fs from 'fs';
import path from 'path';

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
  }
})