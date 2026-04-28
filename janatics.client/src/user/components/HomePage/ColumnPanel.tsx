import { useState } from "react";
import { Modal, Field, inputSx, selectSx } from "./ui";
import { SEED_COLUMNS, COLUMN_TYPES } from "./data";

export function ColumnPanel({ entityId, entityName, onClose }: any) {
  const [cols, setCols] = useState((SEED_COLUMNS as any)[entityId] || []);
  const [adding, setAdding] = useState(false);
  const [newCol, setNewCol] = useState({
    name: "",
    type: "string",
    required: false,
    isPK: false,
    isFk: false,
    fkRef: "",
  });

  const addCol = () => {
    setCols((p: any) => [...p, { ...newCol, id: `c${Date.now()}` }]);
    setAdding(false);
    setNewCol({
      name: "",
      type: "string",
      required: false,
      isPK: false,
      isFk: false,
      fkRef: "",
    });
  };

  const TYPE_C: Record<string, string> = {
    string: "#0ea5e9",
    number: "#f59e0b",
    decimal: "#8b5cf6",
    date: "#22c55e",
    boolean: "#ef4444",
    guid: "#64748b",
  };

  return (
    <Modal title={`Columns — ${entityName}`} onClose={onClose}>
      <div style={{ marginBottom: 14 }}>
        {cols.map((c: any) => (
          <div
            key={c.id}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 10,
              padding: "9px 12px",
              borderRadius: 7,
              marginBottom: 6,
              background: "#f8faff",
              border: "1px solid #e8f0fb",
            }}
          >
            <span
              style={{
                fontFamily: "'DM Mono',monospace",
                fontSize: 13,
                flex: 1,
                color: "#0f1c3a",
                fontWeight: 600,
              }}
            >
              {c.name}
            </span>
            <span
              style={{
                fontSize: 10,
                color: TYPE_C[c.type] || "#444",
                fontWeight: 700,
                width: 52,
              }}
            >
              {c.type}
            </span>
            {c.isPK && (
              <span
                style={{
                  fontSize: 9,
                  background: "#fef3c7",
                  color: "#92400e",
                  border: "1px solid #fde68a",
                  borderRadius: 4,
                  padding: "1px 5px",
                  fontWeight: 700,
                }}
              >
                PK
              </span>
            )}
            {c.isFk && (
              <span
                style={{
                  fontSize: 9,
                  background: "#ede9fe",
                  color: "#4c1d95",
                  border: "1px solid #ddd6fe",
                  borderRadius: 4,
                  padding: "1px 5px",
                  fontWeight: 700,
                }}
              >
                FK→{c.fkRef}
              </span>
            )}
            {c.required && (
              <span style={{ fontSize: 9, color: "#ef4444", fontWeight: 700 }}>
                REQ
              </span>
            )}
            <button
              onClick={() =>
                setCols((p: any) => p.filter((x: any) => x.id !== c.id))
              }
              style={{
                background: "none",
                border: "none",
                color: "#94a3b8",
                cursor: "pointer",
                fontSize: 14,
                padding: "0 2px",
              }}
            >
              ×
            </button>
          </div>
        ))}
        {cols.length === 0 && (
          <div
            style={{
              textAlign: "center",
              padding: "20px",
              color: "#94a3b8",
              fontSize: 13,
            }}
          >
            No columns registered yet.
          </div>
        )}
      </div>

      {adding ? (
        <div
          style={{
            background: "#f8faff",
            border: "1.5px dashed #c7d2fe",
            borderRadius: 8,
            padding: 14,
            marginBottom: 12,
          }}
        >
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: 10,
              marginBottom: 10,
            }}
          >
            <Field label="Column Name">
              <input
                style={inputSx}
                value={newCol.name}
                onChange={(e) =>
                  setNewCol((p) => ({ ...p, name: e.target.value }))
                }
                placeholder="ColumnName"
              />
            </Field>
            <Field label="Type">
              <select
                style={selectSx}
                value={newCol.type}
                onChange={(e) =>
                  setNewCol((p) => ({ ...p, type: e.target.value }))
                }
              >
                {COLUMN_TYPES.map((t) => (
                  <option key={t}>{t}</option>
                ))}
              </select>
            </Field>
          </div>
          <div style={{ display: "flex", gap: 16, marginBottom: 10 }}>
            {Object.entries({
              isPK: "Primary Key",
              isFk: "Foreign Key",
              required: "Required",
            }).map(([k, l]) => (
              <label
                key={k}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 5,
                  fontSize: 12,
                  cursor: "pointer",
                }}
              >
                <input
                  type="checkbox"
                  checked={(newCol as any)[k]}
                  onChange={(e) =>
                    setNewCol((p) => ({ ...p, [k]: e.target.checked }))
                  }
                />
                {l}
              </label>
            ))}
          </div>
          {newCol.isFk && (
            <Field label="FK Reference (Table.Column)">
              <input
                style={inputSx}
                value={newCol.fkRef}
                onChange={(e) =>
                  setNewCol((p) => ({ ...p, fkRef: e.target.value }))
                }
                placeholder="Vendor.VendorId"
              />
            </Field>
          )}
          <div style={{ display: "flex", gap: 8 }}>
            <button
              onClick={addCol}
              style={{
                padding: "7px 18px",
                background: "#1e40af",
                color: "#fff",
                border: "none",
                borderRadius: 6,
                cursor: "pointer",
                fontSize: 12,
                fontFamily: "'Sora',sans-serif",
                fontWeight: 700,
              }}
            >
              Add Column
            </button>
            <button
              onClick={() => setAdding(false)}
              style={{
                padding: "7px 14px",
                background: "#fff",
                color: "#64748b",
                border: "1px solid #e2e8f0",
                borderRadius: 6,
                cursor: "pointer",
                fontSize: 12,
              }}
            >
              Cancel
            </button>
          </div>
        </div>
      ) : (
        <button
          onClick={() => setAdding(true)}
          style={{
            width: "100%",
            padding: "9px",
            border: "1.5px dashed #c7d2fe",
            borderRadius: 7,
            background: "#f5f7ff",
            color: "#3730a3",
            cursor: "pointer",
            fontSize: 13,
            fontFamily: "'Sora',sans-serif",
            fontWeight: 600,
          }}
        >
          + Add Column
        </button>
      )}
    </Modal>
  );
}
