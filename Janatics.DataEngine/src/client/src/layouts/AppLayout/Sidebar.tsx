import { NavLink } from "react-router-dom";

const navItems = [
  { path: "/dashboard", label: "Dashboard" },
  { path: "/users", label: "Users" },
];

export const Sidebar = () => (
  <aside className="w-full border-b border-border bg-muted/75 p-4 sm:w-72 sm:border-r sm:border-b-0">
    <nav className="space-y-2">
      {navItems.map((item) => (
        <NavLink
          key={item.path}
          to={item.path}
          className={({ isActive }) =>
            `block rounded-xl px-4 py-3 text-sm font-medium transition ${
              isActive ? "bg-primary text-primary-foreground" : "text-foreground/80 hover:text-foreground"
            }`
          }
        >
          {item.label}
        </NavLink>
      ))}
    </nav>
  </aside>
);
