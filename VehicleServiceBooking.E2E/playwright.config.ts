import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  retries: 1,
  workers: 1, // sequential — testet ndajnë të dhëna live
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],

  use: {
    baseURL: 'http://localhost:5174',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Ndiz serverët automatikisht para ekzekutimit të testeve
  webServer: [
    {
      // Backend — dotnet run duhet të ekzekutohet nga rrënja e projektit
      command: 'dotnet run --project ../VehicleServiceBooking/VehicleServiceBooking.csproj',
      url: 'http://localhost:5294/api/servicecenters',
      timeout: 60_000,
      reuseExistingServer: true,
    },
    {
      // Frontend — Vite dev server
      command: 'npm run dev --prefix ../VehicleServiceBooking.Client/vehicle-service-booking-client',
      url: 'http://localhost:5174',
      timeout: 30_000,
      reuseExistingServer: true,
    },
  ],
});
