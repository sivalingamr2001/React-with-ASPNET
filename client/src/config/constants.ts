import type { UserRole, Permission } from "@/shared/types/common.types";

export const PAGINATION_PAGE_SIZES = [10, 20, 50, 100] as const;
export const DEFAULT_PAGE_SIZE = 20;
export const DEFAULT_STALE_TIME_MS = 1000 * 60 * 5;
export const REQUEST_TIMEOUT_MS = 30_000;

export const ROLE_PERMISSIONS: Record<UserRole, Permission[]> = {
  admin: ["users:read", "users:write", "users:delete", "settings:write"],
  manager: ["users:read", "users:write"],
  viewer: ["users:read"],
};