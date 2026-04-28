import React from "react";
import { createRoot } from "react-dom/client";
import App from "./App";
import "./index.css";
import { TooltipProvider } from "./components/ui/tooltip";
import { Toaster } from "./components/ui/sonner";
import AuthProvider from "./context/auth-provider";
import { ThemeProvider } from "./context/theme-provider";

createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <ThemeProvider>
      <AuthProvider>
        <TooltipProvider delayDuration={0}>
          <Toaster position="top-center" richColors />
          <App />
        </TooltipProvider>
      </AuthProvider>
    </ThemeProvider>
  </React.StrictMode>,
);
