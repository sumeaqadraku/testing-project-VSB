// PERSONA 3 — System Tests (Playwright E2E skeleton)

import { test, expect } from '@playwright/test';

test.describe('Payment Flow (E2E)', () => {
  test.todo('Client completes full payment → Booking becomes Completed');
  test.todo('Partial payment → Booking remains active, balance updated');
  test.todo('Second payment completes remaining balance → auto close');
});

test.describe('Negative Scenarios', () => {
  test.todo('Client accesses /manager → redirected (forbidden)');
  test.todo('Payment exceeds balance → specific error message');
  test.todo('Payment without invoice → "Invoice does not exist" error');
});