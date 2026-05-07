import type { AxiosInstance, AxiosError } from "axios";
import { AppError } from "@/core/error/AppError";

export const attachErrorInterceptor = (instance: AxiosInstance): void => {
  instance.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      const apiError = normalizeApiError(error);
      return Promise.reject(apiError);
    }
  );
};

const normalizeApiError = (error: AxiosError): AppError => {
  if (error.response) {
    const data = error.response.data as Record<string, unknown>;
    return new AppError({
      message: (data?.message as string) ?? "An error occurred",
      code: (data?.code as string) ?? "API_ERROR",
      statusCode: error.response.status,
      details: data?.errors,
    });
  }

  if (error.request) {
    return new AppError({
      message: "Network error — please check your connection",
      code: "NETWORK_ERROR",
      statusCode: 0,
    });
  }

  return new AppError({
    message: error.message ?? "Unexpected error",
    code: "UNKNOWN_ERROR",
    statusCode: -1,
  });
};