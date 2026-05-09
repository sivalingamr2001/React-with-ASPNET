import type { ReactNode } from "react";
import { Toaster } from "sonner";

interface ToastProviderProps {
  children: ReactNode;
}

export const ToastProvider = ({ children }: ToastProviderProps) => (
  <>
    {children}
    <Toaster richColors />
  </>
);
