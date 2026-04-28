import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../../context/auth-provider";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const auth = useAuth();
  const navigate = useNavigate();

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    try {
      await auth.login(username, password);
      const role = auth.user?.role;
      if (role === "Admin") navigate("/admin");
      else navigate("/portal");
    } catch (err: any) {
      setError(err?.message || "Login failed");
    }
  };

  return (
    <div style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", background: "#071020" }}>
      <form onSubmit={onSubmit} style={{ background: "#0b1220", padding: 28, borderRadius: 8, width: 340, color: "#e6eef8" }}>
        <h3 style={{ marginBottom: 12 }}>Sign In</h3>
        <div style={{ marginBottom: 8 }}>
          <input placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} style={{ width: "100%", padding: 8, borderRadius: 6, border: "1px solid #21314a", background: "#071020", color: "#e6eef8" }} />
        </div>
        <div style={{ marginBottom: 12 }}>
          <input type="password" placeholder="Password" value={password} onChange={e => setPassword(e.target.value)} style={{ width: "100%", padding: 8, borderRadius: 6, border: "1px solid #21314a", background: "#071020", color: "#e6eef8" }} />
        </div>
        {error && <div style={{ color: "#ffb4b4", marginBottom: 10 }}>{error}</div>}
        <div style={{ display: "flex", justifyContent: "space-between", gap: 8 }}>
          <button style={{ flex: 1, padding: 8, borderRadius: 6, background: "#1e3a8a", color: "#fff", border: "none" }}>Sign in</button>
        </div>
      </form>
    </div>
  );
}
