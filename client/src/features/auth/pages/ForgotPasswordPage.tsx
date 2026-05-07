import { Link } from "react-router-dom";
import { Button } from "@/shared/components/ui/button";

export const ForgotPasswordPage = () => (
  <div className="space-y-6 text-center">
    <div>
      <h1 className="text-3xl font-semibold">Forgot password</h1>
      <p className="text-sm text-muted-foreground">
        This demo app does not include a working reset flow.
      </p>
    </div>
    <Button asChild>
      <Link to="/login" className="w-full block">
        Back to sign in
      </Link>
    </Button>
  </div>
);
