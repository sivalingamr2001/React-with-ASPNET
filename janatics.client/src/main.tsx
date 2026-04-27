import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { Toaster } from "sonner";

import { TooltipProvider } from "./components/ui/tooltip.tsx";
import { ThemeProvider } from "./context/theme-provider.tsx";
import "./index.css";
import AppRouter from "./Router.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter basename="/static">
      <ThemeProvider>
        <Toaster position="top-right" richColors />
        <TooltipProvider>
          <AppRouter />
        </TooltipProvider>
      </ThemeProvider>
    </BrowserRouter>
  </StrictMode>,
);
