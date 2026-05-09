import type { ReactNode } from "react";
import { useAuthStore } from "@/core/store/authStore";
import type { UserRole, Permission } from "@/shared/types/common.types";
import { ROLE_PERMISSIONS } from "@/config/constants";

interface CanProps {
  perform: Permission;
  children: ReactNode;
  fallback?: ReactNode;
}

export const Can = ({ perform, children, fallback = null }: CanProps) => {
  const userRole = useAuthStore((s) => s.user?.role);

  const hasPermission =
    userRole !== undefined &&
    ROLE_PERMISSIONS[userRole as UserRole]?.includes(perform);

  return hasPermission ? <>{children}</> : <>{fallback}</>;
};