import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { AuthProvider, useAuth } from '../contexts/AuthContext';

// Stub authService — testet unit nuk dërgojnë kërkesa reale
vi.mock('../services/authService', () => ({
  authService: {
    login: vi.fn(),
    registerClient: vi.fn(),
  },
}));

function TestConsumer() {
  const { isAuthenticated, user, loading } = useAuth();
  if (loading) return <div>Loading...</div>;
  return (
    <div>
      <span data-testid="authenticated">{String(isAuthenticated)}</span>
      <span data-testid="user">{user ? user.email : 'null'}</span>
    </div>
  );
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('useAuth hedh error kur përdoret jashtë AuthProvider', () => {
    // Suppress React error boundary output gjatë testit
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => render(<TestConsumer />)).toThrow(
      'useAuth must be used within AuthProvider'
    );
    spy.mockRestore();
  });

  it('isAuthenticated është false kur nuk ka token në localStorage', async () => {
    await act(async () => {
      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );
    });
    expect(screen.getByTestId('authenticated').textContent).toBe('false');
    expect(screen.getByTestId('user').textContent).toBe('null');
  });

  it('lexon userin nga localStorage nëse token ekziston', async () => {
    const fakeUser = { email: 'client@test.com', role: 'Client' };
    localStorage.setItem('token', 'fake-jwt-token');
    localStorage.setItem('user', JSON.stringify(fakeUser));

    await act(async () => {
      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );
    });

    expect(screen.getByTestId('authenticated').textContent).toBe('true');
    expect(screen.getByTestId('user').textContent).toBe('client@test.com');
  });

  it('pastron localStorage nëse user JSON është i dëmtuar', async () => {
    localStorage.setItem('token', 'fake-jwt-token');
    localStorage.setItem('user', 'INVALID_JSON{{{');

    await act(async () => {
      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      );
    });

    expect(screen.getByTestId('authenticated').textContent).toBe('false');
    expect(localStorage.getItem('token')).toBeNull();
  });
});
