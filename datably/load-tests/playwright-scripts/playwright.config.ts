import { defineConfig, devices } from '@playwright/test';

import * as dotenv from 'dotenv';

dotenv.config({ path: '.env' });

export default defineConfig({
  globalTimeout: 7_200_000, // 2 hrs
  globalSetup: require.resolve('./global-setup.ts'),
  fullyParallel: true,
  timeout: 0,
  expect: {
    timeout: 0,
  },
  
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
    // {
    //   name: 'firefox',
    //   use: { ...devices['Desktop Firefox'] },
    // },
    // {
    //   name: 'safari',
    //   use: { ...devices['Desktop Safari'] },
    // },
  ],

});
