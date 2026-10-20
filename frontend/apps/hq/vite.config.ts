import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [
    tailwindcss(),
    vue({
      template: {
        compilerOptions: {},
      },
    }),
  ],
  server: {
    port: 5174,
    proxy: {
      // /iam flows go to the local Hosts.HQ dev instance (no CORS surface in HostBase yet).
      '/iam': 'http://localhost:8787',
    },
  },
});
