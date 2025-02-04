import { defineConfig, devices } from '@playwright/test';

import * as dotenv from 'dotenv';

dotenv.config({ path: '.env' });

export default defineConfig({
  globalSetup: require.resolve('./global-setup.ts'),
  testDir: './',
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
