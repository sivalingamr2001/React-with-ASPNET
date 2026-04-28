import AppSkeleton from "./AppSkeleton";
import { useLayoutConfig } from "./hooks/useLayoutConfig";
import AdminRoutes from "./AdminRoutes";

export default function AdminShell() {
  const { config, isLoading } = useLayoutConfig();
  if (isLoading || !config) return <AppSkeleton />;
  return <AdminRoutes config={config} />;
}
