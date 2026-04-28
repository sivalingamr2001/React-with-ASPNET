import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { Toaster } from "sonner";

import AuthProvider from "./context/auth-provider.tsx";
import "./index.css";
import { ThemeProvider } from "./context/theme-provider.tsx";
import { TooltipProvider } from "./components/ui/tooltip.tsx";
import App from "./App.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter basename="/static">
      <AuthProvider>
        <ThemeProvider>
          <Toaster position="top-right" richColors />
          <TooltipProvider>
            <App />
          </TooltipProvider>
        </ThemeProvider>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
);
