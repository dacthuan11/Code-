import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const gatewayUrl = env.VITE_GATEWAY_URL || 'http://localhost:5000'

  return {
    plugins: [react()],
    resolve: {
      alias: {
        '@app':     `${import.meta.dirname}/src/app`,
        '@pages':   `${import.meta.dirname}/src/pages`,
        '@widgets': `${import.meta.dirname}/src/widgets`,
        '@shared':  `${import.meta.dirname}/src/shared`,
      },
    },
    server: {
      proxy: {
        '/api': {
          target: gatewayUrl,
          changeOrigin: true,
        },
      },
    },
  }
})
