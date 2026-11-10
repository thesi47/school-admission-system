import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The server must listen on 0.0.0.0 so the browser on your computer can reach
// it when it runs inside a Docker container.
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
  },
})
