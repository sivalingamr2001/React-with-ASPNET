import React, { useEffect, useState } from "react";
import { useAuth } from "../../context/auth-provider";

export default function QueryBuilder() {
  const { token } = useAuth();
  const [profiles, setProfiles] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      const res = await fetch("/api/dataengine/profiles", {
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      });
      if (res.ok) {
        setProfiles(await res.json());
      }
      setLoading(false);
    };
    load();
  }, [token]);

  return (
    <div style={{ padding: 28 }}>
      <h2>Query Builder (Admin)</h2>
      {loading ? (
        <div>Loading profiles…</div>
      ) : (
        <div>
          {profiles.map((p) => (
            <div
              key={p.id}
              style={{ padding: 8, borderBottom: "1px solid #e6eef8" }}
            >
              <strong>{p.name}</strong>{" "}
              <span style={{ color: "#64748b" }}>{p.provider}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
