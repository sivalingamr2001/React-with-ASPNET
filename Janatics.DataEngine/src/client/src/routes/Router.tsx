import { lazy, Suspense } from "react";
import {
    createBrowserRouter,
    RouterProvider,
    Navigate,
} from "react-router-dom";
import { AppLayout } from "@/layouts/AppLayout";
import { AuthLayout } from "@/layouts/AuthLayout";
import { ProtectedRoute } from "./ProtectedRoute";
import { RouteErrorBoundary } from "@/core/error/RouteErrorBoundary";
import { BlankLayout } from "@/layouts/BlankLayout/BlankLayout";
import { PageLoader } from "@/shared/components/LoadingSpinner/LoadingSpinner";

// Lazy-loaded feature pages
const LoginPage = lazy(() =>
    import("@/shared/components/auth/LoginForm.tsx").then((m) => ({
        default: m.LoginForm,
    }))
);
const ForgotPasswordPage = lazy(() =>
    import("@/features/auth/pages/ForgotPasswordPage").then((m) => ({
        default: m.ForgotPasswordPage,
    }))
);
const DashboardV2Layout = lazy(() =>
    import("@/features/dashboard_v2/layout").then((m) => ({
        default: m.default,
    }))
);
const DashboardV2Page = lazy(() =>
    import("@/features/dashboard_v2/page").then((m) => ({
        default: m.default,
    }))
);
const DatabaseConfigPage = lazy(() =>
    import("@/features/dashboard_v2/db-config/page").then((m) => ({
        default: m.default,
    }))
);
const FieldMapperPage = lazy(() =>
    import("@/features/dashboard_v2/field-mapper/page").then((m) => ({
        default: m.default,
    }))
);
const LogsPage = lazy(() =>
    import("@/features/dashboard_v2/logs/page").then((m) => ({
        default: m.default,
    }))
);
const QueryBuilderPage = lazy(() =>
    import("@/features/dashboard_v2/query-builder/page").then((m) => ({
        default: m.default,
    }))
);
const TableManagerPage = lazy(() =>
    import("@/features/dashboard_v2/table-manager/page").then((m) => ({
        default: m.default,
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
    {
        element: <AuthLayout />,
        errorElement: <RouteErrorBoundary />,
        children: [
            { path: "/login", element: withSuspense(LoginPage) },
            { path: "/forgot-password", element: withSuspense(ForgotPasswordPage) },
        ],
    },
    {
        path: "/",
        element: <Navigate to="/dashboard" replace />,
    },
    {
        path: "/dashboard",
        element: (
            <ProtectedRoute>
                <Suspense fallback={<PageLoader />}>
                    <DashboardV2Layout />
                </Suspense>
            </ProtectedRoute>
        ),
        errorElement: <RouteErrorBoundary />,
        children: [
            { index: true, element: withSuspense(DashboardV2Page) },
            { path: "db-config", element: withSuspense(DatabaseConfigPage) },
            { path: "field-mapper", element: withSuspense(FieldMapperPage) },
            { path: "logs", element: withSuspense(LogsPage) },
            { path: "query-builder", element: withSuspense(QueryBuilderPage) },
            { path: "table-manager", element: withSuspense(TableManagerPage) },
        ],
    },
    {
        element: (
            <ProtectedRoute>
                <AppLayout />
            </ProtectedRoute>
        ),
        errorElement: <RouteErrorBoundary />,
        children: [
            {
                path: "/users",
                element: (
                    <Suspense fallback={<PageLoader />}>
                        <UsersListPage />
                    </Suspense>
                ),
            },
            {
                path: "/users/:userId",
                element: (
                    <Suspense fallback={<PageLoader />}>
                        <UserDetailPage />
                    </Suspense>
                ),
            },
        ],
    },
    {
        element: <BlankLayout />,
        children: [{ path: "*", element: withSuspense(NotFoundPage) }],
    },
], { basename: "/react-app" });

export const Router = () => <RouterProvider router={router} />;