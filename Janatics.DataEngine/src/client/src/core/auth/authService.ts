import { axiosInstance } from "@/core/api/axiosInstance";
import type {
  LoginPayload,
  AuthResponse,
  RefreshResponse,
} from "@/shared/types/common.types";
import { tokenStorage } from "./tokenStorage";

export const authService = {
  login: async (payload: LoginPayload): Promise<AuthResponse> => {
    const { data } = await axiosInstance.post<AuthResponse>(
      "/auth/login",
      payload
    );
    tokenStorage.setRefreshToken(data.refreshToken);
    return data;
  },

  logout: async (): Promise<void> => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (refreshToken) {
      try {
        await axiosInstance.post("/auth/logout", { refreshToken });
      } catch {
        // Always clear local state even if server call fails
      }
    }
    tokenStorage.clearRefreshToken();
  },

  refreshToken: async (): Promise<string> => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (!refreshToken) throw new Error("No refresh token available");

    const { data } = await axiosInstance.post<RefreshResponse>(
      "/auth/refresh",
      { refreshToken }
    );

    tokenStorage.setRefreshToken(data.refreshToken);
    return data.accessToken;
  },

  getMe: async () => {
    const { data } = await axiosInstance.get("/auth/me");
    return data;
  },
};