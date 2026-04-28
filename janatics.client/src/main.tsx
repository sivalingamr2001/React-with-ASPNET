import React from "react";
import { createRoot } from "react-dom/client";
import App from "./App";
import "./index.css";
import { TooltipProvider } from "./components/ui/tooltip";
import { Toaster } from "./components/ui/sonner";
import AuthProvider from "./context/auth-provider";

createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <AuthProvider>
      <TooltipProvider>
        <Toaster position="top-center" richColors />
        <App />
      </TooltipProvider>
    </AuthProvider>
  </React.StrictMode>,
);
