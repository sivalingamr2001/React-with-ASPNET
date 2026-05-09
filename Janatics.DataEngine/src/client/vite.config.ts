import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { visualizer } from "rollup-plugin-visualizer";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default defineConfig(({ mode }) => ({
  base: "/react-app",
  plugins: [
    react(),
    tsconfigPaths(),
    tailwindcss(),
    mode === "analyze" &&
      visualizer({
        open: true,
        gzipSize: true,
        brotliSize: true,
        filename: "dist/bundle-analysis.html",
      }),
  ].filter(Boolean),
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ["react", "react-dom"],
          router: ["react-router-dom"],
          query: ["@tanstack/react-query"],
          ui: ["@radix-ui/react-dialog", "@radix-ui/react-select"],
        },
      },
    },
  },
}));
