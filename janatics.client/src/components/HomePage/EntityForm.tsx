import { useState } from "react";
import { Field, inputSx, Tag } from "./ui";

export function EntityForm({ initial, onSave, onClose }: any) {
  const [form, setForm] = useState(
    initial || {
      name: "",
      schema: "",
      pkColumn: "Id",
      isReadOnly: false,
      roles: [],
    },
  );
  const [roleInput, setRoleInput] = useState("");
  const set = (k: string, v: any) => setForm((p: any) => ({ ...p, [k]: v }));
  const addRole = () => {
    if (roleInput && !form.roles.includes(roleInput)) {
      set("roles", [...form.roles, roleInput]);
      setRoleInput("");
    }
  };

  return (
    <>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
        <Field label="Entity Name" hint="Exact DB table name">
          <input
            style={inputSx}
            value={form.name}
            onChange={(e) => set("name", e.target.value)}
            placeholder="PurchaseOrder"
          />
        </Field>
        <Field label="Schema Prefix" hint="e.g. JANATICS, HR, dbo">
          <input
            style={inputSx}
            value={form.schema}
            onChange={(e) => set("schema", e.target.value)}
            placeholder="JANATICS"
          />
        </Field>
      </div>
      <Field label="Primary Key Column">
        <input
          style={inputSx}
          value={form.pkColumn}
          onChange={(e) => set("pkColumn", e.target.value)}
          placeholder="Id"
        />
      </Field>
      <Field label="Read Only">
        <label
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            cursor: "pointer",
          }}
        >
          <input
            type="checkbox"
            checked={form.isReadOnly}
            onChange={(e) => set("isReadOnly", e.target.checked)}
          />
          <span style={{ fontSize: 13, color: "#475569" }}>
            Mark this entity as read-only (no INSERT/UPDATE allowed)
          </span>
        </label>
      </Field>
      <Field label="Allowed Roles">
        <div
          style={{ display: "flex", flexWrap: "wrap", gap: 6, marginBottom: 8 }}
        >
          {form.roles.map((r: string) => (
            <Tag
              key={r}
              label={r}
              onRemove={() =>
                set(
                  "roles",
                  form.roles.filter((x: string) => x !== r),
                )
              }
            />
          ))}
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <input
            style={{ ...inputSx, flex: 1 }}
            value={roleInput}
            onChange={(e) => setRoleInput(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && addRole()}
            placeholder="admin, user, hr …"
          />
          <button
            onClick={addRole}
            style={{
              padding: "9px 14px",
              background: "#eef2ff",
              color: "#3730a3",
              border: "1px solid #c7d2fe",
              borderRadius: 7,
              cursor: "pointer",
              fontSize: 13,
              fontFamily: "'Sora',sans-serif",
            }}
          >
            Add
          </button>
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
              id: initial?.id || `e${Date.now()}`,
              columnCount: initial?.columnCount || 0,
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
          {initial ? "Save Changes" : "Add Entity"}
        </button>
      </div>
    </>
  );
}
