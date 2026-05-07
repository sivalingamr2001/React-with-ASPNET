export const DashboardPage = () => (
  <div className="space-y-6">
    <div className="space-y-2">
      <h1 className="text-3xl font-semibold">Dashboard</h1>
      <p className="text-sm text-muted-foreground">This is the app dashboard. The router and protected routes are now wired.</p>
    </div>
    <div className="grid gap-4 sm:grid-cols-2">
      <div className="rounded-2xl border border-border p-5">Welcome to the admin experience.</div>
      <div className="rounded-2xl border border-border p-5">Use the sidebar to navigate between screens.</div>
    </div>
  </div>
);
