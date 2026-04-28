import React from "react";
import { useAuth } from "../../../context/auth-provider";

export default function Portal() {
  const { user, logout } = useAuth();
  return (
    <div style={{ padding: 28 }}>
      <h2>Welcome to the Portal</h2>
      <p style={{ color: "#64748b" }}>Signed in as <strong>{user?.name}</strong> ({user?.role})</p>
      <div style={{ marginTop: 12 }}>
        <button onClick={() => logout()} style={{ padding: 8, borderRadius: 6 }}>Sign out</button>
      </div>
    </div>
  );
}
