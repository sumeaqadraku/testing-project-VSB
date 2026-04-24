// PERSONA 1 — Login component unit tests
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../contexts/AuthContext';
import Login from '../pages/Login';

vi.mock('../services/authService', () => ({
  authService: {
    login: vi.fn(),
    registerClient: vi.fn(),
  },
}));

// Login kërkon useNavigate dhe useAuth — i ofrojmë nëpërmjet wrappers
function renderLogin() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <Login />
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('Login', () => {
  it('shfaq inputin email', () => {
    renderLogin();
    expect(screen.getByPlaceholderText(/email address/i)).toBeInTheDocument();
  });

  it('shfaq inputin password', () => {
    renderLogin();
    expect(screen.getByPlaceholderText(/password/i)).toBeInTheDocument();
  });

  it('shfaq butonin Sign in', () => {
    renderLogin();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('shfaq linkun Register as Client', () => {
    renderLogin();
    expect(screen.getByRole('link', { name: /register as client/i })).toBeInTheDocument();
  });

  it('mund të shkruash email dhe password', () => {
    renderLogin();
    const emailInput    = screen.getByPlaceholderText(/email address/i);
    const passwordInput = screen.getByPlaceholderText(/password/i);

    fireEvent.change(emailInput,    { target: { value: 'test@test.com' } });
    fireEvent.change(passwordInput, { target: { value: 'MyPass@123' } });

    expect(emailInput.value).toBe('test@test.com');
    expect(passwordInput.value).toBe('MyPass@123');
  });

  it('shfaq mesazh gabimi kur login dështon', async () => {
    const { authService } = await import('../services/authService');
    authService.login.mockRejectedValueOnce({
      response: { data: { message: 'Invalid email or password.' } },
    });

    renderLogin();
    fireEvent.change(screen.getByPlaceholderText(/email address/i), { target: { value: 'bad@bad.com' } });
    fireEvent.change(screen.getByPlaceholderText(/password/i),      { target: { value: 'Wrong@Pass' } });
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid email or password/i)).toBeInTheDocument();
    });
  });
});
