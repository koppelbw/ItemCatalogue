import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
// The ItemCatalogue API does not enable CORS, so the dev server proxies /api
// to it. Point VITE_API_TARGET elsewhere if the API runs on a different port.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: process.env.VITE_API_TARGET ?? 'http://localhost:5012',
        changeOrigin: true,
      },
    },
  },
})
