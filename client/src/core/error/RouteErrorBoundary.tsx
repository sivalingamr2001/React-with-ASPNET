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