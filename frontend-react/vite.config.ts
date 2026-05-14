import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    host: '0.0.0.0',
    port: 3000,
    // OnlyOffice DocServer (container) fetch plugin config qua host.docker.internal:3000.
    // Vite 5 default block unknown Host header → cần whitelist.
    allowedHosts: ['host.docker.internal', 'localhost', '127.0.0.1'],
    proxy: {
      // Proxy API → BE container/local. Tránh CORS preflight + simpler dev.
      '/api': {
        target: process.env.VITE_API_TARGET || 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
});
