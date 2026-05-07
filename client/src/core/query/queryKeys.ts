// Centralized query key registry ensures consistent cache keys across the app
// and enables targeted cache invalidation without string-matching

export const queryKeys = {
  // Users domain
  users: {
    all: ["users"] as const,
    lists: () => [...queryKeys.users.all, "list"] as const,
    list: (filters: Record<string, unknown>) =>
      [...queryKeys.users.lists(), filters] as const,
    details: () => [...queryKeys.users.all, "detail"] as const,
    detail: (userId: string) =>
      [...queryKeys.users.details(), userId] as const,
  },

  // Dashboard domain
  dashboard: {
    all: ["dashboard"] as const,
    stats: () => [...queryKeys.dashboard.all, "stats"] as const,
    activity: () => [...queryKeys.dashboard.all, "activity"] as const,
  },

  // Auth domain
  auth: {
    me: ["auth", "me"] as const,
    permissions: (userId: string) =>
      ["auth", "permissions", userId] as const,
  },
} as const;