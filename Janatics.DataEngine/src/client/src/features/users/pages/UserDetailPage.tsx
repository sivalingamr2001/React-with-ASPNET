import { useParams, Link } from "react-router-dom";

export const UserDetailPage = () => {
  const { userId } = useParams();

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold">User details</h1>
        <p className="text-sm text-muted-foreground">Details for user ID: {userId}</p>
      </div>
      <div className="rounded-2xl border border-border p-5">
        <p className="text-sm">This page is a placeholder for the user detail view.</p>
      </div>
      <Link to="/users" className="text-primary underline">
        Back to users list
      </Link>
    </div>
  );
};
