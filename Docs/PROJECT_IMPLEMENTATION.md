# PROJECT_IMPLEMENTATION.md
# Enterprise React Application — Principal Architect Blueprint

---

## 1. PROJECT OVERVIEW

### Architectural Philosophy

This platform is designed around the principle of **bounded autonomy**: each feature team owns a complete vertical slice of the application — from UI components to API calls to local state — without needing to coordinate with other teams for routine changes. The architecture enforces isolation at the module boundary while providing shared infrastructure through a well-defined core layer.

The stack treats **TypeScript as the source of truth**. Every API contract, state shape, form schema, and route parameter is typed end-to-end. Zod schemas serve as the single definition point for validation rules and are inferred into TypeScript types — eliminating duplication between runtime validation and static types.

**React 19** is adopted as a production baseline. Concurrent features (Suspense, transitions, `useOptimistic`, `useActionState`) are integrated into the architecture from day one rather than retrofitted later.

### Scalability Strategy

- **Feature-first folder structure**: Each domain feature is self-contained under `src/features/<domain>/`. This maps directly to team ownership. A feature module is the atomic unit of development.
- **Lazy loading by default**: Every route and every large third-party dependency is code-split at the bundle boundary. The initial JS budget is ≤150KB gzipped.
- **API layer abstraction**: Components never call Axios directly. All network calls go through typed service modules consumed via TanStack Query hooks. This allows the network layer to be mocked, swapped, or versioned independently.
- **State stratification**: Server state lives in TanStack Query cache. Global UI state lives in Zustand. Local component state stays in `useState`/`useReducer`. This discipline eliminates the most common state duplication bugs.

### Enterprise Engineering Principles

1. **Explicit over implicit**: No magic. Every import path, state slice, and API binding is traceable.
2. **Fail loudly in development, gracefully in production**: Strict TypeScript, runtime Zod validation on API boundaries, and Error Boundaries at every layout level.
3. **Optimize for reading, not writing**: Code is read 10× more than it is written. Verbosity that aids comprehension is preferred over brevity that requires mental decoding.
4. **Zero-trust API boundaries**: Every API response is parsed through a Zod schema before entering application state. Unknown shapes throw in development and log + fallback in production.
5. **Accessibility as a first-class concern**: shadcn/ui's Radix primitives provide WAI-ARIA compliance. All interactive elements support keyboard navigation. Color contrast ratios meet WCAG 2.1 AA.

### Long-Term Maintainability Goals

- Any engineer unfamiliar with the codebase can find the implementation for any feature within 90 seconds by following the `src/features/<domain>/` convention.
- Upgrading a major dependency (e.g., React Router, TanStack Query) requires changes only to the relevant infrastructure layer, not feature code.
- The application compiles with zero TypeScript errors on `strict: true` at all times. CI blocks merges that introduce type errors.

### DX Optimization Strategy

- **Path aliases**: `@/` maps to `src/`. No `../../../../` import chains.
- **Husky + lint-staged**: Linting and formatting run only on staged files. Pre-commit is fast (<3s).
- **Vite HMR**: Sub-100ms hot module replacement during development.
- **Co-located tests**: Unit tests live alongside the code they test. No separate `__tests__` directories.
- **Generated API types**: Future-ready for OpenAPI code generation via `openapi-typescript`.

### Performance-First Architecture Goals

- **Core Web Vitals targets**: LCP < 2.5s, FID < 100ms, CLS < 0.1 on all primary routes.
- **Route-level code splitting**: Each route is a dynamic import. React Router's lazy loading prevents shipping unused feature code to users.
- **Query-level caching**: TanStack Query caches server state with configurable stale times. Cache is invalidated precisely on mutations, not broadly.
- **Bundle analysis**: `rollup-plugin-visualizer` is included in the build pipeline. Bundle regressions are caught in CI.

---

## 2. COMPLETE PROJECT INITIALIZATION

### Step 1 — Scaffold with Vite

```bash
pnpm create vite@latest my-enterprise-app -- --template react-ts
cd my-enterprise-app
```

> **Note**: `pnpm` is the required package manager for this project. It provides strict dependency isolation, content-addressable storage, and faster installs than npm or yarn for monorepo-ready setups.

### Step 2 — Install Core Dependencies

```bash
# Routing
pnpm add react-router-dom

# Server state
pnpm add @tanstack/react-query @tanstack/react-query-devtools

# HTTP client
pnpm add axios

# Global state
pnpm add zustand immer

# Forms and validation
pnpm add react-hook-form zod @hookform/resolvers

# UI primitives and styling
pnpm add tailwindcss @tailwindcss/vite
pnpm add class-variance-authority clsx tailwind-merge
pnpm add lucide-react

# shadcn/ui CLI (used to add individual components)
pnpm add -D @shadcn/ui

# Utilities
pnpm add date-fns
```

### Step 3 — Install Dev Dependencies

```bash
pnpm add -D \
  typescript \
  @types/node \
  eslint \
  @eslint/js \
  eslint-plugin-react \
  eslint-plugin-react-hooks \
  eslint-plugin-react-refresh \
  eslint-plugin-import \
  eslint-plugin-jsx-a11y \
  @typescript-eslint/eslint-plugin \
  @typescript-eslint/parser \
  prettier \
  prettier-plugin-tailwindcss \
  husky \
  lint-staged \
  rollup-plugin-visualizer \
  vite-tsconfig-paths
```

### Step 4 — Initialize Tailwind CSS

```bash
# tailwind.config.ts is configured manually — see Section 5
```

`tailwind.config.ts`:

```typescript
import type { Config } from "tailwindcss";

const config: Config = {
  darkMode: ["class"],
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    container: {
      center: true,
      padding: "2rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        popover: {
          DEFAULT: "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height)" },
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height)" },
          to: { height: "0" },
        },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
};

export default config;
```

### Step 5 — Initialize shadcn/ui

```bash
pnpm dlx shadcn@latest init
```

Select the following during initialization:
- Style: `Default`
- Base color: `Slate`
- CSS variables: `Yes`

Then add components as needed:

```bash
pnpm dlx shadcn@latest add button input label card dialog toast form select
```

### Step 6 — Configure Path Aliases

`vite.config.ts`:

```typescript
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tsconfigPaths from "vite-tsconfig-paths";
import { visualizer } from "rollup-plugin-visualizer";

export default defineConfig(({ mode }) => ({
  plugins: [
    react(),
    tsconfigPaths(),
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
```

`tsconfig.json`:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

### Step 7 — Initialize Husky

```bash
pnpm dlx husky init
echo "pnpm lint-staged" > .husky/pre-commit
```

### Step 8 — Configure Environment Files

```bash
# Development
touch .env.development .env.staging .env.production .env.local
```

`.env.development`:

```env
VITE_API_BASE_URL=http://localhost:3000/api/v1
VITE_APP_ENV=development
VITE_ENABLE_DEVTOOLS=true
VITE_APP_VERSION=$npm_package_version
```

`.env.production`:

```env
VITE_API_BASE_URL=https://api.myapp.com/api/v1
VITE_APP_ENV=production
VITE_ENABLE_DEVTOOLS=false
VITE_APP_VERSION=$npm_package_version
```

### Step 9 — package.json Scripts

```json
{
  "scripts": {
    "dev": "vite --mode development",
    "build": "tsc -b && vite build --mode production",
    "build:staging": "tsc -b && vite build --mode staging",
    "build:analyze": "tsc -b && vite build --mode analyze",
    "preview": "vite preview",
    "lint": "eslint . --ext ts,tsx --report-unused-disable-directives --max-warnings 0",
    "lint:fix": "eslint . --ext ts,tsx --fix",
    "format": "prettier --write \"src/**/*.{ts,tsx,json,css}\"",
    "format:check": "prettier --check \"src/**/*.{ts,tsx,json,css}\"",
    "type-check": "tsc --noEmit"
  },
  "lint-staged": {
    "src/**/*.{ts,tsx}": ["eslint --fix --max-warnings 0", "prettier --write"],
    "src/**/*.{json,css,md}": ["prettier --write"]
  }
}
```

---

## 3. ENTERPRISE FOLDER STRUCTURE

```
my-enterprise-app/
├── public/
│   ├── favicon.ico
│   └── robots.txt
│
├── src/
│   │
│   ├── app/                          # Application bootstrap layer
│   │   ├── App.tsx                   # Root component: providers composition
│   │   ├── index.css                 # Global CSS + CSS variables (theme tokens)
│   │   └── main.tsx                  # Entry point
│   │
│   ├── config/                       # Environment + feature flag configuration
│   │   ├── env.ts                    # Validated env vars via Zod
│   │   └── constants.ts              # App-wide constants (pagination, timeouts)
│   │
│   ├── core/                         # Infrastructure — shared by all features
│   │   ├── api/                      # Axios client and interceptors
│   │   │   ├── axiosInstance.ts      # Configured Axios instance
│   │   │   ├── interceptors/
│   │   │   │   ├── authInterceptor.ts
│   │   │   │   └── errorInterceptor.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── auth/                     # Auth engine (token management, session)
│   │   │   ├── authService.ts
│   │   │   ├── tokenStorage.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── error/                    # Error boundary components + error types
│   │   │   ├── GlobalErrorBoundary.tsx
│   │   │   ├── RouteErrorBoundary.tsx
│   │   │   └── AppError.ts
│   │   │
│   │   ├── query/                    # TanStack Query configuration
│   │   │   ├── queryClient.ts
│   │   │   └── queryKeys.ts          # Global query key registry
│   │   │
│   │   └── store/                    # Zustand root store
│   │       ├── authStore.ts
│   │       ├── uiStore.ts
│   │       └── index.ts
│   │
│   ├── features/                     # Feature modules — ONE per domain
│   │   │
│   │   ├── auth/                     # Authentication domain
│   │   │   ├── api/
│   │   │   │   └── authApi.ts        # Auth-specific API calls
│   │   │   ├── components/
│   │   │   │   ├── LoginForm.tsx
│   │   │   │   └── LogoutButton.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useLogin.ts
│   │   │   │   └── useLogout.ts
│   │   │   ├── pages/
│   │   │   │   ├── LoginPage.tsx
│   │   │   │   └── ForgotPasswordPage.tsx
│   │   │   ├── schemas/
│   │   │   │   └── loginSchema.ts    # Zod validation schemas
│   │   │   ├── types/
│   │   │   │   └── auth.types.ts
│   │   │   └── index.ts              # Public API of this feature module
│   │   │
│   │   ├── dashboard/                # Dashboard domain
│   │   │   ├── api/
│   │   │   ├── components/
│   │   │   ├── hooks/
│   │   │   ├── pages/
│   │   │   │   └── DashboardPage.tsx
│   │   │   ├── types/
│   │   │   └── index.ts
│   │   │
│   │   ├── users/                    # User management domain
│   │   │   ├── api/
│   │   │   │   └── usersApi.ts
│   │   │   ├── components/
│   │   │   │   ├── UserTable.tsx
│   │   │   │   ├── UserFilters.tsx
│   │   │   │   └── UserDrawer.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useUsers.ts
│   │   │   │   ├── useCreateUser.ts
│   │   │   │   └── useUpdateUser.ts
│   │   │   ├── pages/
│   │   │   │   ├── UsersListPage.tsx
│   │   │   │   └── UserDetailPage.tsx
│   │   │   ├── schemas/
│   │   │   │   └── userSchema.ts
│   │   │   ├── types/
│   │   │   │   └── user.types.ts
│   │   │   └── index.ts
│   │   │
│   │   └── settings/                 # Settings domain (extendable per team)
│   │       ├── components/
│   │       ├── pages/
│   │       └── index.ts
│   │
│   ├── layouts/                      # Layout components (shell, auth, blank)
│   │   ├── AppLayout/
│   │   │   ├── AppLayout.tsx         # Authenticated shell: sidebar + header
│   │   │   ├── Sidebar.tsx
│   │   │   ├── Header.tsx
│   │   │   └── index.ts
│   │   ├── AuthLayout/
│   │   │   ├── AuthLayout.tsx        # Centered card layout for login/register
│   │   │   └── index.ts
│   │   └── BlankLayout/
│   │       └── BlankLayout.tsx       # No chrome — used for error/404 pages
│   │
│   ├── providers/                    # React context providers
│   │   ├── QueryProvider.tsx         # TanStack Query + Devtools
│   │   ├── ThemeProvider.tsx         # Dark/light mode
│   │   ├── ToastProvider.tsx         # Sonner or shadcn Toaster
│   │   └── index.ts                  # Composed AppProviders
│   │
│   ├── routes/                       # React Router route definitions
│   │   ├── Router.tsx                # Root router with lazy routes
│   │   ├── ProtectedRoute.tsx        # Auth guard
│   │   ├── RoleGuard.tsx             # Role-based access guard
│   │   ├── publicRoutes.ts           # Public route definitions
│   │   ├── protectedRoutes.ts        # Authenticated route definitions
│   │   └── index.ts
│   │
│   ├── shared/                       # Cross-feature shared UI and utilities
│   │   ├── components/               # Reusable, domain-agnostic components
│   │   │   ├── DataTable/
│   │   │   │   ├── DataTable.tsx
│   │   │   │   ├── DataTablePagination.tsx
│   │   │   │   ├── DataTableToolbar.tsx
│   │   │   │   └── index.ts
│   │   │   ├── PageHeader/
│   │   │   │   ├── PageHeader.tsx
│   │   │   │   └── index.ts
│   │   │   ├── EmptyState/
│   │   │   │   └── EmptyState.tsx
│   │   │   ├── LoadingSpinner/
│   │   │   │   └── LoadingSpinner.tsx
│   │   │   └── ConfirmDialog/
│   │   │       └── ConfirmDialog.tsx
│   │   │
│   │   ├── hooks/                    # Shared, domain-agnostic hooks
│   │   │   ├── useDebounce.ts
│   │   │   ├── useLocalStorage.ts
│   │   │   ├── usePagination.ts
│   │   │   └── useMediaQuery.ts
│   │   │
│   │   └── types/                    # Shared TypeScript types
│   │       ├── api.types.ts          # Generic API response shapes
│   │       ├── pagination.types.ts
│   │       └── common.types.ts
│   │
│   ├── lib/                          # Third-party library wrappers + utilities
│   │   ├── axios.ts                  # Re-export of configured instance
│   │   ├── cn.ts                     # clsx + tailwind-merge utility
│   │   └── zod.ts                    # Shared Zod utilities / custom validators
│   │
│   └── styles/
│       └── globals.css               # CSS custom properties (design tokens)
│
├── .env.development
├── .env.staging
├── .env.production
├── .env.local                        # Gitignored — local overrides
├── .eslintrc.cjs
├── .prettierrc
├── .prettierignore
├── .gitignore
├── index.html
├── package.json
├── pnpm-lock.yaml
├── tailwind.config.ts
├── tsconfig.json
├── tsconfig.node.json
└── vite.config.ts
```

### Folder Responsibility Matrix

| Folder | Owns | Does NOT own |
|---|---|---|
| `src/features/<domain>/` | All UI, logic, and API calls for one domain | Cross-domain state, shared components |
| `src/core/` | Infrastructure: HTTP, auth, query config, global store | Business logic |
| `src/shared/` | Reusable UI components and hooks with no domain knowledge | Any domain-specific rendering |
| `src/layouts/` | Shell structure: sidebars, headers, layout grids | Page content |
| `src/routes/` | Route tree, guards, lazy loading | Component implementations |
| `src/providers/` | React context composition | Application logic |
| `src/config/` | Environment variable access and constants | Dynamic state |
| `src/lib/` | Third-party library configuration and wrappers | Business rules |

---

## 4. ENTERPRISE ROUTING ARCHITECTURE

### Router Setup

`src/routes/Router.tsx`:

```typescript
import { lazy, Suspense } from "react";
import {
  createBrowserRouter,
  RouterProvider,
  Navigate,
} from "react-router-dom";
import { AppLayout } from "@/layouts/AppLayout";
import { AuthLayout } from "@/layouts/AuthLayout";
import { BlankLayout } from "@/layouts/BlankLayout";
import { ProtectedRoute } from "./ProtectedRoute";
import { RoleGuard } from "./RoleGuard";
import { RouteErrorBoundary } from "@/core/error/RouteErrorBoundary";
import { PageLoader } from "@/shared/components/LoadingSpinner";

// Lazy-loaded feature pages
const LoginPage = lazy(() =>
  import("@/features/auth/pages/LoginPage").then((m) => ({
    default: m.LoginPage,
  }))
);
const ForgotPasswordPage = lazy(() =>
  import("@/features/auth/pages/ForgotPasswordPage").then((m) => ({
    default: m.ForgotPasswordPage,
  }))
);
const DashboardPage = lazy(() =>
  import("@/features/dashboard/pages/DashboardPage").then((m) => ({
    default: m.DashboardPage,
  }))
);
const UsersListPage = lazy(() =>
  import("@/features/users/pages/UsersListPage").then((m) => ({
    default: m.UsersListPage,
  }))
);
const UserDetailPage = lazy(() =>
  import("@/features/users/pages/UserDetailPage").then((m) => ({
    default: m.UserDetailPage,
  }))
);
const NotFoundPage = lazy(() =>
  import("@/features/errors/pages/NotFoundPage").then((m) => ({
    default: m.NotFoundPage,
  }))
);

const withSuspense = (Component: React.ComponentType) => (
  <Suspense fallback={<PageLoader />}>
    <Component />
  </Suspense>
);

const router = createBrowserRouter([
  // Public routes — redirect authenticated users away
  {
    element: <AuthLayout />,
    errorElement: <RouteErrorBoundary />,
    children: [
      { path: "/login", element: withSuspense(LoginPage) },
      { path: "/forgot-password", element: withSuspense(ForgotPasswordPage) },
    ],
  },
  // Protected routes — require authentication
  {
    element: (
      <ProtectedRoute>
        <AppLayout />
      </ProtectedRoute>
    ),
    errorElement: <RouteErrorBoundary />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: "/dashboard", element: withSuspense(DashboardPage) },
      // Admin-only routes
      {
        path: "/users",
        element: (
          <RoleGuard allowedRoles={["admin", "manager"]}>
            <Suspense fallback={<PageLoader />}>
              <UsersListPage />
            </Suspense>
          </RoleGuard>
        ),
      },
      {
        path: "/users/:userId",
        element: (
          <RoleGuard allowedRoles={["admin", "manager"]}>
            <Suspense fallback={<PageLoader />}>
              <UserDetailPage />
            </Suspense>
          </RoleGuard>
        ),
      },
    ],
  },
  // Catch-all
  {
    element: <BlankLayout />,
    children: [{ path: "*", element: withSuspense(NotFoundPage) }],
  },
]);

export const Router = () => <RouterProvider router={router} />;
```

### Protected Route

`src/routes/ProtectedRoute.tsx`:

```typescript
import { type ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuthStore } from "@/core/store/authStore";

interface ProtectedRouteProps {
  children: ReactNode;
}

export const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
};
```

### Role Guard

`src/routes/RoleGuard.tsx`:

```typescript
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
```

### Route Error Boundary

`src/core/error/RouteErrorBoundary.tsx`:

```typescript
import { useRouteError, isRouteErrorResponse, Link } from "react-router-dom";
import { Button } from "@/shared/components/ui/button";

export const RouteErrorBoundary = () => {
  const error = useRouteError();

  if (isRouteErrorResponse(error)) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <h1 className="text-4xl font-bold">{error.status}</h1>
        <p className="text-muted-foreground">{error.statusText}</p>
        <Button asChild>
          <Link to="/dashboard">Return to Dashboard</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4">
      <h1 className="text-4xl font-bold">Unexpected Error</h1>
      <p className="text-muted-foreground">
        Something went wrong. Please try again.
      </p>
    </div>
  );
};
```

---

## 5. SHADCN/UI ENTERPRISE DESIGN SYSTEM

### CSS Variables — Design Tokens

`src/styles/globals.css`:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;

    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;

    --popover: 0 0% 100%;
    --popover-foreground: 222.2 84% 4.9%;

    --primary: 221.2 83.2% 53.3%;
    --primary-foreground: 210 40% 98%;

    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;

    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;

    --accent: 210 40% 96.1%;
    --accent-foreground: 222.2 47.4% 11.2%;

    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;

    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 221.2 83.2% 53.3%;
    --radius: 0.5rem;
  }

  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;

    --card: 222.2 84% 4.9%;
    --card-foreground: 210 40% 98%;

    --primary: 217.2 91.2% 59.8%;
    --primary-foreground: 222.2 47.4% 11.2%;

    --secondary: 217.2 32.6% 17.5%;
    --secondary-foreground: 210 40% 98%;

    --muted: 217.2 32.6% 17.5%;
    --muted-foreground: 215 20.2% 65.1%;

    --accent: 217.2 32.6% 17.5%;
    --accent-foreground: 210 40% 98%;

    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 210 40% 98%;

    --border: 217.2 32.6% 17.5%;
    --input: 217.2 32.6% 17.5%;
    --ring: 224.3 76.3% 48%;
  }
}

@layer base {
  * {
    @apply border-border;
  }
  body {
    @apply bg-background text-foreground;
    font-feature-settings: "rlig" 1, "calt" 1;
  }
}
```

### `cn` Utility

`src/lib/cn.ts`:

```typescript
import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export const cn = (...inputs: ClassValue[]) => twMerge(clsx(inputs));
```

### Component Variant Pattern (CVA)

`src/shared/components/ui/badge.tsx`:

```typescript
import { type VariantProps, cva } from "class-variance-authority";
import { cn } from "@/lib/cn";

const badgeVariants = cva(
  "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
  {
    variants: {
      variant: {
        default:
          "border-transparent bg-primary text-primary-foreground hover:bg-primary/80",
        secondary:
          "border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80",
        destructive:
          "border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/80",
        outline: "text-foreground",
        success:
          "border-transparent bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-100",
        warning:
          "border-transparent bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-100",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

export const Badge = ({ className, variant, ...props }: BadgeProps) => (
  <div className={cn(badgeVariants({ variant }), className)} {...props} />
);
```

### Page Header Component

`src/shared/components/PageHeader/PageHeader.tsx`:

```typescript
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: ReactNode;
  className?: string;
}

export const PageHeader = ({
  title,
  description,
  actions,
  className,
}: PageHeaderProps) => (
  <div className={cn("flex items-start justify-between gap-4 pb-6", className)}>
    <div className="space-y-1">
      <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
      {description && (
        <p className="text-sm text-muted-foreground">{description}</p>
      )}
    </div>
    {actions && <div className="flex items-center gap-2">{actions}</div>}
  </div>
);
```

### Theme Provider

`src/providers/ThemeProvider.tsx`:

```typescript
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";

type Theme = "light" | "dark" | "system";

interface ThemeContextValue {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  resolvedTheme: "light" | "dark";
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

interface ThemeProviderProps {
  children: ReactNode;
  defaultTheme?: Theme;
  storageKey?: string;
}

export const ThemeProvider = ({
  children,
  defaultTheme = "system",
  storageKey = "app-theme",
}: ThemeProviderProps) => {
  const [theme, setThemeState] = useState<Theme>(
    () => (localStorage.getItem(storageKey) as Theme) ?? defaultTheme
  );

  const resolvedTheme =
    theme === "system"
      ? window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light"
      : theme;

  useEffect(() => {
    const root = window.document.documentElement;
    root.classList.remove("light", "dark");
    root.classList.add(resolvedTheme);
  }, [resolvedTheme]);

  const setTheme = useCallback(
    (newTheme: Theme) => {
      localStorage.setItem(storageKey, newTheme);
      setThemeState(newTheme);
    },
    [storageKey]
  );

  return (
    <ThemeContext.Provider value={{ theme, setTheme, resolvedTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = (): ThemeContextValue => {
  const context = useContext(ThemeContext);
  if (!context) throw new Error("useTheme must be used within ThemeProvider");
  return context;
};
```

---

## 6. STATE MANAGEMENT ARCHITECTURE

### Recommendation: Zustand over Redux Toolkit

For enterprise React applications with a feature-first architecture, **Zustand** is the superior choice. RTK introduces significant boilerplate and requires global action namespacing that conflicts with feature isolation. Zustand's atomic store model maps directly to bounded contexts: each domain can own a store slice without registering it in a global combiner.

RTK is appropriate if: the team has deep Redux expertise, a legacy codebase already uses Redux, or time-travel debugging via Redux DevTools is a hard requirement.

For greenfield enterprise applications: **Zustand with Immer middleware and persist middleware**.

### Auth Store

`src/core/store/authStore.ts`:

```typescript
import { create } from "zustand";
import { devtools, persist } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";
import type { AuthUser } from "@/shared/types/common.types";

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
}

interface AuthActions {
  setCredentials: (user: AuthUser, accessToken: string) => void;
  setAccessToken: (accessToken: string) => void;
  clearAuth: () => void;
  setInitializing: (value: boolean) => void;
}

type AuthStore = AuthState & AuthActions;

const initialState: AuthState = {
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isInitializing: true,
};

export const useAuthStore = create<AuthStore>()(
  devtools(
    persist(
      immer((set) => ({
        ...initialState,

        setCredentials: (user, accessToken) =>
          set((state) => {
            state.user = user;
            state.accessToken = accessToken;
            state.isAuthenticated = true;
          }),

        setAccessToken: (accessToken) =>
          set((state) => {
            state.accessToken = accessToken;
          }),

        clearAuth: () =>
          set((state) => {
            state.user = null;
            state.accessToken = null;
            state.isAuthenticated = false;
          }),

        setInitializing: (value) =>
          set((state) => {
            state.isInitializing = value;
          }),
      })),
      {
        name: "auth-storage",
        // Persist ONLY the access token — never store sensitive user PII in localStorage
        partialize: (state) => ({
          accessToken: state.accessToken,
        }),
      }
    ),
    { name: "AuthStore" }
  )
);

// Selector factories — prevents unnecessary re-renders
export const selectUser = (state: AuthStore) => state.user;
export const selectIsAuthenticated = (state: AuthStore) =>
  state.isAuthenticated;
export const selectUserRole = (state: AuthStore) => state.user?.role;
```

### UI Store

`src/core/store/uiStore.ts`:

```typescript
import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";

interface UIState {
  sidebarOpen: boolean;
  globalLoading: boolean;
  activeModal: string | null;
}

interface UIActions {
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  setGlobalLoading: (loading: boolean) => void;
  openModal: (modalId: string) => void;
  closeModal: () => void;
}

type UIStore = UIState & UIActions;

export const useUIStore = create<UIStore>()(
  devtools(
    immer((set) => ({
      sidebarOpen: true,
      globalLoading: false,
      activeModal: null,

      toggleSidebar: () =>
        set((state) => {
          state.sidebarOpen = !state.sidebarOpen;
        }),

      setSidebarOpen: (open) =>
        set((state) => {
          state.sidebarOpen = open;
        }),

      setGlobalLoading: (loading) =>
        set((state) => {
          state.globalLoading = loading;
        }),

      openModal: (modalId) =>
        set((state) => {
          state.activeModal = modalId;
        }),

      closeModal: () =>
        set((state) => {
          state.activeModal = null;
        }),
    })),
    { name: "UIStore" }
  )
);
```

### Feature-Scoped Store Pattern

For domain-specific state that doesn't belong in auth or UI:

```typescript
// src/features/users/store/usersFilterStore.ts
import { create } from "zustand";
import { immer } from "zustand/middleware/immer";

interface UsersFilterState {
  search: string;
  role: string | null;
  status: "active" | "inactive" | "all";
  page: number;
  pageSize: number;
}

interface UsersFilterActions {
  setSearch: (search: string) => void;
  setRole: (role: string | null) => void;
  setStatus: (status: UsersFilterState["status"]) => void;
  setPage: (page: number) => void;
  resetFilters: () => void;
}

const initialFilters: UsersFilterState = {
  search: "",
  role: null,
  status: "all",
  page: 1,
  pageSize: 20,
};

export const useUsersFilterStore = create<
  UsersFilterState & UsersFilterActions
>()(
  immer((set) => ({
    ...initialFilters,

    setSearch: (search) =>
      set((state) => {
        state.search = search;
        state.page = 1; // Reset page on search change
      }),

    setRole: (role) =>
      set((state) => {
        state.role = role;
        state.page = 1;
      }),

    setStatus: (status) =>
      set((state) => {
        state.status = status;
        state.page = 1;
      }),

    setPage: (page) =>
      set((state) => {
        state.page = page;
      }),

    resetFilters: () => set(() => ({ ...initialFilters })),
  }))
);
```

---

## 7. API LAYER ARCHITECTURE

### Axios Instance Configuration

`src/core/api/axiosInstance.ts`:

```typescript
import axios, { type AxiosInstance } from "axios";
import { env } from "@/config/env";

export const createAxiosInstance = (): AxiosInstance => {
  const instance = axios.create({
    baseURL: env.VITE_API_BASE_URL,
    timeout: 30_000,
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
  });

  return instance;
};

export const axiosInstance = createAxiosInstance();
```

### Auth Interceptor

`src/core/api/interceptors/authInterceptor.ts`:

```typescript
import type {
  AxiosInstance,
  InternalAxiosRequestConfig,
  AxiosResponse,
} from "axios";
import { useAuthStore } from "@/core/store/authStore";
import { authService } from "@/core/auth/authService";

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else if (token) {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

export const attachAuthInterceptor = (instance: AxiosInstance): void => {
  // Request — inject Bearer token
  instance.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      const accessToken = useAuthStore.getState().accessToken;
      if (accessToken) {
        config.headers.Authorization = `Bearer ${accessToken}`;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  // Response — handle 401 with token refresh + request replay
  instance.interceptors.response.use(
    (response: AxiosResponse) => response,
    async (error) => {
      const originalRequest = error.config;

      if (error.response?.status === 401 && !originalRequest._retry) {
        if (isRefreshing) {
          return new Promise((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          })
            .then((token) => {
              originalRequest.headers.Authorization = `Bearer ${token}`;
              return instance(originalRequest);
            })
            .catch((err) => Promise.reject(err));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const newToken = await authService.refreshToken();
          useAuthStore.getState().setAccessToken(newToken);
          processQueue(null, newToken);
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return instance(originalRequest);
        } catch (refreshError) {
          processQueue(refreshError, null);
          useAuthStore.getState().clearAuth();
          window.location.href = "/login";
          return Promise.reject(refreshError);
        } finally {
          isRefreshing = false;
        }
      }

      return Promise.reject(error);
    }
  );
};
```

### Error Interceptor

`src/core/api/interceptors/errorInterceptor.ts`:

```typescript
import type { AxiosInstance, AxiosError } from "axios";
import { AppError } from "@/core/error/AppError";

export const attachErrorInterceptor = (instance: AxiosInstance): void => {
  instance.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      const apiError = normalizeApiError(error);
      return Promise.reject(apiError);
    }
  );
};

const normalizeApiError = (error: AxiosError): AppError => {
  if (error.response) {
    const data = error.response.data as Record<string, unknown>;
    return new AppError({
      message: (data?.message as string) ?? "An error occurred",
      code: (data?.code as string) ?? "API_ERROR",
      statusCode: error.response.status,
      details: data?.errors,
    });
  }

  if (error.request) {
    return new AppError({
      message: "Network error — please check your connection",
      code: "NETWORK_ERROR",
      statusCode: 0,
    });
  }

  return new AppError({
    message: error.message ?? "Unexpected error",
    code: "UNKNOWN_ERROR",
    statusCode: -1,
  });
};
```

### AppError Class

`src/core/error/AppError.ts`:

```typescript
interface AppErrorOptions {
  message: string;
  code: string;
  statusCode: number;
  details?: unknown;
}

export class AppError extends Error {
  readonly code: string;
  readonly statusCode: number;
  readonly details: unknown;

  constructor({ message, code, statusCode, details }: AppErrorOptions) {
    super(message);
    this.name = "AppError";
    this.code = code;
    this.statusCode = statusCode;
    this.details = details;
  }

  get isUnauthorized() {
    return this.statusCode === 401;
  }

  get isForbidden() {
    return this.statusCode === 403;
  }

  get isNotFound() {
    return this.statusCode === 404;
  }

  get isServerError() {
    return this.statusCode >= 500;
  }
}
```

### API Module Pattern

`src/features/users/api/usersApi.ts`:

```typescript
import { z } from "zod";
import { axiosInstance } from "@/core/api/axiosInstance";
import type {
  CreateUserPayload,
  UpdateUserPayload,
  User,
} from "../types/user.types";
import type { PaginatedResponse } from "@/shared/types/api.types";

const USERS_BASE = "/users";

const userSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email(),
  name: z.string(),
  role: z.enum(["admin", "manager", "viewer"]),
  status: z.enum(["active", "inactive"]),
  createdAt: z.string().datetime(),
});

export const usersApi = {
  getUsers: async (params: {
    page: number;
    pageSize: number;
    search?: string;
    role?: string;
  }): Promise<PaginatedResponse<User>> => {
    const { data } = await axiosInstance.get<PaginatedResponse<User>>(
      USERS_BASE,
      { params }
    );
    return data;
  },

  getUserById: async (userId: string): Promise<User> => {
    const { data } = await axiosInstance.get<unknown>(`${USERS_BASE}/${userId}`);
    return userSchema.parse(data) as User;
  },

  createUser: async (payload: CreateUserPayload): Promise<User> => {
    const { data } = await axiosInstance.post<User>(USERS_BASE, payload);
    return data;
  },

  updateUser: async (
    userId: string,
    payload: UpdateUserPayload
  ): Promise<User> => {
    const { data } = await axiosInstance.patch<User>(
      `${USERS_BASE}/${userId}`,
      payload
    );
    return data;
  },

  deleteUser: async (userId: string): Promise<void> => {
    await axiosInstance.delete(`${USERS_BASE}/${userId}`);
  },
};
```

---

## 8. TANSTACK QUERY STRATEGY

### Query Client Configuration

`src/core/query/queryClient.ts`:

```typescript
import { QueryClient } from "@tanstack/react-query";
import { AppError } from "@/core/error/AppError";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,       // 5 minutes — server data is fresh for 5min
      gcTime: 1000 * 60 * 30,          // 30 minutes garbage collection
      retry: (failureCount, error) => {
        // Do not retry 4xx errors — these are client errors
        if (error instanceof AppError && error.statusCode < 500) return false;
        return failureCount < 2;
      },
      refetchOnWindowFocus: false,      // Disable for enterprise apps with heavy data
      throwOnError: false,
    },
    mutations: {
      retry: 0,
    },
  },
});
```

### Query Key Factory

`src/core/query/queryKeys.ts`:

```typescript
// Centralized query key registry ensures consistent cache keys across the app
// and enables targeted cache invalidation without string-matching

export const queryKeys = {
  // Users domain
  users: {
    all: ["users"] as const,
    lists: () => [...queryKeys.users.all, "list"] as const,
    list: (filters: Record<string, unknown>) =>
      [...queryKeys.users.lists(), filters] as const,
    details: () => [...queryKeys.users.all, "detail"] as const,
    detail: (userId: string) =>
      [...queryKeys.users.details(), userId] as const,
  },

  // Dashboard domain
  dashboard: {
    all: ["dashboard"] as const,
    stats: () => [...queryKeys.dashboard.all, "stats"] as const,
    activity: () => [...queryKeys.dashboard.all, "activity"] as const,
  },

  // Auth domain
  auth: {
    me: ["auth", "me"] as const,
    permissions: (userId: string) =>
      ["auth", "permissions", userId] as const,
  },
} as const;
```

### Feature Query Hooks

`src/features/users/hooks/useUsers.ts`:

```typescript
import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/core/query/queryKeys";
import { usersApi } from "../api/usersApi";
import { useUsersFilterStore } from "../store/usersFilterStore";

export const useUsers = () => {
  const { search, role, status, page, pageSize } = useUsersFilterStore();

  const filters = { search, role, status, page, pageSize };

  return useQuery({
    queryKey: queryKeys.users.list(filters),
    queryFn: () => usersApi.getUsers(filters),
    placeholderData: (previousData) => previousData, // Prevents loading flash on filter change
  });
};
```

`src/features/users/hooks/useCreateUser.ts`:

```typescript
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/core/query/queryKeys";
import { usersApi } from "../api/usersApi";
import type { CreateUserPayload } from "../types/user.types";
import { toast } from "sonner";

export const useCreateUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateUserPayload) => usersApi.createUser(payload),

    onSuccess: (newUser) => {
      // Invalidate the user list cache — triggers refetch for all list queries
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });

      // Optimistically seed the detail cache
      queryClient.setQueryData(queryKeys.users.detail(newUser.id), newUser);

      toast.success(`User ${newUser.name} created successfully`);
    },

    onError: (error) => {
      toast.error(error.message ?? "Failed to create user");
    },
  });
};
```

`src/features/users/hooks/useUpdateUser.ts`:

```typescript
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/core/query/queryKeys";
import { usersApi } from "../api/usersApi";
import type { UpdateUserPayload, User } from "../types/user.types";
import { toast } from "sonner";

export const useUpdateUser = (userId: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateUserPayload) =>
      usersApi.updateUser(userId, payload),

    // Optimistic update — update cache before the request completes
    onMutate: async (payload) => {
      await queryClient.cancelQueries({
        queryKey: queryKeys.users.detail(userId),
      });

      const previousUser = queryClient.getQueryData<User>(
        queryKeys.users.detail(userId)
      );

      queryClient.setQueryData<User>(queryKeys.users.detail(userId), (old) =>
        old ? { ...old, ...payload } : old
      );

      return { previousUser };
    },

    onError: (error, _variables, context) => {
      // Rollback on failure
      if (context?.previousUser) {
        queryClient.setQueryData(
          queryKeys.users.detail(userId),
          context.previousUser
        );
      }
      toast.error(error.message ?? "Failed to update user");
    },

    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.users.detail(userId),
      });
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
      toast.success("User updated successfully");
    },
  });
};
```

---

## 9. FORM ARCHITECTURE

### Login Form Schema

`src/features/auth/schemas/loginSchema.ts`:

```typescript
import { z } from "zod";

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
  password: z
    .string()
    .min(1, "Password is required")
    .min(8, "Password must be at least 8 characters"),
  rememberMe: z.boolean().optional().default(false),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
```

### Reusable Form Field Component

`src/shared/components/FormField/FormField.tsx`:

```typescript
import type { ReactNode } from "react";
import {
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
  FormDescription,
} from "@/shared/components/ui/form";

interface FormFieldWrapperProps {
  label?: string;
  description?: string;
  required?: boolean;
  children: ReactNode;
}

export const FormFieldWrapper = ({
  label,
  description,
  required,
  children,
}: FormFieldWrapperProps) => (
  <FormItem>
    {label && (
      <FormLabel>
        {label}
        {required && <span className="ml-1 text-destructive">*</span>}
      </FormLabel>
    )}
    <FormControl>{children}</FormControl>
    {description && <FormDescription>{description}</FormDescription>}
    <FormMessage />
  </FormItem>
);
```

### Login Form Implementation

`src/features/auth/components/LoginForm.tsx`:

```typescript
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Form,
  FormField,
} from "@/shared/components/ui/form";
import { Button } from "@/shared/components/ui/button";
import { Input } from "@/shared/components/ui/input";
import { Checkbox } from "@/shared/components/ui/checkbox";
import { FormFieldWrapper } from "@/shared/components/FormField";
import { loginSchema, type LoginFormValues } from "../schemas/loginSchema";
import { useLogin } from "../hooks/useLogin";

export const LoginForm = () => {
  const { mutate: login, isPending } = useLogin();

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: "",
      password: "",
      rememberMe: false,
    },
  });

  const handleSubmit = (values: LoginFormValues) => {
    login(values);
  };

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(handleSubmit)}
        className="space-y-4"
        noValidate
      >
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormFieldWrapper label="Email" required>
              <Input
                {...field}
                type="email"
                placeholder="you@company.com"
                autoComplete="email"
                disabled={isPending}
              />
            </FormFieldWrapper>
          )}
        />

        <FormField
          control={form.control}
          name="password"
          render={({ field }) => (
            <FormFieldWrapper label="Password" required>
              <Input
                {...field}
                type="password"
                placeholder="••••••••"
                autoComplete="current-password"
                disabled={isPending}
              />
            </FormFieldWrapper>
          )}
        />

        <FormField
          control={form.control}
          name="rememberMe"
          render={({ field }) => (
            <div className="flex items-center gap-2">
              <Checkbox
                id="rememberMe"
                checked={field.value}
                onCheckedChange={field.onChange}
                disabled={isPending}
              />
              <label
                htmlFor="rememberMe"
                className="cursor-pointer text-sm text-muted-foreground"
              >
                Remember me for 30 days
              </label>
            </div>
          )}
        />

        <Button type="submit" className="w-full" disabled={isPending}>
          {isPending ? "Signing in..." : "Sign in"}
        </Button>
      </form>
    </Form>
  );
};
```

---

## 10. AUTHENTICATION FOUNDATION

### Auth Service

`src/core/auth/authService.ts`:

```typescript
import { axiosInstance } from "@/core/api/axiosInstance";
import type {
  LoginPayload,
  AuthResponse,
  RefreshResponse,
} from "@/shared/types/common.types";
import { tokenStorage } from "./tokenStorage";

export const authService = {
  login: async (payload: LoginPayload): Promise<AuthResponse> => {
    const { data } = await axiosInstance.post<AuthResponse>(
      "/auth/login",
      payload
    );
    tokenStorage.setRefreshToken(data.refreshToken);
    return data;
  },

  logout: async (): Promise<void> => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (refreshToken) {
      try {
        await axiosInstance.post("/auth/logout", { refreshToken });
      } catch {
        // Always clear local state even if server call fails
      }
    }
    tokenStorage.clearRefreshToken();
  },

  refreshToken: async (): Promise<string> => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (!refreshToken) throw new Error("No refresh token available");

    const { data } = await axiosInstance.post<RefreshResponse>(
      "/auth/refresh",
      { refreshToken }
    );

    tokenStorage.setRefreshToken(data.refreshToken);
    return data.accessToken;
  },

  getMe: async () => {
    const { data } = await axiosInstance.get("/auth/me");
    return data;
  },
};
```

### Token Storage

`src/core/auth/tokenStorage.ts`:

```typescript
// Refresh tokens are stored in localStorage (HttpOnly cookies preferred in production)
// Access tokens are stored in-memory via Zustand (never localStorage directly)

const REFRESH_TOKEN_KEY = "app.rt";

export const tokenStorage = {
  getRefreshToken: (): string | null => {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  setRefreshToken: (token: string): void => {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
  },

  clearRefreshToken: (): void => {
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  },
};
```

### Login Mutation Hook

`src/features/auth/hooks/useLogin.ts`:

```typescript
import { useMutation } from "@tanstack/react-query";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuthStore } from "@/core/store/authStore";
import { authService } from "@/core/auth/authService";
import type { LoginFormValues } from "../schemas/loginSchema";
import { toast } from "sonner";

export const useLogin = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const setCredentials = useAuthStore((s) => s.setCredentials);

  const from =
    (location.state as { from?: Location })?.from?.pathname ?? "/dashboard";

  return useMutation({
    mutationFn: (values: LoginFormValues) =>
      authService.login({
        email: values.email,
        password: values.password,
      }),

    onSuccess: (data) => {
      setCredentials(data.user, data.accessToken);
      navigate(from, { replace: true });
    },

    onError: (error) => {
      toast.error(error.message ?? "Invalid credentials");
    },
  });
};
```

### Permission-Based Rendering

`src/shared/components/Can/Can.tsx`:

```typescript
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
```

---

## 11. ERROR HANDLING STRATEGY

### Global Error Boundary

`src/core/error/GlobalErrorBoundary.tsx`:

```typescript
import { Component, type ReactNode, type ErrorInfo } from "react";
import { Button } from "@/shared/components/ui/button";

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorId: string | null;
}

export class GlobalErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null, errorId: null };
  }

  static getDerivedStateFromError(error: Error): State {
    const errorId = `err_${Date.now()}_${Math.random().toString(36).slice(2)}`;
    return { hasError: true, error, errorId };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    // Send to monitoring service (e.g., Sentry)
    console.error("[GlobalErrorBoundary]", {
      error,
      componentStack: info.componentStack,
      errorId: this.state.errorId,
    });

    // In production: Sentry.captureException(error, { extra: { errorId, componentStack } })
  }

  handleReset = (): void => {
    this.setState({ hasError: false, error: null, errorId: null });
  };

  render(): ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <div className="flex min-h-screen flex-col items-center justify-center gap-6 p-8 text-center">
          <div className="space-y-2">
            <h1 className="text-3xl font-bold tracking-tight">
              Something went wrong
            </h1>
            <p className="text-muted-foreground">
              An unexpected error occurred. Our team has been notified.
            </p>
            {this.state.errorId && (
              <p className="font-mono text-xs text-muted-foreground">
                Error ID: {this.state.errorId}
              </p>
            )}
          </div>
          <div className="flex gap-3">
            <Button onClick={this.handleReset}>Try Again</Button>
            <Button variant="outline" onClick={() => window.location.assign("/")}>
              Return Home
            </Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
```

### Error Normalization Hook

`src/shared/hooks/useErrorHandler.ts`:

```typescript
import { useCallback } from "react";
import { toast } from "sonner";
import { AppError } from "@/core/error/AppError";

export const useErrorHandler = () => {
  const handleError = useCallback((error: unknown) => {
    if (error instanceof AppError) {
      if (error.isForbidden) {
        toast.error("You do not have permission to perform this action");
        return;
      }
      if (error.isNotFound) {
        toast.error("The requested resource was not found");
        return;
      }
      if (error.isServerError) {
        toast.error("A server error occurred. Please try again later.");
        return;
      }
      toast.error(error.message);
      return;
    }

    if (error instanceof Error) {
      toast.error(error.message);
      return;
    }

    toast.error("An unexpected error occurred");
  }, []);

  return { handleError };
};
```

---

## 12. PERFORMANCE OPTIMIZATION

### Memoization Strategy

Apply memoization only where there is a measurable performance impact. Premature memoization adds noise. Use React DevTools Profiler to identify actual bottlenecks.

**Appropriate memoization targets:**
- Components with expensive renders that receive stable props
- Derived values computed from large arrays or objects
- Event handlers passed to deeply nested or listed components

```typescript
// Expensive list component — stable rendering critical
import { memo, useMemo, useCallback } from "react";

interface UserRowProps {
  user: User;
  onSelect: (userId: string) => void;
}

export const UserRow = memo(({ user, onSelect }: UserRowProps) => {
  const handleSelect = useCallback(() => {
    onSelect(user.id);
  }, [user.id, onSelect]);

  return (
    <tr onClick={handleSelect} className="cursor-pointer hover:bg-muted/50">
      <td>{user.name}</td>
      <td>{user.email}</td>
    </tr>
  );
});

UserRow.displayName = "UserRow";
```

### Suspense Architecture

Wrap each route boundary and each independently-loadable data section:

```typescript
// Nested Suspense for granular loading states
const DashboardPage = () => (
  <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
    <Suspense fallback={<StatCardSkeleton count={4} />}>
      <DashboardStats />
    </Suspense>

    <Suspense fallback={<TableSkeleton rows={8} />}>
      <RecentActivity />
    </Suspense>
  </div>
);
```

### Bundle Optimization

`vite.config.ts` manual chunks (see Section 2 for full config):

```typescript
manualChunks: {
  // Vendor chunk — rarely changes, maximizes cache hits
  vendor: ["react", "react-dom"],
  // Router — separate from vendor, can be updated independently
  router: ["react-router-dom"],
  // Query — large dep, isolated chunk
  query: ["@tanstack/react-query"],
  // Radix UI — tree-shaken per component, but still large
  radix: [
    "@radix-ui/react-dialog",
    "@radix-ui/react-dropdown-menu",
    "@radix-ui/react-select",
    "@radix-ui/react-table",
  ],
}
```

### `useDebounce` Hook

`src/shared/hooks/useDebounce.ts`:

```typescript
import { useEffect, useState } from "react";

export const useDebounce = <T>(value: T, delay: number = 300): T => {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debouncedValue;
};
```

---

## 13. ENVIRONMENT CONFIGURATION

### Zod-Validated Env

`src/config/env.ts`:

```typescript
import { z } from "zod";

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().url("VITE_API_BASE_URL must be a valid URL"),
  VITE_APP_ENV: z.enum(["development", "staging", "production"]),
  VITE_ENABLE_DEVTOOLS: z
    .string()
    .transform((v) => v === "true")
    .pipe(z.boolean()),
  VITE_APP_VERSION: z.string().optional().default("0.0.0"),
});

type Env = z.infer<typeof envSchema>;

const parseEnv = (): Env => {
  const result = envSchema.safeParse(import.meta.env);

  if (!result.success) {
    const formatted = result.error.issues
      .map((issue) => `  • ${issue.path.join(".")}: ${issue.message}`)
      .join("\n");

    throw new Error(
      `Environment variable validation failed:\n${formatted}\n\nCheck your .env files.`
    );
  }

  return result.data;
};

// Throw at startup — fail loudly before rendering anything
export const env = parseEnv();
```

### Constants

`src/config/constants.ts`:

```typescript
import type { UserRole, Permission } from "@/shared/types/common.types";

export const PAGINATION_PAGE_SIZES = [10, 20, 50, 100] as const;
export const DEFAULT_PAGE_SIZE = 20;
export const DEFAULT_STALE_TIME_MS = 1000 * 60 * 5;
export const REQUEST_TIMEOUT_MS = 30_000;

export const ROLE_PERMISSIONS: Record<UserRole, Permission[]> = {
  admin: ["users:read", "users:write", "users:delete", "settings:write"],
  manager: ["users:read", "users:write"],
  viewer: ["users:read"],
};
```

---

## 14. CODE QUALITY SYSTEM

### ESLint Configuration

`.eslintrc.cjs`:

```javascript
/** @type {import('eslint').Linter.Config} */
module.exports = {
  root: true,
  env: { browser: true, es2022: true },
  extends: [
    "eslint:recommended",
    "plugin:@typescript-eslint/strict-type-checked",
    "plugin:react/recommended",
    "plugin:react/jsx-runtime",
    "plugin:react-hooks/recommended",
    "plugin:jsx-a11y/recommended",
    "plugin:import/recommended",
    "plugin:import/typescript",
  ],
  parser: "@typescript-eslint/parser",
  parserOptions: {
    ecmaVersion: "latest",
    sourceType: "module",
    project: ["./tsconfig.json", "./tsconfig.node.json"],
    tsconfigRootDir: __dirname,
  },
  plugins: [
    "@typescript-eslint",
    "react-refresh",
    "react",
    "jsx-a11y",
    "import",
  ],
  settings: {
    react: { version: "detect" },
    "import/resolver": {
      typescript: { alwaysTryTypes: true, project: "./tsconfig.json" },
    },
  },
  rules: {
    // React
    "react-refresh/only-export-components": [
      "warn",
      { allowConstantExport: true },
    ],
    "react/prop-types": "off", // TypeScript handles this

    // TypeScript
    "@typescript-eslint/no-explicit-any": "error",
    "@typescript-eslint/no-unused-vars": [
      "error",
      { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
    ],
    "@typescript-eslint/consistent-type-imports": [
      "error",
      { prefer: "type-imports", fixStyle: "inline-type-imports" },
    ],
    "@typescript-eslint/no-floating-promises": "error",
    "@typescript-eslint/await-thenable": "error",

    // Imports
    "import/order": [
      "error",
      {
        groups: [
          "builtin",
          "external",
          "internal",
          ["parent", "sibling", "index"],
        ],
        "newlines-between": "always",
        alphabetize: { order: "asc", caseInsensitive: true },
      },
    ],
    "import/no-default-export": "off", // Pages and layouts use default exports

    // General
    "no-console": ["warn", { allow: ["warn", "error"] }],
    eqeqeq: ["error", "always"],
  },
  overrides: [
    {
      // Relax rules for config files
      files: ["*.config.ts", "*.config.cjs", "vite.config.ts"],
      rules: {
        "import/no-default-export": "off",
        "@typescript-eslint/no-var-requires": "off",
      },
    },
  ],
};
```

### Prettier Configuration

`.prettierrc`:

```json
{
  "semi": true,
  "singleQuote": false,
  "tabWidth": 2,
  "trailingComma": "all",
  "printWidth": 90,
  "bracketSpacing": true,
  "arrowParens": "always",
  "endOfLine": "lf",
  "plugins": ["prettier-plugin-tailwindcss"]
}
```

`.prettierignore`:

```
dist/
node_modules/
pnpm-lock.yaml
.env*
*.md
```

---

## 15. ENTERPRISE TYPESCRIPT STRATEGY

### Shared Types

`src/shared/types/common.types.ts`:

```typescript
export type UserRole = "admin" | "manager" | "viewer";

export type Permission =
  | "users:read"
  | "users:write"
  | "users:delete"
  | "settings:write";

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  avatarUrl?: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export interface AuthResponse {
  user: AuthUser;
  accessToken: string;
  refreshToken: string;
}

export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
}
```

`src/shared/types/api.types.ts`:

```typescript
export interface PaginatedResponse<T> {
  data: T[];
  meta: {
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
  };
}

export interface ApiErrorResponse {
  message: string;
  code: string;
  errors?: Record<string, string[]>;
}

// Generic Result type — used in service layers
export type Result<T, E = Error> =
  | { success: true; data: T }
  | { success: false; error: E };
```

### Domain Types

`src/features/users/types/user.types.ts`:

```typescript
import type { UserRole } from "@/shared/types/common.types";

export interface User {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  status: "active" | "inactive";
  department?: string;
  createdAt: string;
  updatedAt: string;
}

export type CreateUserPayload = Pick<User, "email" | "name" | "role"> & {
  password: string;
};

export type UpdateUserPayload = Partial<
  Pick<User, "name" | "role" | "status" | "department">
>;
```

### Utility Types

```typescript
// Make specific fields required in an otherwise Partial type
export type RequiredFields<T, K extends keyof T> = Omit<T, K> &
  Required<Pick<T, K>>;

// Extract the value type of a Record
export type ValueOf<T> = T[keyof T];

// Construct a type with all properties set to never except the specified keys
export type Only<T, K extends keyof T> = Pick<T, K> &
  Partial<Record<Exclude<keyof T, K>, never>>;

// Typed event handler
export type ChangeHandler<T = HTMLInputElement> =
  React.ChangeEventHandler<T>;

// Typed async action
export type AsyncAction<TArgs = void, TReturn = void> = (
  args: TArgs
) => Promise<TReturn>;
```

---

## 16. REUSABLE ARCHITECTURAL PATTERNS

### Container / Presentation Pattern

```typescript
// Presentation: dumb, highly reusable, receives all data via props
// src/features/users/components/UserTable.tsx
interface UserTableProps {
  users: User[];
  isLoading: boolean;
  onEdit: (userId: string) => void;
  onDelete: (userId: string) => void;
}

export const UserTable = ({
  users,
  isLoading,
  onEdit,
  onDelete,
}: UserTableProps) => {
  if (isLoading) return <TableSkeleton rows={5} />;

  return (
    <DataTable
      data={users}
      columns={userColumns({ onEdit, onDelete })}
    />
  );
};

// Container: owns data fetching, selection, and state
// src/features/users/pages/UsersListPage.tsx
export const UsersListPage = () => {
  const { data, isLoading } = useUsers();
  const { mutate: deleteUser } = useDeleteUser();
  const navigate = useNavigate();

  const handleEdit = (userId: string) => navigate(`/users/${userId}`);
  const handleDelete = (userId: string) => deleteUser(userId);

  return (
    <div className="space-y-6">
      <PageHeader title="Users" actions={<CreateUserButton />} />
      <UserTable
        users={data?.data ?? []}
        isLoading={isLoading}
        onEdit={handleEdit}
        onDelete={handleDelete}
      />
    </div>
  );
};
```

### Providers Composition

`src/providers/index.ts`:

```typescript
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
```

### Custom Hook Pattern

```typescript
// Hooks encapsulate behavior. A hook that combines query + filter store:
// src/features/users/hooks/useUsersList.ts
export const useUsersList = () => {
  const filters = useUsersFilterStore();
  const { data, isLoading, isError, error } = useUsers();
  const debouncedSearch = useDebounce(filters.search, 300);

  return {
    users: data?.data ?? [],
    pagination: data?.meta,
    isLoading,
    isError,
    error,
    filters,
    debouncedSearch,
  } as const;
};
```

---

## 17. SECURITY BEST PRACTICES

### Token Security

- **Access tokens** are stored in Zustand (in-memory). They are never written to `localStorage` directly.
- **Refresh tokens** are stored in `localStorage` with a short-lived key name. In a high-security production environment, refresh tokens should be stored in `HttpOnly` cookies issued by the server, making them inaccessible to JavaScript entirely.
- Tokens are never logged, never included in URL parameters, and never sent to third-party domains.

### XSS Prevention

- React's JSX escapes all dynamic values by default. Never use `dangerouslySetInnerHTML` unless the content has been sanitized with `DOMPurify`.
- CSP headers are configured at the server/CDN level:

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self';
  style-src 'self' 'unsafe-inline';
  img-src 'self' data: https:;
  connect-src 'self' https://api.myapp.com;
  frame-ancestors 'none';
```

### Route Security

- `ProtectedRoute` prevents rendering any authenticated content before the auth store is populated.
- `RoleGuard` prevents rendering role-specific content without explicit role matching.
- Sensitive UI sections use the `<Can>` component with permission checks.

### API Security Considerations

- All API requests include the `Authorization: Bearer <token>` header via the request interceptor.
- The `axiosInstance` is configured with a 30-second timeout to prevent request hanging.
- API base URLs are read from environment variables — never hardcoded.
- HTTPS is enforced on all API endpoints. Mixed content is blocked by the CSP.

---

## 18. CI/CD READY FOUNDATION

### GitHub Actions Workflow

`.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  quality:
    name: Type Check, Lint, Format
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v3
        with:
          version: 9

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: "pnpm"

      - name: Install dependencies
        run: pnpm install --frozen-lockfile

      - name: Type check
        run: pnpm type-check

      - name: Lint
        run: pnpm lint

      - name: Format check
        run: pnpm format:check

  build:
    name: Production Build
    runs-on: ubuntu-latest
    needs: quality
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v3
        with:
          version: 9

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: "pnpm"

      - name: Install dependencies
        run: pnpm install --frozen-lockfile

      - name: Build
        run: pnpm build
        env:
          VITE_API_BASE_URL: ${{ secrets.VITE_API_BASE_URL }}
          VITE_APP_ENV: production
          VITE_ENABLE_DEVTOOLS: "false"

      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: dist
          path: dist/
          retention-days: 7
```

### Production Build Checklist

Before deploying to production, verify:

- `pnpm type-check` exits with code 0
- `pnpm lint` exits with code 0 and zero warnings
- `pnpm build` completes without errors
- Bundle analysis (`pnpm build:analyze`) shows no unexpected large chunks
- All environment variables are set in the deployment environment
- CSP headers are configured on the hosting server
- HTTPS is enforced with HSTS headers
- `robots.txt` and `sitemap.xml` are present for SEO (if applicable)

---

## 19. ENTERPRISE SCALING STRATEGY

### Multi-Team Scaling

This architecture supports up to 10+ feature teams with zero coordination overhead for routine development:

- **Team A** owns `src/features/users/` end-to-end
- **Team B** owns `src/features/dashboard/` end-to-end
- **Platform Team** owns `src/core/`, `src/shared/`, `src/layouts/`, `src/routes/`

Teams import from each other's **public API** (`index.ts`) only — never from internal files. This enforces the equivalent of package-level encapsulation without the overhead of a monorepo.

### Feature Module Public API

Each feature's `index.ts` explicitly exports only what other modules should consume:

```typescript
// src/features/users/index.ts
// Public API — the only file other features may import from

export { UsersListPage } from "./pages/UsersListPage";
export { UserDetailPage } from "./pages/UserDetailPage";
export type { User, CreateUserPayload } from "./types/user.types";
// Internal components, hooks, and API modules are NOT re-exported
```

### Microfrontend Readiness

Each feature module is structured as a self-contained unit that can be extracted into a Webpack Module Federation remote with minimal refactoring:

- No circular dependencies between feature modules
- Feature modules have no knowledge of each other (no cross-feature imports)
- Shared infrastructure lives in `src/core/` and can become a shared singleton
- CSS is scoped via Tailwind utility classes — no global class collisions

### Monorepo Compatibility

The project structure maps directly to a Turborepo workspace:

```
apps/
  web/               # This application
  admin/             # Separate admin SPA sharing packages
packages/
  ui/                # Extracted shared/components → shared package
  types/             # Extracted shared/types → shared package
  api-client/        # Extracted core/api → shared package
  config/            # Shared ESLint, TypeScript, Tailwind configs
```

---

## 20. FINAL ENGINEERING STANDARDS

### Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| React components | `PascalCase` | `UserDetailPage`, `LoginForm` |
| Hooks | `camelCase` prefixed with `use` | `useUsers`, `useCreateUser` |
| Utilities | `camelCase` | `formatCurrency`, `parseDate` |
| Constants | `SCREAMING_SNAKE_CASE` | `DEFAULT_PAGE_SIZE` |
| TypeScript types/interfaces | `PascalCase` | `AuthUser`, `PaginatedResponse` |
| Zod schemas | `camelCase` + `Schema` suffix | `loginSchema`, `userSchema` |
| API modules | `camelCase` + `Api` suffix | `usersApi`, `authApi` |
| Store files | `camelCase` + `Store` suffix | `authStore`, `uiStore` |
| CSS class names | Tailwind utilities only — no custom class names |
| Event handlers | `handleXxx` prefix | `handleSubmit`, `handleDelete` |

### File Conventions

- Maximum **150 lines** per file in `src/shared/`. Larger components are split.
- Maximum **200 lines** per file in `src/features/`. Pages that exceed this extract subcomponents.
- Every file has a **single responsibility**. A file that exports more than one component is a code smell (layout files excepted).
- Index files (`index.ts`) are used for barrel exports only — no logic.
- Pages are **never** imported directly. Only via the feature's `index.ts`.

### Import Conventions

```typescript
// Order enforced by eslint-plugin-import:
// 1. Node built-ins
// 2. External packages
// 3. Internal aliases (@/)
// 4. Relative imports

import { useEffect, useState } from "react";          // 2. External

import { useQuery } from "@tanstack/react-query";      // 2. External

import { axiosInstance } from "@/core/api";            // 3. Internal
import { useAuthStore } from "@/core/store/authStore"; // 3. Internal
import type { User } from "@/shared/types/common.types"; // 3. Internal

import { UserTable } from "../components/UserTable";   // 4. Relative
import { useUsers } from "../hooks/useUsers";          // 4. Relative
```

- Use `type` imports for TypeScript-only imports: `import type { Foo } from "..."`
- Never use wildcard imports: `import * as foo` (except for third-party interop)
- Avoid default exports except for React components and pages

### Architecture Rules

1. **Components never call Axios directly.** Always via a hook that wraps a TanStack Query mutation or query.
2. **Hooks never import from other feature modules.** Cross-feature data is passed via props or shared through `src/core/`.
3. **Zustand stores are never imported in presentation components.** Container components select from stores and pass data as props.
4. **Zod schemas define the shape; TypeScript infers the types.** Never duplicate schema definitions.
5. **Every async operation has an error state.** No fire-and-forget mutations without error handling.
6. **`any` is banned.** TypeScript's `strict: true` and ESLint's `no-explicit-any: error` enforce this.
7. **No inline styles.** All styling via Tailwind utility classes.
8. **No magic strings for query keys.** Always use `queryKeys` factory.

### Engineering Governance Standards

- **PR size limit**: PRs should not exceed 400 lines of changed code. Large features are broken into incremental vertical slices.
- **Definition of Done**: A feature is done when it has TypeScript coverage (zero `any`), lint passes, and the PR description includes a description of the API contract changes.
- **Deprecation process**: Deprecated utilities are marked with a JSDoc `@deprecated` tag and a removal target version before deletion.
- **Breaking changes**: Changes to public feature APIs (`index.ts` exports) require a PR review from the platform team.
- **Dependency additions**: New npm dependencies require justification in the PR description covering: bundle size impact, maintenance status, and alternative evaluation.
