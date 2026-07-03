import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The dev server proxies /api to the local API so the app is same-origin in
// dev. The API 307-redirects http to its https endpoint (localhost:7072), so
// the proxy targets https directly; secure:false accepts the self-signed dev
// cert. Point VITE_API_TARGET elsewhere if the API runs on a different port.
export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env.PORT ?? 5173),
    proxy: {
      '/api': {
        target: process.env.VITE_API_TARGET ?? 'https://localhost:7072',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
