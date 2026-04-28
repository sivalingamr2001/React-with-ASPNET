import { useState } from "react";
import { Field, inputSx, selectSx } from "./ui";
import { PROVIDERS } from "./data";

export function ProfileForm({ initial, onSave, onClose }: any) {
  const [form, setForm] = useState(
    initial || {
      name: "",
      provider: "Oracle",
      host: "",
      port: "",
      database: "",
      username: "",
      password: "",
    },
  );
  const set = (k: string, v: any) => setForm((p: any) => ({ ...p, [k]: v }));
  const connStr =
    form.provider === "Sqlite"
      ? `Data Source=${form.database || "/path/to/file.db"};`
      : form.provider === "Oracle"
        ? `Data Source=${form.host}:${form.port}/${form.database};User Id=${form.username};Password=***;`
        : `Server=${form.host};Database=${form.database};User Id=${form.username};Password=***;`;

  return (
    <>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
        <Field label="Profile Name">
          <input
            style={inputSx}
            value={form.name}
            onChange={(e) => set("name", e.target.value)}
            placeholder="OracleERP"
          />
        </Field>
        <Field label="Provider">
          <select
            style={selectSx}
            value={form.provider}
            onChange={(e) => set("provider", e.target.value)}
          >
            {PROVIDERS.map((p) => (
              <option key={p}>{p}</option>
            ))}
          </select>
        </Field>
      </div>

      {form.provider !== "Sqlite" && (
        <div
          style={{ display: "grid", gridTemplateColumns: "1fr 100px", gap: 12 }}
        >
          <Field label="Host / Server">
            <input
              style={inputSx}
              value={form.host}
              onChange={(e) => set("host", e.target.value)}
              placeholder="erp.janatics.local"
            />
          </Field>
          <Field label="Port">
            <input
              style={inputSx}
              value={form.port}
              onChange={(e) => set("port", e.target.value)}
              placeholder={form.provider === "Oracle" ? "1521" : "1433"}
            />
          </Field>
        </div>
      )}

      <Field
        label={form.provider === "Sqlite" ? "File Path" : "Database / Service"}
      >
        <input
          style={inputSx}
          value={form.database}
          onChange={(e) => set("database", e.target.value)}
          placeholder={form.provider === "Sqlite" ? "/data/logs.db" : "ERPDB"}
        />
      </Field>

      {form.provider !== "Sqlite" && (
        <div
          style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}
        >
          <Field label="Username">
            <input
              style={inputSx}
              value={form.username}
              onChange={(e) => set("username", e.target.value)}
            />
          </Field>
          <Field label="Password">
            <input
              style={inputSx}
              type="password"
              value={form.password || ""}
              onChange={(e) => set("password", e.target.value)}
            />
          </Field>
        </div>
      )}

      <Field
        label="Generated Connection String"
        hint="Auto-built from fields above"
      >
        <div
          style={{
            ...inputSx,
            background: "#f0f4ff",
            color: "#3b4ecc",
            fontSize: 11,
            wordBreak: "break-all",
            lineHeight: 1.6,
          }}
        >
          {connStr}
        </div>
      </Field>

      <div
        style={{
          display: "flex",
          gap: 10,
          justifyContent: "flex-end",
          marginTop: 8,
        }}
      >
        <button
          onClick={onClose}
          style={{
            padding: "9px 20px",
            border: "1.5px solid #e2e8f0",
            borderRadius: 7,
            background: "#fff",
            color: "#64748b",
            cursor: "pointer",
            fontFamily: "'Sora',sans-serif",
            fontSize: 13,
          }}
        >
          Cancel
        </button>
        <button
          onClick={() =>
            onSave({
              ...form,
              connectionString: connStr,
              id: initial?.id || `p${Date.now()}`,
              status: "idle",
              entityCount: 0,
              lastTested: "never",
            })
          }
          style={{
            padding: "9px 22px",
            border: "none",
            borderRadius: 7,
            background: "linear-gradient(135deg,#1e40af,#3b82f6)",
            color: "#fff",
            cursor: "pointer",
            fontFamily: "'Sora',sans-serif",
            fontSize: 13,
            fontWeight: 700,
          }}
        >
          {initial ? "Save Changes" : "Add Profile"}
        </button>
      </div>
    </>
  );
}
