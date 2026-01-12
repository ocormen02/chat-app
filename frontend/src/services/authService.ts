import { AuthResponse } from '../types';

export const authService = {
  saveAuthData(data: AuthResponse): void {
    localStorage.setItem('authToken', data.token);
    localStorage.setItem('userId', data.userId);
    localStorage.setItem('userEmail', data.email);
    if (data.displayName) {
      localStorage.setItem('userDisplayName', data.displayName);
    }
  },

  clearAuthData(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('userId');
    localStorage.removeItem('userEmail');
    localStorage.removeItem('userDisplayName');
  },

  getToken(): string | null {
    return localStorage.getItem('authToken');
  },

  getUserId(): string | null {
    return localStorage.getItem('userId');
  },

  getUserEmail(): string | null {
    return localStorage.getItem('userEmail');
  },

  getUserDisplayName(): string | null {
    return localStorage.getItem('userDisplayName');
  },

  isAuthenticated(): boolean {
    return !!this.getToken();
  },
};

export default authService;
