import {defineConfig} from 'vitest/config';
import {fileURLToPath} from 'node:url';

export default defineConfig({
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    include: ['tests/**/*.test.ts', 'tests/**/*.test.tsx']
  },
  resolve: {
    alias: {
      '@': new URL('./src', import.meta.url).pathname,
      'server-only': fileURLToPath(new URL('./node_modules/server-only/empty.js', import.meta.url))
    }
  },
  oxc: {
    jsx: {runtime: 'automatic'}
  }
});
