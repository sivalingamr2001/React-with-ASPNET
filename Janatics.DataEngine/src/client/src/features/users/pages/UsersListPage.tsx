import { Link } from "react-router-dom";

const demoUsers = [
  { id: "1", name: "Alice Johnson" },
  { id: "2", name: "Brian Miles" },
  { id: "3", name: "Carla Nguyen" },
];

export const UsersListPage = () => (
  <div className="space-y-6">
    <div className="space-y-2">
      <h1 className="text-3xl font-semibold">Users</h1>
      <p className="text-sm text-muted-foreground">A minimal users list to verify route wiring.</p>
    </div>
    <div className="grid gap-4">
      {demoUsers.map((user) => (
        <Link
          key={user.id}
          to={`/users/${user.id}`}
          className="block rounded-2xl border border-border p-4 hover:border-primary hover:text-primary"
        >
          {user.name}
        </Link>
      ))}
    </div>
  </div>
);
