import { create } from "zustand";
import { devtools, persist } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";
import type { AuthUser } from "@/shared/types/common.types";

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
}

interface AuthActions {
  setCredentials: (user: AuthUser, accessToken: string) => void;
  setAccessToken: (accessToken: string) => void;
  clearAuth: () => void;
  setInitializing: (value: boolean) => void;
}

type AuthStore = AuthState & AuthActions;

const initialState: AuthState = {
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isInitializing: true,
};

export const useAuthStore = create<AuthStore>()(
  devtools(
    persist(
      immer((set) => ({
        ...initialState,

        setCredentials: (user, accessToken) =>
          set((state) => {
            state.user = user;
            state.accessToken = accessToken;
            state.isAuthenticated = true;
          }),

        setAccessToken: (accessToken) =>
          set((state) => {
            state.accessToken = accessToken;
          }),

        clearAuth: () =>
          set((state) => {
            state.user = null;
            state.accessToken = null;
            state.isAuthenticated = false;
          }),

        setInitializing: (value) =>
          set((state) => {
            state.isInitializing = value;
          }),
      })),
      {
        name: "auth-storage",
        // Persist ONLY the access token — never store sensitive user PII in localStorage
        partialize: (state) => ({
          accessToken: state.accessToken,
        }),
      }
    ),
    { name: "AuthStore" }
  )
);

// Selector factories — prevents unnecessary re-renders
export const selectUser = (state: AuthStore) => state.user;
export const selectIsAuthenticated = (state: AuthStore) =>
  state.isAuthenticated;
export const selectUserRole = (state: AuthStore) => state.user?.role;