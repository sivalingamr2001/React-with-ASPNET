import { lazy, Suspense } from "react";
import {
    createBrowserRouter,
    RouterProvider,
    Navigate,
} from "react-router-dom";
import { AppLayout } from "@/layouts/AppLayout";
import { AuthLayout } from "@/layouts/AuthLayout";
import { ProtectedRoute } from "./ProtectedRoute";
import { RoleGuard } from "./RoleGuard";
import { RouteErrorBoundary } from "@/core/error/RouteErrorBoundary";
import { BlankLayout } from "@/layouts/BlankLayout/BlankLayout";
import { PageLoader } from "@/shared/components/LoadingSpinner/LoadingSpinner";

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
], { basename: "/react-app" });

export const Router = () => <RouterProvider router={router} />;