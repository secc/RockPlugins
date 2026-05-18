import { defineConfig, devices } from '@playwright/test';

import * as dotenv from 'dotenv';

dotenv.config({ path: '.env' });

export default defineConfig({
  timeout: 0,
  globalTimeout: 0,
  expect: {
    // Bound per-assertion polling so a stuck server throws and lets the retry
    // logic in check-in.test.ts open a fresh kiosk session. Test-level and
    // global timeouts remain unbounded for long load runs.
    timeout: 30_000,
  },
  
  globalSetup: require.resolve('./global-setup.ts'),
  
  workers: '100%', // % of CPU cores or a number
  fullyParallel: true,
  
  reporter: 'html',
  use: {
    trace: 'retain-on-failure',
    video: 'retain-on-failure'
  },

  projects: [
    {
      name: 'chrome',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'safari',
      use: { ...devices['Desktop Safari'] },
    },
  ],
});
