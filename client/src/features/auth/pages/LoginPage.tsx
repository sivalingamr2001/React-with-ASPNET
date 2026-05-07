import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuthStore } from "@/core/store/authStore";
import { Button } from "@/shared/components/ui/button";

export const LoginPage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const setCredentials = useAuthStore((s) => s.setCredentials);

  const from = (location.state as { from?: Location })?.from?.pathname ?? "/dashboard";

  const handleDemoLogin = () => {
    setCredentials(
      {
        id: "1",
        email: "admin@example.com",
        name: "Admin User",
        role: "admin",
      },
      "demo-token"
    );

    navigate(from, { replace: true });
  };

  return (
    <div className="space-y-6 w-full max-w-md rounded-3xl border border-border bg-white p-8 shadow-lg shadow-slate-900/5">
      <div className="space-y-2 text-center">
        <h1 className="text-3xl font-semibold">Welcome back</h1>
        <p className="text-sm text-muted-foreground">
          Click the button below to sign in with the demo account.
        </p>
      </div>
      <Button className="w-full" onClick={handleDemoLogin}>
        Sign in as admin
      </Button>
      <p className="text-center text-sm text-muted-foreground">
        Forgot your password? <Link to="/forgot-password" className="text-primary underline">Reset it here</Link>.
      </p>
    </div>
  );
};
