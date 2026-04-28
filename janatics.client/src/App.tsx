import AdminPortal from "./apps/admin/Index";
import UserPortal from "./apps/user";
import { useAuth } from "./context/auth-provider";
import ProtectedRoute from "./pages/ProtectedRoute";

export default function App() {
  const { user } = useAuth();

  return (
    <ProtectedRoute user={user}>
      {user?.role === "admin" ? <AdminPortal /> : <UserPortal />}
    </ProtectedRoute>
  );
}
