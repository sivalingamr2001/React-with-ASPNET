import { useState } from "react";
import {
  SEED_PROFILES,
  SEED_ENTITIES,
  SEED_COLUMNS,
  DISCOVERED_TABLES,
} from "../components/HomePage/data";
import {
  ProviderBadge,
  StatusDot,
  Tag,
  Modal,
} from "../components/HomePage/ui";
import { ProfileForm } from "../components/HomePage/ProfileForm";
import { EntityForm } from "../components/HomePage/EntityForm";
import { ColumnPanel } from "../components/HomePage/ColumnPanel";
// ── Main Page ─────────────────────────────────────────────────────────────────
export default function RegistryManager() {
  const [profiles, setProfiles] = useState(SEED_PROFILES);
  const [entities, setEntities] = useState(SEED_ENTITIES);
  const [activeProfile, setActiveProfile] = useState("p1");
  const [search, setSearch] = useState("");
  const [modal, setModal] = useState(null); // null | { type, data }
  const [testingId, setTestingId] = useState(null);
  const [discovering, setDiscovering] = useState(false);
  const [discovered, setDiscovered] = useState([]);
  const [expandedEntity, setExpandedEntity] = useState(null);

  const closeModal = () => setModal(null);

  const saveProfile = (p) => {
    setProfiles((prev) =>
      prev.find((x) => x.id === p.id)
        ? prev.map((x) => (x.id === p.id ? p : x))
        : [...prev, p],
    );
    closeModal();
  };

  const deleteProfile = (id) => {
    setProfiles((p) => p.filter((x) => x.id !== id));
    const { [id]: _, ...rest } = entities;
    setEntities(rest);
    if (activeProfile === id) setActiveProfile(profiles[0]?.id || "");
  };

  const testConnection = async (id) => {
    setTestingId(id);
    await new Promise((r) => setTimeout(r, 1800));
    setProfiles((p) =>
      p.map((x) =>
        x.id === id ? { ...x, status: "connected", lastTested: "just now" } : x,
      ),
    );
    setTestingId(null);
  };

  const discoverTables = async () => {
    setDiscovering(true);
    await new Promise((r) => setTimeout(r, 2000));
    setDiscovered(
      DISCOVERED_TABLES.filter(
        (t) => !(entities[activeProfile] || []).some((e) => e.name === t),
      ),
    );
    setDiscovering(false);
  };

  const importDiscovered = (name) => {
    const newEntity = {
      id: `e${Date.now()}`,
      name,
      schema: "JANATICS",
      pkColumn: "Id",
      isReadOnly: false,
      roles: ["admin"],
      columnCount: 0,
    };
    setEntities((p) => ({
      ...p,
      [activeProfile]: [...(p[activeProfile] || []), newEntity],
    }));
    setDiscovered((p) => p.filter((t) => t !== name));
  };

  const saveEntity = (profileId, entity) => {
    setEntities((p) => {
      const list = p[profileId] || [];
      return {
        ...p,
        [profileId]: list.find((x) => x.id === entity.id)
          ? list.map((x) => (x.id === entity.id ? entity : x))
          : [...list, entity],
      };
    });
    closeModal();
  };

  const deleteEntity = (profileId, entityId) => {
    setEntities((p) => ({
      ...p,
      [profileId]: (p[profileId] || []).filter((x) => x.id !== entityId),
    }));
  };

  const filteredEntities = (entities[activeProfile] || []).filter(
    (e) =>
      e.name.toLowerCase().includes(search.toLowerCase()) ||
      e.schema.toLowerCase().includes(search.toLowerCase()),
  );

  const activeProf = profiles.find((p) => p.id === activeProfile);

  // ── Styles ────────────────────────────────────────────────────────────────
  const S = {
    root: {
      minHeight: "100vh",
      display: "flex",
      flexDirection: "column",
      background: "#0a0f1e",
      fontFamily: "'Sora', sans-serif",
    },
    topBar: {
      background: "linear-gradient(90deg,#0d1b3e,#112040)",
      borderBottom: "1px solid #1e3060",
      padding: "0 28px",
      display: "flex",
      alignItems: "center",
      gap: 20,
      height: 56,
    },
    body: {
      display: "flex",
      flex: 1,
      overflow: "hidden",
      height: "calc(100vh - 56px)",
    },
    sidebar: {
      width: 260,
      background: "#080d1c",
      borderRight: "1px solid #1a2a50",
      display: "flex",
      flexDirection: "column",
      overflowY: "auto",
    },
    main: { flex: 1, overflowY: "auto", padding: 28, background: "#0d1226" },
    sideItem: (active) => ({
      padding: "12px 16px",
      cursor: "pointer",
      background: active
        ? "linear-gradient(90deg,#1e3a8a20,#3b82f620)"
        : "transparent",
      borderLeft: active ? "3px solid #3b82f6" : "3px solid transparent",
      transition: "all 0.15s",
    }),
    entityCard: {
      background: "#111827",
      border: "1px solid #1f2d4a",
      borderRadius: 10,
      marginBottom: 10,
      overflow: "hidden",
      transition: "border 0.15s",
    },
    btn: (variant) => ({
      padding: "7px 16px",
      borderRadius: 6,
      cursor: "pointer",
      fontSize: 12,
      fontWeight: 600,
      fontFamily: "'Sora',sans-serif",
      border: "none",
      transition: "all 0.15s",
      ...(variant === "primary"
        ? {
            background: "linear-gradient(135deg,#1e40af,#3b82f6)",
            color: "#fff",
          }
        : variant === "ghost"
          ? {
              background: "transparent",
              color: "#64748b",
              border: "1px solid #1f2d4a",
            }
          : variant === "danger"
            ? {
                background: "#7f1d1d20",
                color: "#fca5a5",
                border: "1px solid #7f1d1d",
              }
            : variant === "success"
              ? {
                  background: "#14532d20",
                  color: "#86efac",
                  border: "1px solid #14532d",
                }
              : {
                  background: "#1e293b",
                  color: "#94a3b8",
                  border: "1px solid #1f2d4a",
                }),
    }),
  };

  return (
    <div style={S.root}>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Sora:wght@400;600;700&family=DM+Mono:wght@400;600&family=JetBrains+Mono:wght@400;600&display=swap');
        * { box-sizing: border-box; margin: 0; }
        ::-webkit-scrollbar { width: 5px; } 
        ::-webkit-scrollbar-track { background: #080d1c; }
        ::-webkit-scrollbar-thumb { background: #1e3060; border-radius: 4px; }
        input, select { transition: border 0.15s; }
        input:focus, select:focus { border-color: #3b82f6 !important; outline: none; }
      `}</style>

      {/* Top Bar */}
      <div style={S.topBar}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div
            style={{
              width: 28,
              height: 28,
              background: "linear-gradient(135deg,#3b82f6,#6366f1)",
              borderRadius: 6,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 14,
            }}
          >
            🗄
          </div>
          <span
            style={{
              fontWeight: 700,
              fontSize: 15,
              color: "#e2e8f0",
              letterSpacing: "-0.01em",
            }}
          >
            DB Registry Manager
          </span>
          <span
            style={{
              fontSize: 10,
              background: "#1e3a8a",
              color: "#93c5fd",
              padding: "2px 8px",
              borderRadius: 10,
              fontWeight: 700,
              border: "1px solid #1d4ed8",
            }}
          >
            ADMIN
          </span>
        </div>
        <div style={{ flex: 1 }} />
        <div style={{ fontSize: 11, color: "#475569" }}>
          {profiles.length} profiles · {Object.values(entities).flat().length}{" "}
          entities registered
        </div>
      </div>

      <div style={S.body}>
        {/* Sidebar — DB Profiles */}
        <div style={S.sidebar}>
          <div
            style={{
              padding: "14px 16px 8px",
              borderBottom: "1px solid #1a2a50",
            }}
          >
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
              }}
            >
              <span
                style={{
                  fontSize: 10,
                  fontWeight: 700,
                  color: "#475569",
                  letterSpacing: "0.1em",
                  textTransform: "uppercase",
                }}
              >
                DB Profiles
              </span>
              <button
                onClick={() => setModal({ type: "addProfile" })}
                style={{
                  background: "#1e3a8a",
                  color: "#93c5fd",
                  border: "none",
                  borderRadius: 5,
                  padding: "3px 9px",
                  fontSize: 11,
                  cursor: "pointer",
                  fontWeight: 700,
                }}
              >
                + New
              </button>
            </div>
          </div>

          {profiles.map((p) => (
            <div
              key={p.id}
              style={S.sideItem(activeProfile === p.id)}
              onClick={() => setActiveProfile(p.id)}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  marginBottom: 5,
                }}
              >
                <span
                  style={{
                    fontWeight: 700,
                    fontSize: 13,
                    color: activeProfile === p.id ? "#93c5fd" : "#cbd5e1",
                  }}
                >
                  {p.name}
                </span>
                <StatusDot status={p.status} />
              </div>
              <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                <ProviderBadge provider={p.provider} />
                <span style={{ fontSize: 10, color: "#475569" }}>
                  {(entities[p.id] || []).length} tables
                </span>
              </div>
            </div>
          ))}
        </div>

        {/* Main content */}
        <div style={S.main}>
          {activeProf && (
            <>
              {/* Profile header card */}
              <div
                style={{
                  background: "linear-gradient(135deg,#111827,#1a2a4a)",
                  border: "1px solid #1f3060",
                  borderRadius: 12,
                  padding: "18px 22px",
                  marginBottom: 20,
                  display: "flex",
                  alignItems: "flex-start",
                  justifyContent: "space-between",
                }}
              >
                <div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: 10,
                      marginBottom: 6,
                    }}
                  >
                    <span
                      style={{
                        fontWeight: 700,
                        fontSize: 18,
                        color: "#e2e8f0",
                      }}
                    >
                      {activeProf.name}
                    </span>
                    <ProviderBadge provider={activeProf.provider} />
                    <StatusDot status={activeProf.status} />
                  </div>
                  <div
                    style={{
                      fontFamily: "'DM Mono',monospace",
                      fontSize: 11,
                      color: "#475569",
                      background: "#0a0f1e",
                      padding: "5px 10px",
                      borderRadius: 5,
                      border: "1px solid #1a2a50",
                      maxWidth: 480,
                      wordBreak: "break-all",
                    }}
                  >
                    {activeProf.connectionString}
                  </div>
                  <div style={{ fontSize: 11, color: "#334155", marginTop: 6 }}>
                    Last tested: {activeProf.lastTested}
                  </div>
                </div>
                <div style={{ display: "flex", gap: 8, flexShrink: 0 }}>
                  <button
                    style={S.btn("success")}
                    onClick={() => testConnection(activeProf.id)}
                    disabled={testingId === activeProf.id}
                  >
                    {testingId === activeProf.id
                      ? "⏳ Testing…"
                      : "⚡ Test Connection"}
                  </button>
                  <button
                    style={S.btn("ghost")}
                    onClick={() =>
                      setModal({ type: "editProfile", data: activeProf })
                    }
                  >
                    Edit
                  </button>
                  <button
                    style={S.btn("danger")}
                    onClick={() => deleteProfile(activeProf.id)}
                  >
                    Delete
                  </button>
                </div>
              </div>

              {/* Entity list header */}
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 12,
                  marginBottom: 16,
                }}
              >
                <div style={{ flex: 1, position: "relative" }}>
                  <span
                    style={{
                      position: "absolute",
                      left: 11,
                      top: "50%",
                      transform: "translateY(-50%)",
                      color: "#475569",
                      fontSize: 13,
                    }}
                  >
                    🔍
                  </span>
                  <input
                    style={{
                      width: "100%",
                      padding: "8px 12px 8px 32px",
                      background: "#111827",
                      border: "1px solid #1f2d4a",
                      borderRadius: 7,
                      color: "#e2e8f0",
                      fontSize: 13,
                      fontFamily: "'Sora',sans-serif",
                    }}
                    placeholder="Search entities…"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                </div>
                <button
                  style={S.btn()}
                  onClick={discoverTables}
                  disabled={discovering}
                >
                  {discovering ? "⏳ Discovering…" : "🔭 Auto-Discover"}
                </button>
                <button
                  style={S.btn("primary")}
                  onClick={() =>
                    setModal({ type: "addEntity", profileId: activeProfile })
                  }
                >
                  + Add Entity
                </button>
              </div>

              {/* Discovered tables banner */}
              {discovered.length > 0 && (
                <div
                  style={{
                    background: "#0c1a0a",
                    border: "1px solid #14532d",
                    borderRadius: 8,
                    padding: "12px 16px",
                    marginBottom: 16,
                  }}
                >
                  <div
                    style={{
                      fontSize: 12,
                      color: "#86efac",
                      fontWeight: 700,
                      marginBottom: 8,
                    }}
                  >
                    🔭 {discovered.length} unregistered tables found in DB
                  </div>
                  <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
                    {discovered.map((t) => (
                      <div
                        key={t}
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: 6,
                        }}
                      >
                        <span
                          style={{
                            fontFamily: "'DM Mono',monospace",
                            fontSize: 11,
                            color: "#4ade80",
                            background: "#052e16",
                            padding: "3px 8px",
                            borderRadius: 4,
                            border: "1px solid #14532d",
                          }}
                        >
                          {t}
                        </span>
                        <button
                          onClick={() => importDiscovered(t)}
                          style={{
                            fontSize: 11,
                            background: "#14532d",
                            color: "#86efac",
                            border: "none",
                            borderRadius: 4,
                            padding: "3px 8px",
                            cursor: "pointer",
                            fontWeight: 600,
                          }}
                        >
                          Import
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Entity cards */}
              {filteredEntities.length === 0 ? (
                <div
                  style={{
                    textAlign: "center",
                    padding: "50px",
                    color: "#334155",
                  }}
                >
                  <div style={{ fontSize: 32, marginBottom: 10 }}>🗂</div>
                  <div style={{ fontSize: 14 }}>
                    No entities registered for this profile.
                  </div>
                </div>
              ) : (
                filteredEntities.map((entity) => (
                  <div key={entity.id} style={S.entityCard}>
                    {/* Entity row */}
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: 14,
                        padding: "13px 16px",
                        cursor: "pointer",
                      }}
                      onClick={() =>
                        setExpandedEntity(
                          expandedEntity === entity.id ? null : entity.id,
                        )
                      }
                    >
                      {/* Expand chevron */}
                      <span
                        style={{ color: "#334155", fontSize: 11, width: 12 }}
                      >
                        {expandedEntity === entity.id ? "▼" : "▶"}
                      </span>

                      {/* Name + schema */}
                      <div style={{ flex: 1 }}>
                        <div
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: 8,
                          }}
                        >
                          <span
                            style={{
                              fontFamily: "'DM Mono',monospace",
                              fontWeight: 600,
                              fontSize: 14,
                              color: "#93c5fd",
                            }}
                          >
                            {entity.name}
                          </span>
                          <span style={{ fontSize: 10, color: "#475569" }}>
                            {entity.schema}.{entity.name}
                          </span>
                          {entity.isReadOnly && (
                            <span
                              style={{
                                fontSize: 9,
                                background: "#3f1a0020",
                                color: "#fbbf24",
                                border: "1px solid #78350f",
                                padding: "1px 6px",
                                borderRadius: 4,
                                fontWeight: 700,
                              }}
                            >
                              READ-ONLY
                            </span>
                          )}
                        </div>
                        <div
                          style={{
                            display: "flex",
                            gap: 6,
                            marginTop: 5,
                            flexWrap: "wrap",
                          }}
                        >
                          <span style={{ fontSize: 10, color: "#475569" }}>
                            PK:{" "}
                            <span
                              style={{
                                color: "#fbbf24",
                                fontFamily: "'DM Mono',monospace",
                              }}
                            >
                              {entity.pkColumn}
                            </span>
                          </span>
                          <span style={{ fontSize: 10, color: "#334155" }}>
                            ·
                          </span>
                          <span style={{ fontSize: 10, color: "#475569" }}>
                            {entity.columnCount} columns
                          </span>
                          <span style={{ fontSize: 10, color: "#334155" }}>
                            ·
                          </span>
                          {entity.roles.map((r) => (
                            <Tag key={r} label={r} />
                          ))}
                        </div>
                      </div>

                      {/* Actions */}
                      <div
                        style={{ display: "flex", gap: 6 }}
                        onClick={(e) => e.stopPropagation()}
                      >
                        <button
                          style={S.btn()}
                          onClick={() =>
                            setExpandedEntity(entity.id) ||
                            setModal({
                              type: "columns",
                              entityId: entity.id,
                              entityName: entity.name,
                            })
                          }
                        >
                          Columns
                        </button>
                        <button
                          style={S.btn("ghost")}
                          onClick={() =>
                            setModal({
                              type: "editEntity",
                              profileId: activeProfile,
                              data: entity,
                            })
                          }
                        >
                          Edit
                        </button>
                        <button
                          style={S.btn("danger")}
                          onClick={() => deleteEntity(activeProfile, entity.id)}
                        >
                          ×
                        </button>
                      </div>
                    </div>

                    {/* Expanded column preview */}
                    {expandedEntity === entity.id &&
                      (SEED_COLUMNS[entity.id] || []).length > 0 && (
                        <div
                          style={{
                            borderTop: "1px solid #1f2d4a",
                            padding: "10px 16px 12px",
                            background: "#0a0f1e",
                          }}
                        >
                          <div
                            style={{
                              fontSize: 10,
                              color: "#334155",
                              fontWeight: 700,
                              letterSpacing: "0.08em",
                              marginBottom: 8,
                            }}
                          >
                            REGISTERED COLUMNS
                          </div>
                          <div
                            style={{
                              display: "flex",
                              flexWrap: "wrap",
                              gap: 6,
                            }}
                          >
                            {(SEED_COLUMNS[entity.id] || []).map((c) => (
                              <span
                                key={c.id}
                                style={{
                                  fontFamily: "'DM Mono',monospace",
                                  fontSize: 11,
                                  background: "#111827",
                                  color: c.isPK
                                    ? "#fbbf24"
                                    : c.isFk
                                      ? "#c4b5fd"
                                      : "#64748b",
                                  border: `1px solid ${c.isPK ? "#78350f" : c.isFk ? "#4c1d95" : "#1f2d4a"}`,
                                  padding: "2px 8px",
                                  borderRadius: 4,
                                }}
                              >
                                {c.name}: {c.type}
                                {c.isPK && " 🔑"}
                                {c.isFk && ` →${c.fkRef}`}
                              </span>
                            ))}
                          </div>
                        </div>
                      )}
                  </div>
                ))
              )}
            </>
          )}
        </div>
      </div>

      {/* Modals */}
      {modal?.type === "addProfile" && (
        <Modal title="Add DB Profile" onClose={closeModal}>
          <ProfileForm onSave={saveProfile} onClose={closeModal} />
        </Modal>
      )}
      {modal?.type === "editProfile" && (
        <Modal title="Edit DB Profile" onClose={closeModal}>
          <ProfileForm
            initial={modal.data}
            onSave={saveProfile}
            onClose={closeModal}
          />
        </Modal>
      )}
      {modal?.type === "addEntity" && (
        <Modal title="Register Entity" onClose={closeModal}>
          <EntityForm
            onSave={(e) => saveEntity(modal.profileId, e)}
            onClose={closeModal}
          />
        </Modal>
      )}
      {modal?.type === "editEntity" && (
        <Modal title="Edit Entity" onClose={closeModal}>
          <EntityForm
            initial={modal.data}
            onSave={(e) => saveEntity(modal.profileId, e)}
            onClose={closeModal}
          />
        </Modal>
      )}
      {modal?.type === "columns" && (
        <ColumnPanel
          entityId={modal.entityId}
          entityName={modal.entityName}
          onClose={closeModal}
        />
      )}
    </div>
  );
}
