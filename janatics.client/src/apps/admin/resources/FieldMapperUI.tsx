import { useState, useCallback } from "react";

// ── Mock schema data (replace with real API calls to /api/schema/{entity}) ──
const MOCK_SCHEMA = {
  PurchaseOrder: {
    columns: [
      { name: "PONumber", type: "string", required: true, isPK: false },
      { name: "VendorId", type: "number", required: true, isPK: false },
      { name: "Status", type: "string", required: false, isPK: false },
      { name: "OrderDate", type: "date", required: false, isPK: false },
      { name: "TotalAmount", type: "decimal", required: false, isPK: false },
    ],
    children: ["PurchaseOrderLine"],
  },
  PurchaseOrderLine: {
    columns: [
      { name: "ItemCode", type: "string", required: true, isPK: false },
      { name: "Qty", type: "number", required: true, isPK: false },
      { name: "UnitPrice", type: "decimal", required: true, isPK: false },
      { name: "Description", type: "string", required: false, isPK: false },
    ],
    children: [],
  },
  Employee: {
    columns: [
      { name: "EmployeeCode", type: "string", required: true, isPK: false },
      { name: "Name", type: "string", required: true, isPK: false },
      { name: "Department", type: "string", required: false, isPK: false },
      { name: "JoiningDate", type: "date", required: false, isPK: false },
      { name: "Salary", type: "decimal", required: false, isPK: false },
    ],
    children: ["EmployeeAddress"],
  },
  EmployeeAddress: {
    columns: [
      { name: "AddressLine1", type: "string", required: true, isPK: false },
      { name: "City", type: "string", required: true, isPK: false },
      { name: "State", type: "string", required: false, isPK: false },
      { name: "PinCode", type: "string", required: false, isPK: false },
    ],
    children: [],
  },
};

const TYPE_COLORS = {
  string: { bg: "#e8f4f8", text: "#1a6b8a", border: "#90cce0" },
  number: { bg: "#fef3e2", text: "#8a5a00", border: "#f0c060" },
  decimal: { bg: "#f0e8fa", text: "#5a1a8a", border: "#c090e0" },
  date: { bg: "#e8faee", text: "#1a6b3a", border: "#80d0a0" },
};

const ENTITIES = Object.keys(MOCK_SCHEMA);

// ── Sub-components ──────────────────────────────────────────────────────────

function TypeBadge({ type }) {
  const style = TYPE_COLORS[type] || { bg: "#f0f0f0", text: "#444", border: "#ccc" };
  return (
    <span style={{
      fontSize: 10, fontFamily: "'JetBrains Mono', monospace",
      background: style.bg, color: style.text,
      border: `1px solid ${style.border}`,
      padding: "1px 6px", borderRadius: 4, letterSpacing: "0.04em"
    }}>
      {type}
    </span>
  );
}

function ColumnRow({ col, value, onChange, isUpdate }) {
  const inputStyle = {
    flex: 1, padding: "7px 10px", border: "1px solid #d0d8e4",
    borderRadius: 6, fontSize: 13, fontFamily: "'DM Sans', sans-serif",
    background: value ? "#f7faff" : "#fff", outline: "none",
    transition: "border 0.15s",
    color: "#1e2a3a"
  };

  return (
    <div style={{
      display: "flex", alignItems: "center", gap: 10,
      padding: "9px 14px",
      background: value ? "#f9fbff" : "#fff",
      borderBottom: "1px solid #f0f4f8",
      transition: "background 0.15s"
    }}>
      {/* Column name */}
      <div style={{ width: 160, flexShrink: 0 }}>
        <div style={{
          fontSize: 13, fontWeight: 600, color: "#1e2a3a",
          fontFamily: "'DM Sans', sans-serif",
          display: "flex", alignItems: "center", gap: 5
        }}>
          {col.name}
          {col.required && <span style={{ color: "#e05555", fontSize: 10 }}>*</span>}
        </div>
        <TypeBadge type={col.type} />
      </div>

      {/* Arrow */}
      <div style={{ color: value ? "#3b7de8" : "#c0ccd8", fontSize: 16, flexShrink: 0 }}>→</div>

      {/* Value input */}
      <input
        style={inputStyle}
        placeholder={col.required ? `Required — enter ${col.name}` : `Optional`}
        value={value || ""}
        onChange={e => onChange(col.name, e.target.value)}
        type={col.type === "date" ? "date" : col.type === "number" || col.type === "decimal" ? "number" : "text"}
        onFocus={e => e.target.style.borderColor = "#3b7de8"}
        onBlur={e => e.target.style.borderColor = "#d0d8e4"}
      />

      {/* Clear */}
      {value && (
        <button
          onClick={() => onChange(col.name, "")}
          style={{
            background: "none", border: "none", cursor: "pointer",
            color: "#aab4c0", fontSize: 16, padding: "2px 4px", flexShrink: 0
          }}
        >×</button>
      )}
    </div>
  );
}

function ChildSection({ childEntity, fkColumn, onFKChange, childValues, onChildValueChange, onRemove }) {
  const schema = MOCK_SCHEMA[childEntity];
  if (!schema) return null;

  return (
    <div style={{
      border: "1px solid #e0e8f4",
      borderRadius: 10, marginBottom: 12,
      overflow: "hidden",
      boxShadow: "0 1px 4px rgba(60,100,160,0.06)"
    }}>
      {/* Child header */}
      <div style={{
        background: "linear-gradient(90deg, #f0f5ff, #e8f0fb)",
        padding: "10px 14px",
        display: "flex", alignItems: "center", justifyContent: "space-between",
        borderBottom: "1px solid #dde6f4"
      }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{
            background: "#3b7de8", color: "#fff",
            fontSize: 10, padding: "2px 7px", borderRadius: 10,
            fontFamily: "'JetBrains Mono', monospace", fontWeight: 600
          }}>CHILD</span>
          <span style={{ fontWeight: 700, fontSize: 14, color: "#1e2a3a", fontFamily: "'DM Sans', sans-serif" }}>
            {childEntity}
          </span>
        </div>
        <button onClick={onRemove} style={{
          background: "#fff0f0", border: "1px solid #f0c0c0",
          color: "#c05050", borderRadius: 6, cursor: "pointer",
          fontSize: 12, padding: "3px 10px", fontFamily: "'DM Sans', sans-serif"
        }}>Remove</button>
      </div>

      {/* FK config */}
      <div style={{
        padding: "10px 14px", background: "#fffbf0",
        borderBottom: "1px solid #f0e8d0",
        display: "flex", alignItems: "center", gap: 10
      }}>
        <span style={{ fontSize: 12, color: "#8a6020", fontWeight: 600, width: 120, fontFamily: "'DM Sans', sans-serif" }}>
          FK Column
        </span>
        <span style={{ color: "#c0a040", fontSize: 14 }}>→</span>
        <input
          style={{
            padding: "5px 10px", border: "1px solid #e0d0a0",
            borderRadius: 6, fontSize: 12, fontFamily: "'JetBrains Mono', monospace",
            background: "#fffdf5", flex: 1, color: "#5a4010"
          }}
          placeholder="e.g. PurchaseOrderId"
          value={fkColumn}
          onChange={e => onFKChange(e.target.value)}
        />
      </div>

      {/* Child record ID (for update) */}
      <div style={{
        padding: "10px 14px", background: "#f8f8f8",
        borderBottom: "1px solid #eee",
        display: "flex", alignItems: "center", gap: 10
      }}>
        <span style={{ fontSize: 12, color: "#606878", fontWeight: 600, width: 120, fontFamily: "'DM Sans', sans-serif" }}>
          Record ID
        </span>
        <span style={{ color: "#9090a0", fontSize: 14 }}>→</span>
        <input
          style={{
            padding: "5px 10px", border: "1px solid #ddd",
            borderRadius: 6, fontSize: 12, fontFamily: "'JetBrains Mono', monospace",
            background: "#fff", flex: 1, color: "#333"
          }}
          placeholder="Leave empty for INSERT"
          value={childValues.__recordId || ""}
          onChange={e => onChildValueChange("__recordId", e.target.value)}
        />
      </div>

      {/* Child columns */}
      {schema.columns.map(col => (
        <ColumnRow
          key={col.name}
          col={col}
          value={childValues[col.name]}
          onChange={onChildValueChange}
        />
      ))}
    </div>
  );
}

// ── Payload Preview ──────────────────────────────────────────────────────────

function buildPayload(entity, transactionId, fieldValues, children) {
  const extendedProperties = {};
  Object.entries(fieldValues).forEach(([k, v]) => {
    if (v !== "" && v !== undefined) extendedProperties[k] = v;
  });

  const relProps = children.map(c => {
    const props = {};
    Object.entries(c.values).forEach(([k, v]) => {
      if (k !== "__recordId" && v !== "" && v !== undefined) props[k] = v;
    });
    return {
      entityName: c.entity,
      foreignKeyColumn: c.fkColumn,
      recordId: c.values.__recordId || null,
      properties: props
    };
  });

  return {
    transactionEntityName: entity,
    transactionId: transactionId || null,
    extendedProperties,
    relProps
  };
}

// ── Main Component ───────────────────────────────────────────────────────────

export default function FieldMapperUI() {
  const [selectedEntity, setSelectedEntity] = useState("");
  const [transactionId, setTransactionId] = useState("");
  const [isUpdate, setIsUpdate] = useState(false);
  const [fieldValues, setFieldValues] = useState({});
  const [children, setChildren] = useState([]);
  const [showPayload, setShowPayload] = useState(false);
  const [submitState, setSubmitState] = useState(null); // null | "loading" | "success" | "error"
  const [activeTab, setActiveTab] = useState("mapper"); // "mapper" | "preview"

  const schema = selectedEntity ? MOCK_SCHEMA[selectedEntity] : null;

  const handleEntityChange = (entity) => {
    setSelectedEntity(entity);
    setFieldValues({});
    setChildren([]);
    setTransactionId("");
    setSubmitState(null);
  };

  const handleFieldChange = useCallback((col, val) => {
    setFieldValues(prev => ({ ...prev, [col]: val }));
  }, []);

  const addChild = (childEntity) => {
    setChildren(prev => [...prev, {
      id: Date.now(),
      entity: childEntity,
      fkColumn: `${selectedEntity}Id`,
      values: {}
    }]);
  };

  const removeChild = (id) => {
    setChildren(prev => prev.filter(c => c.id !== id));
  };

  const updateChildFK = (id, fk) => {
    setChildren(prev => prev.map(c => c.id === id ? { ...c, fkColumn: fk } : c));
  };

  const updateChildValue = (id, col, val) => {
    setChildren(prev => prev.map(c =>
      c.id === id ? { ...c, values: { ...c.values, [col]: val } } : c
    ));
  };

  const payload = selectedEntity
    ? buildPayload(selectedEntity, isUpdate ? transactionId : null, fieldValues, children)
    : null;

  // Validation
  const missingRequired = schema
    ? schema.columns.filter(col => col.required && !fieldValues[col.name])
    : [];
  const isValid = missingRequired.length === 0 && selectedEntity;

  const handleSubmit = async () => {
    if (!isValid) return;
    setSubmitState("loading");
    await new Promise(r => setTimeout(r, 1500));
    setSubmitState("success");
    setTimeout(() => setSubmitState(null), 3000);
  };

  // ── Styles ─────────────────────────────────────────────────────────────────
  const styles = {
    root: {
      minHeight: "100vh",
      background: "linear-gradient(135deg, #f0f4ff 0%, #e8f0fb 50%, #f4f0ff 100%)",
      fontFamily: "'DM Sans', sans-serif",
      padding: "28px 20px"
    },
    card: {
      maxWidth: 920, margin: "0 auto",
      background: "#fff",
      borderRadius: 16,
      boxShadow: "0 8px 40px rgba(40,70,140,0.10)",
      overflow: "hidden"
    },
    header: {
      background: "linear-gradient(135deg, #1a2a4a 0%, #2a3d6e 100%)",
      padding: "24px 28px",
      display: "flex", alignItems: "center", justifyContent: "space-between"
    },
    tabBar: {
      display: "flex", borderBottom: "2px solid #f0f4f8",
      background: "#fafbff"
    },
    tab: (active) => ({
      padding: "13px 22px",
      fontSize: 13, fontWeight: active ? 700 : 500,
      color: active ? "#3b7de8" : "#7a8898",
      borderBottom: active ? "2px solid #3b7de8" : "2px solid transparent",
      cursor: "pointer", background: "none", border: "none",
      fontFamily: "'DM Sans', sans-serif",
      marginBottom: -2, transition: "all 0.15s"
    }),
    section: {
      padding: "20px 28px"
    },
    label: {
      fontSize: 11, fontWeight: 700, color: "#7a8898",
      letterSpacing: "0.08em", textTransform: "uppercase",
      marginBottom: 8, display: "block"
    },
    select: {
      width: "100%", padding: "10px 14px",
      border: "1.5px solid #d0d8e8", borderRadius: 8,
      fontSize: 14, color: "#1e2a3a",
      background: "#f7faff", cursor: "pointer",
      fontFamily: "'DM Sans', sans-serif", outline: "none"
    },
    toggleBtn: (active) => ({
      padding: "7px 16px", borderRadius: 6,
      border: `1.5px solid ${active ? "#3b7de8" : "#d0d8e8"}`,
      background: active ? "#3b7de8" : "#fff",
      color: active ? "#fff" : "#6a7888",
      fontSize: 13, cursor: "pointer", fontWeight: active ? 600 : 400,
      fontFamily: "'DM Sans', sans-serif", transition: "all 0.15s"
    }),
    submitBtn: {
      padding: "11px 28px",
      background: isValid ? "linear-gradient(135deg, #3b7de8, #2060c8)" : "#c0ccd8",
      color: "#fff", border: "none", borderRadius: 8,
      fontSize: 14, fontWeight: 700, cursor: isValid ? "pointer" : "not-allowed",
      fontFamily: "'DM Sans', sans-serif",
      boxShadow: isValid ? "0 4px 16px rgba(59,125,232,0.3)" : "none",
      transition: "all 0.2s"
    }
  };

  return (
    <div style={styles.root}>
      {/* Google Fonts */}
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=JetBrains+Mono:wght@400;600&display=swap');
        * { box-sizing: border-box; }
        input:focus { outline: none; }
      `}</style>

      <div style={styles.card}>

        {/* Header */}
        <div style={styles.header}>
          <div>
            <div style={{
              fontSize: 20, fontWeight: 700, color: "#fff",
              letterSpacing: "-0.02em", fontFamily: "'DM Sans', sans-serif"
            }}>
              ⚡ Transaction Field Mapper
            </div>
            <div style={{ fontSize: 12, color: "#8090b8", marginTop: 3 }}>
              Map entity fields → generate safe transaction payload
            </div>
          </div>
          {selectedEntity && (
            <div style={{
              background: "rgba(255,255,255,0.08)",
              border: "1px solid rgba(255,255,255,0.15)",
              borderRadius: 8, padding: "6px 14px",
              fontSize: 12, color: "#a0b8e8",
              fontFamily: "'JetBrains Mono', monospace"
            }}>
              {isUpdate ? "UPDATE" : "INSERT"} → {selectedEntity}
            </div>
          )}
        </div>

        {/* Tab bar */}
        <div style={styles.tabBar}>
          {["mapper", "preview"].map(tab => (
            <button key={tab} style={styles.tab(activeTab === tab)} onClick={() => setActiveTab(tab)}>
              {tab === "mapper" ? "🗂 Field Mapper" : "📋 Payload Preview"}
            </button>
          ))}
        </div>

        {/* ── MAPPER TAB ── */}
        {activeTab === "mapper" && (
          <>
            {/* Entity + mode selection */}
            <div style={{ ...styles.section, borderBottom: "1px solid #f0f4f8" }}>
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}>

                {/* Entity selector */}
                <div>
                  <label style={styles.label}>Entity (Table)</label>
                  <select
                    style={styles.select}
                    value={selectedEntity}
                    onChange={e => handleEntityChange(e.target.value)}
                  >
                    <option value="">— Select entity —</option>
                    {ENTITIES.map(e => <option key={e} value={e}>{e}</option>)}
                  </select>
                </div>

                {/* Insert / Update toggle */}
                <div>
                  <label style={styles.label}>Operation Mode</label>
                  <div style={{ display: "flex", gap: 8 }}>
                    <button style={styles.toggleBtn(!isUpdate)} onClick={() => setIsUpdate(false)}>
                      ＋ INSERT
                    </button>
                    <button style={styles.toggleBtn(isUpdate)} onClick={() => setIsUpdate(true)}>
                      ✎ UPDATE
                    </button>
                  </div>
                </div>
              </div>

              {/* Transaction ID for update */}
              {isUpdate && (
                <div style={{ marginTop: 16 }}>
                  <label style={styles.label}>Transaction ID (existing record)</label>
                  <input
                    style={{
                      ...styles.select, fontFamily: "'JetBrains Mono', monospace",
                      borderColor: transactionId ? "#3b7de8" : "#f0a030"
                    }}
                    placeholder="Enter the ID of the record to update"
                    value={transactionId}
                    onChange={e => setTransactionId(e.target.value)}
                  />
                  {!transactionId && (
                    <div style={{ fontSize: 11, color: "#e08030", marginTop: 4 }}>
                      ⚠ Transaction ID is required for UPDATE mode
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Field mapper */}
            {schema && (
              <>
                <div style={{ padding: "16px 28px 8px", borderBottom: "1px solid #f0f4f8" }}>
                  <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                    <div>
                      <span style={styles.label}>Main Fields — {selectedEntity}</span>
                      <span style={{ fontSize: 12, color: "#7a8898" }}>
                        {Object.values(fieldValues).filter(Boolean).length} of {schema.columns.length} mapped
                        {missingRequired.length > 0 && (
                          <span style={{ color: "#e05555", marginLeft: 8 }}>
                            · {missingRequired.length} required field{missingRequired.length > 1 ? "s" : ""} missing
                          </span>
                        )}
                      </span>
                    </div>
                    {/* Progress bar */}
                    <div style={{
                      width: 120, height: 6, background: "#f0f4f8",
                      borderRadius: 4, overflow: "hidden"
                    }}>
                      <div style={{
                        height: "100%", borderRadius: 4,
                        width: `${(Object.values(fieldValues).filter(Boolean).length / schema.columns.length) * 100}%`,
                        background: "linear-gradient(90deg, #3b7de8, #6a40e8)",
                        transition: "width 0.3s"
                      }} />
                    </div>
                  </div>
                </div>

                <div>
                  {schema.columns.map(col => (
                    <ColumnRow
                      key={col.name}
                      col={col}
                      value={fieldValues[col.name]}
                      onChange={handleFieldChange}
                      isUpdate={isUpdate}
                    />
                  ))}
                </div>

                {/* Children section */}
                {schema.children.length > 0 && (
                  <div style={{ ...styles.section, borderTop: "2px solid #f0f4f8" }}>
                    <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 14 }}>
                      <span style={styles.label}>Related Child Tables</span>
                      <div style={{ display: "flex", gap: 8 }}>
                        {schema.children.map(child => (
                          <button
                            key={child}
                            onClick={() => addChild(child)}
                            style={{
                              padding: "6px 14px",
                              background: "#f0f5ff", color: "#3b7de8",
                              border: "1.5px solid #c0d8f8", borderRadius: 6,
                              fontSize: 12, cursor: "pointer", fontWeight: 600,
                              fontFamily: "'DM Sans', sans-serif"
                            }}
                          >
                            + {child}
                          </button>
                        ))}
                      </div>
                    </div>

                    {children.length === 0 && (
                      <div style={{
                        textAlign: "center", padding: "24px",
                        color: "#9aa8b8", fontSize: 13,
                        background: "#fafbff", borderRadius: 8,
                        border: "1.5px dashed #d8e4f4"
                      }}>
                        No child records added. Click a button above to add related records.
                      </div>
                    )}

                    {children.map(c => (
                      <ChildSection
                        key={c.id}
                        childEntity={c.entity}
                        fkColumn={c.fkColumn}
                        onFKChange={fk => updateChildFK(c.id, fk)}
                        childValues={c.values}
                        onChildValueChange={(col, val) => updateChildValue(c.id, col, val)}
                        onRemove={() => removeChild(c.id)}
                      />
                    ))}
                  </div>
                )}

                {/* Submit bar */}
                <div style={{
                  padding: "16px 28px",
                  borderTop: "2px solid #f0f4f8",
                  background: "#fafbff",
                  display: "flex", alignItems: "center", justifyContent: "space-between"
                }}>
                  <div>
                    {!isValid && selectedEntity && (
                      <div style={{ fontSize: 12, color: "#e05555" }}>
                        ⚠ Fill all required fields to submit
                      </div>
                    )}
                    {submitState === "success" && (
                      <div style={{ fontSize: 13, color: "#28a860", fontWeight: 600 }}>
                        ✓ Transaction submitted successfully!
                      </div>
                    )}
                  </div>
                  <div style={{ display: "flex", gap: 10 }}>
                    <button
                      style={{
                        padding: "11px 20px",
                        background: "#fff", color: "#3b7de8",
                        border: "1.5px solid #c0d0f0", borderRadius: 8,
                        fontSize: 13, cursor: "pointer",
                        fontFamily: "'DM Sans', sans-serif", fontWeight: 600
                      }}
                      onClick={() => setActiveTab("preview")}
                    >
                      Preview Payload →
                    </button>
                    <button
                      style={styles.submitBtn}
                      onClick={handleSubmit}
                      disabled={!isValid}
                    >
                      {submitState === "loading" ? "⏳ Submitting…" : `${isUpdate ? "Update" : "Create"} Record`}
                    </button>
                  </div>
                </div>
              </>
            )}

            {/* Empty state */}
            {!selectedEntity && (
              <div style={{
                padding: "60px 28px", textAlign: "center", color: "#9aa8b8"
              }}>
                <div style={{ fontSize: 40, marginBottom: 12 }}>🗂</div>
                <div style={{ fontSize: 16, fontWeight: 600, color: "#5a6878", marginBottom: 6 }}>
                  Select an entity to start mapping
                </div>
                <div style={{ fontSize: 13 }}>
                  Choose a table above to see its columns and begin field mapping
                </div>
              </div>
            )}
          </>
        )}

        {/* ── PREVIEW TAB ── */}
        {activeTab === "preview" && (
          <div style={styles.section}>
            {payload ? (
              <>
                <div style={{
                  display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 14
                }}>
                  <label style={styles.label}>Generated Payload</label>
                  <button
                    onClick={() => navigator.clipboard?.writeText(JSON.stringify(payload, null, 2))}
                    style={{
                      padding: "5px 14px",
                      background: "#f0f5ff", color: "#3b7de8",
                      border: "1px solid #c0d8f8", borderRadius: 6,
                      fontSize: 12, cursor: "pointer",
                      fontFamily: "'DM Sans', sans-serif", fontWeight: 600
                    }}
                  >
                    Copy JSON
                  </button>
                </div>

                <pre style={{
                  background: "#1a2035", color: "#a8d0f8",
                  padding: 20, borderRadius: 10, overflow: "auto",
                  fontSize: 12.5, lineHeight: 1.7,
                  fontFamily: "'JetBrains Mono', monospace",
                  maxHeight: 480, border: "1px solid #2a3555"
                }}>
                  {JSON.stringify(payload, null, 2)}
                </pre>

                {/* Validation summary */}
                <div style={{ marginTop: 16 }}>
                  <label style={styles.label}>Validation</label>
                  {missingRequired.length === 0 ? (
                    <div style={{
                      padding: "10px 14px", background: "#e8faf0",
                      border: "1px solid #80d0a0", borderRadius: 8,
                      color: "#1a6b3a", fontSize: 13, fontWeight: 600
                    }}>
                      ✓ All required fields are mapped. Payload is ready to submit.
                    </div>
                  ) : (
                    <div style={{
                      padding: "10px 14px", background: "#fff0f0",
                      border: "1px solid #f0a0a0", borderRadius: 8,
                      color: "#801a1a", fontSize: 13
                    }}>
                      <strong>Missing required fields:</strong>{" "}
                      {missingRequired.map(c => c.name).join(", ")}
                    </div>
                  )}
                </div>
              </>
            ) : (
              <div style={{ textAlign: "center", padding: "60px", color: "#9aa8b8" }}>
                <div style={{ fontSize: 13 }}>Select an entity and map fields first.</div>
              </div>
            )}
          </div>
        )}

      </div>
    </div>
  );
}