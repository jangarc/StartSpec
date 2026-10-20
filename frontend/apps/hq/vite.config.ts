import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [
    vue({
      template: {
        compilerOptions: {
          isCustomElement: (tag: string) => tag.startsWith('iam-'),
        },
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
