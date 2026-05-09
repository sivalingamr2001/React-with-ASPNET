'use client';

import type { ReactNode } from 'react';
import { useAuth } from '@/lib/hooks/useAuth';
import type { PermissionKey, UserRole } from '@/types/auth';

interface RoleGuardProps {
  children: ReactNode;
  requiredPermissions?: PermissionKey[];
  requiredRoles?: UserRole[];
  fallback?: ReactNode;
  requireAll?: boolean;
}

export function RoleGuard({
  children,
  requiredPermissions,
  requiredRoles,
  fallback = null,
  requireAll = false,
}: RoleGuardProps) {
  const { user, hasAllPermissions, hasAnyPermission } = useAuth();

  if (!user) {
    return fallback;
  }

  // Check roles if specified
  if (requiredRoles && requiredRoles.length > 0) {
    const hasRole = requiredRoles.includes(user.role);
    if (!hasRole) {
      return fallback;
    }
  }

  // Check permissions if specified
  if (requiredPermissions && requiredPermissions.length > 0) {
    const hasPerms = requireAll
      ? hasAllPermissions(requiredPermissions)
      : hasAnyPermission(requiredPermissions);

    if (!hasPerms) {
      return fallback;
    }
  }

  return children;
}
