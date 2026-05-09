import { useAuthStore } from '@/lib/stores/authStore';
import { RolePermissions } from '@/types/auth';
import type { PermissionKey } from '@/types/auth';

export function useAuth() {
  const { user, isAuthenticated, isLoading, login, logout, setUser } = useAuthStore();

  const hasPermission = (permission: PermissionKey): boolean => {
    if (!user) return false;
    const permissions = RolePermissions[user.role] || [];
    return permissions.includes(permission);
  };

  const hasAllPermissions = (permissions: PermissionKey[]): boolean => {
    return permissions.every((p) => hasPermission(p));
  };

  const hasAnyPermission = (permissions: PermissionKey[]): boolean => {
    return permissions.some((p) => hasPermission(p));
  };

  return {
    user,
    isAuthenticated,
    isLoading,
    login,
    logout,
    setUser,
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
  };
}
