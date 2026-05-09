import type { ReactNode } from "react";
import { QueryProvider } from "./QueryProvider";
import { ThemeProvider } from "./ThemeProvider";
import { ToastProvider } from "./ToastProvider";

interface AppProvidersProps {
  children: ReactNode;
}

// Providers are composed in this file — order matters (inner-to-outer)
export const AppProviders = ({ children }: AppProvidersProps) => (
  <QueryProvider>
    <ThemeProvider defaultTheme="system" storageKey="app-theme">
      <ToastProvider>
        {children}
      </ToastProvider>
    </ThemeProvider>
  </QueryProvider>
);