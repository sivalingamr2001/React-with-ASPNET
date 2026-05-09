import { type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuthStore } from "@/core/store/authStore";
import type { UserRole } from "@/shared/types/common.types";

interface RoleGuardProps {
  children: ReactNode;
  allowedRoles: UserRole[];
  fallback?: ReactNode;
}

export const RoleGuard = ({
  children,
  allowedRoles,
  fallback,
}: RoleGuardProps) => {
  const userRole = useAuthStore((s) => s.user?.role);

  const hasAccess = userRole !== undefined && allowedRoles.includes(userRole);

  if (!hasAccess) {
    return fallback ? <>{fallback}</> : <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
};