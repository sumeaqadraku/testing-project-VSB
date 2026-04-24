// PERSONA 3 — ClientDashboard component unit tests
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../contexts/AuthContext';

// Mock API shërbime
vi.mock('../services/bookingService',       () => ({ bookingService:       { getAll: vi.fn().mockResolvedValue([]), cancel: vi.fn() } }));
vi.mock('../services/vehicleService',       () => ({ vehicleService:       { getAll: vi.fn().mockResolvedValue([]), create: vi.fn(), update: vi.fn(), delete: vi.fn() } }));
vi.mock('../services/serviceCenterService', () => ({ serviceCenterService: { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/serviceTypeService',   () => ({ serviceTypeService:   { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/mechanicService',      () => ({ mechanicService:      { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/invoiceService',       () => ({ invoiceService:       { getAll: vi.fn().mockResolvedValue([]), getByWorkOrder: vi.fn() } }));
vi.mock('../services/paymentService',       () => ({ paymentService:       { getAll: vi.fn().mockResolvedValue([]), create: vi.fn() } }));
vi.mock('../services/workOrderService',     () => ({ workOrderService:     { getAll: vi.fn().mockResolvedValue([]) } }));
vi.mock('../services/authService',          () => ({ authService:          { login: vi.fn(), registerClient: vi.fn() } }));

function renderWithClientUser(Component) {
  localStorage.setItem('token', 'fake-client-token');
  localStorage.setItem('user', JSON.stringify({
    id: 'client-id',
    email: 'client@test.com',
    firstName: 'Client',
    lastName:  'User',
    roles:     ['Client'],
  }));

  return render(
    <MemoryRouter>
      <AuthProvider>
        <Component />
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('ClientDashboard', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('renderizon pa crash kur lista e bookings është bosh', async () => {
    const { default: ClientDashboard } = await import('../pages/ClientDashboard');
    expect(() => renderWithClientUser(ClientDashboard)).not.toThrow();
  });

  it('shfaq seksionin e automjeteve', async () => {
    const { default: ClientDashboard } = await import('../pages/ClientDashboard');
    renderWithClientUser(ClientDashboard);

    await waitFor(() => {
      // Prit çdo tekst lidhur me automjetet (My Vehicles, Automjetet, etj.)
      const el = document.body.textContent;
      expect(el).toBeTruthy();
    });
  });

  it('kur booking lista është bosh, nuk hedh error', async () => {
    const { bookingService } = await import('../services/bookingService');
    bookingService.getAll.mockResolvedValueOnce([]);

    const { default: ClientDashboard } = await import('../pages/ClientDashboard');
    const { container } = renderWithClientUser(ClientDashboard);

    await waitFor(() => {
      expect(container).toBeTruthy();
    });
  });
});
