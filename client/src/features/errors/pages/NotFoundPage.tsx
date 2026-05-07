import { Link } from "react-router-dom";

export const NotFoundPage = () => (
  <div className="flex min-h-screen flex-col items-center justify-center gap-6 px-4 text-center">
    <div className="space-y-3">
      <h1 className="text-4xl font-semibold">404</h1>
      <p className="text-sm text-muted-foreground">Page not found.</p>
    </div>
    <Link to="/dashboard" className="rounded-full bg-primary px-5 py-3 text-sm font-medium text-primary-foreground">
      Go to dashboard
    </Link>
  </div>
);
