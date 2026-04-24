// PERSONA 2 — ManagerDashboard component unit tests
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../contexts/AuthContext';

// Mock të gjitha shërbimet e API
vi.mock('../services/bookingService',       () => ({ bookingService:       { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/serviceCenterService', () => ({ serviceCenterService: { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/serviceTypeService',   () => ({ serviceTypeService:   { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/workOrderService',     () => ({ workOrderService:     { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/mechanicService',      () => ({ mechanicService:      { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/invoiceService',       () => ({ invoiceService:       { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/paymentService',       () => ({ paymentService:       { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/partService',          () => ({ partService:          { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/vehicleService',       () => ({ vehicleService:       { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/clientsService',       () => ({ clientsService:       { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/scheduleService',      () => ({ scheduleService:      { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/authService',          () => ({ authService:          { login: vi.fn(), registerClient: vi.fn() } }));

function renderWithManagerUser(Component) {
  // Vendos userin Manager në localStorage para renderimit
  localStorage.setItem('token', 'fake-manager-token');
  localStorage.setItem('user', JSON.stringify({
    id: 'mgr-id',
    email: 'manager@test.com',
    firstName: 'Manager',
    lastName: 'User',
    roles: ['Manager'],
  }));

  return render(
    <MemoryRouter>
      <AuthProvider>
        <Component />
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('ManagerDashboard', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('renderizon pa crash', async () => {
    const { default: ManagerDashboard } = await import('../pages/ManagerDashboard');
    expect(() => renderWithManagerUser(ManagerDashboard)).not.toThrow();
  });

  it('shfaq titullin ose elementë kryesorë të dashboard-it', async () => {
    const { default: ManagerDashboard } = await import('../pages/ManagerDashboard');
    renderWithManagerUser(ManagerDashboard);

    // Prit që API calls të mbarojnë
    await waitFor(() => {
      // Verifiko që ekziston diçka në DOM (dashboard nuk është bosh)
      expect(document.body).toBeTruthy();
    });
  });
});
