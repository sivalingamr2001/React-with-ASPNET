// Refresh tokens are stored in localStorage (HttpOnly cookies preferred in production)
// Access tokens are stored in-memory via Zustand (never localStorage directly)

const REFRESH_TOKEN_KEY = "app.rt";

export const tokenStorage = {
  getRefreshToken: (): string | null => {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  setRefreshToken: (token: string): void => {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
  },

  clearRefreshToken: (): void => {
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  },
};