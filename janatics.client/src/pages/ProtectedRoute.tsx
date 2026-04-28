import React from "react";
import type { UserType } from "@/types";
import LoginPage from "./LoginPage";

interface ProtectedRouteProps {
  user: UserType | null;
  children: React.ReactNode;
}

export default function ProtectedRoute({
  user,
  children,
}: ProtectedRouteProps) {
  if (!user) {
    return <LoginPage />;
  }

  return <>{children}</>;
}
