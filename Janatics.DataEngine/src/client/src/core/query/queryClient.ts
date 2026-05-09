import { QueryClient } from "@tanstack/react-query";
import { AppError } from "@/core/error/AppError";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,       // 5 minutes — server data is fresh for 5min
      gcTime: 1000 * 60 * 30,          // 30 minutes garbage collection
      retry: (failureCount, error) => {
        // Do not retry 4xx errors — these are client errors
        if (error instanceof AppError && error.statusCode < 500) return false;
        return failureCount < 2;
      },
      refetchOnWindowFocus: false,      // Disable for enterprise apps with heavy data
      throwOnError: false,
    },
    mutations: {
      retry: 0,
    },
  },
});