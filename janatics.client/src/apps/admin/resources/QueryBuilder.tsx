import { useState, useCallback } from "react";

// ── Mock registry (comes from your RegistryManager DB in real impl) ──────────
const REGISTRY = {
  PurchaseOrder: {
    schema: "JANATICS", pk: "Id",
    columns: [
      { name: "Id", type: "number" }, { name: "PONumber", type: "string" },
      { name: "VendorId", type: "number" }, { name: "Status", type: "string" },
      { name: "OrderDate", type: "date" }, { name: "TotalAmount", type: "decimal" },
    ],
    nodes: ["PurchaseOrderLine"]
  },
  Employee: {
    schema: "HR", pk: "EmpId",
    columns: [
      { name: "EmpId", type: "number" }, { name: "Name", type: "string" },
      { name: "Department", type: "string" }, { name: "JoiningDate", type: "date" },
      { name: "Salary", type: "decimal" }, { name: "IsActive", type: "boolean" },
    ],
    nodes: []
  },
  Vendor: {
    schema: "JANATICS", pk: "VendorId",
    columns: [
      { name: "VendorId", type: "number" }, { name: "VendorCode", type: "string" },
      { name: "VendorName", type: "string" }, { name: "City", type: "string" },
      { name: "IsActive", type: "boolean" }, { name: "CreditLimit", type: "decimal" },
    ],
    nodes: []
  }
};

const ENTITIES = Object.keys(REGISTRY);

const OPS_BY_TYPE = {
  string:  ["eq","neq","contains","startsWith","endsWith","in","isNull","isNotNull"],
  number:  ["eq","neq","gt","gte","lt","lte","between","in","isNull","isNotNull"],
  decimal: ["eq","neq","gt","gte","lt","lte","between","isNull","isNotNull"],
  date:    ["eq","neq","gt","gte","lt","lte","between","isNull","isNotNull"],
  boolean: ["eq","isNull","isNotNull"],
};

const OP_LABELS = {
  eq:"= equals", neq:"≠ not equals", contains:"~ contains",
  startsWith:"^_ starts with", endsWith:"_$ ends with",
  gt:"> greater than", gte:"≥ greater or equal",
  lt:"< less than", lte:"≤ less or equal",
  between:"↔ between", in:"∈ in list",
  isNull:"∅ is null", isNotNull:"• is not null"
};

const TYPE_CHIP = {
  string: { bg:"#1a3a2a", text:"#4ade80", border:"#14532d" },
  number: { bg:"#2a1e0a", text:"#fbbf24", border:"#78350f" },
  decimal:{ bg:"#1e1a3a", text:"#a78bfa", border:"#3730a3" },
  date:   { bg:"#0a2a3a", text:"#38bdf8", border:"#075985" },
  boolean:{ bg:"#2a1a1a", text:"#f87171", border:"#7f1d1d" },
};

function uid() { return Math.random().toString(36).slice(2, 8); }

// ── Filter Row ────────────────────────────────────────────────────────────────
function FilterRow({ filter, columns, onChange, onRemove }) {
  const col = columns.find(c => c.name === filter.column);
  const ops = col ? OPS_BY_TYPE[col.type] || [] : [];
  const needsValue = !["isNull","isNotNull"].includes(filter.op);
  const needsTwo = filter.op === "between";

  const sx = {
    input: {
      background:"#0d1526", border:"1px solid #1e3060", borderRadius:6,
      color:"#c8d8f0", padding:"6px 10px", fontSize:12,
      fontFamily:"'DM Mono',monospace",
    }
  };

  return (
    <div style={{
      display:"flex", alignItems:"center", gap:8, flexWrap:"wrap",
      padding:"10px 12px", background:"#090f1e",
      border:"1px solid #1a2a50", borderRadius:8, marginBottom:6,
      position:"relative"
    }}>
      {/* Logic connector */}
      <span style={{
        fontSize:9, fontWeight:800, color:"#3b82f6",
        background:"#1e3a8a20", border:"1px solid #1e3a8a",
        padding:"2px 6px", borderRadius:4, letterSpacing:"0.1em"
      }}>AND</span>

      {/* Column selector */}
      <select style={{ ...sx.input, cursor:"pointer", minWidth:140 }}
        value={filter.column}
        onChange={e => onChange({ ...filter, column:e.target.value, op:"eq", value:"", value2:"" })}>
        <option value="">— column —</option>
        {columns.map(c => <option key={c.name} value={c.name}>{c.name}</option>)}
      </select>

      {/* Type chip */}
      {col && (() => { const t = TYPE_CHIP[col.type] || {}; return (
        <span style={{
          fontSize:9, fontWeight:700, fontFamily:"'DM Mono',monospace",
          background:t.bg, color:t.text, border:`1px solid ${t.border}`,
          padding:"2px 6px", borderRadius:4
        }}>{col.type}</span>
      ); })()}

      {/* Operator */}
      <select style={{ ...sx.input, cursor:"pointer", minWidth:140 }}
        value={filter.op}
        onChange={e => onChange({ ...filter, op:e.target.value, value:"", value2:"" })}>
        {ops.map(o => <option key={o} value={o}>{OP_LABELS[o] || o}</option>)}
      </select>

      {/* Value inputs */}
      {needsValue && !needsTwo && (
        <input style={{ ...sx.input, minWidth:120 }}
          placeholder="value…"
          value={filter.value}
          onChange={e => onChange({ ...filter, value:e.target.value })}
          type={col?.type === "date" ? "date" : col?.type === "number" || col?.type === "decimal" ? "number" : "text"}
        />
      )}
      {needsTwo && (
        <>
          <input style={{ ...sx.input, width:100 }} placeholder="from"
            value={filter.value} onChange={e => onChange({ ...filter, value:e.target.value })}
            type={col?.type === "date" ? "date" : "number"} />
          <span style={{ color:"#334155", fontSize:11 }}>→</span>
          <input style={{ ...sx.input, width:100 }} placeholder="to"
            value={filter.value2||""} onChange={e => onChange({ ...filter, value2:e.target.value })}
            type={col?.type === "date" ? "date" : "number"} />
        </>
      )}

      <button onClick={onRemove} style={{
        marginLeft:"auto", background:"none", border:"none",
        color:"#334155", cursor:"pointer", fontSize:16, padding:"0 4px",
        lineHeight:1
      }}>×</button>
    </div>
  );
}

// ── Column Checkbox ───────────────────────────────────────────────────────────
function ColCheckbox({ col, selected, onToggle }) {
  const t = TYPE_CHIP[col.type] || {};
  return (
    <label style={{
      display:"flex", alignItems:"center", gap:8, cursor:"pointer",
      padding:"6px 10px", borderRadius:6,
      background: selected ? "#0d1a3a" : "transparent",
      border:`1px solid ${selected ? "#1e3a8a" : "transparent"}`,
      transition:"all 0.15s"
    }}>
      <input type="checkbox" checked={selected} onChange={onToggle}
        style={{ accentColor:"#3b82f6" }} />
      <span style={{
        fontFamily:"'DM Mono',monospace", fontSize:12,
        color: selected ? "#93c5fd" : "#475569"
      }}>{col.name}</span>
      <span style={{
        fontSize:9, background:t.bg, color:t.text,
        border:`1px solid ${t.border}`, padding:"1px 5px", borderRadius:3, fontWeight:700
      }}>{col.type}</span>
      {col.name === REGISTRY[Object.keys(REGISTRY)[0]]?.pk && (
        <span style={{ fontSize:9, color:"#fbbf24" }}>🔑</span>
      )}
    </label>
  );
}

// ── Main Component ────────────────────────────────────────────────────────────
export default function QueryBuilder() {
  const [entity, setEntity] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [includeCount, setIncludeCount] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [searchCols, setSearchCols] = useState([]);
  const [filters, setFilters] = useState([]);
  const [sortCol, setSortCol] = useState("");
  const [sortDir, setSortDir] = useState("asc");
  const [selectedCols, setSelectedCols] = useState([]);
  const [nodes, setNodes] = useState([]);
  const [activeSection, setActiveSection] = useState("columns");
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);

  const schema = entity ? REGISTRY[entity] : null;
  const columns = schema?.columns || [];

  const handleEntityChange = (e) => {
    setEntity(e);
    setSelectedCols([]);
    setFilters([]);
    setSearchCols([]);
    setSortCol("");
    setNodes([]);
    setResult(null);
  };

  const toggleCol = useCallback((name) => {
    setSelectedCols(p => p.includes(name) ? p.filter(c => c !== name) : [...p, name]);
  }, []);

  const toggleSearchCol = useCallback((name) => {
    setSearchCols(p => p.includes(name) ? p.filter(c => c !== name) : [...p, name]);
  }, []);

  const addFilter = () => {
    setFilters(p => [...p, { id:uid(), column:"", op:"eq", value:"", value2:"" }]);
  };

  const buildPayload = () => {
    const payload = {
      rootEntity: entity,
      page, pageSize, includeCount,
      ...(searchTerm && { search:{ term:searchTerm, columns:searchCols.length ? searchCols : columns.map(c=>c.name) } }),
      ...(filters.filter(f=>f.column).length && {
        filters: filters.filter(f=>f.column).map(f => ({
          column:f.column, op:f.op,
          ...(f.value !== "" ? { value: f.value } : {}),
          ...(f.value2 !== "" ? { value2: f.value2 } : {}),
        }))
      }),
      ...(sortCol && { sort:{ column:sortCol, direction:sortDir } }),
      ...(selectedCols.length && { select:selectedCols }),
      ...(nodes.length && { nodes }),
    };
    return payload;
  };

  const handleFetch = async () => {
    if (!entity) return;
    setLoading(true);
    await new Promise(r => setTimeout(r, 1400));
    // Mock response
    setResult({
      totalCount: 142,
      page, pageSize,
      totalPages: Math.ceil(142 / pageSize),
      data: Array.from({ length: Math.min(pageSize, 5) }, (_, i) => ({
        Id: i + 1 + (page-1)*pageSize,
        PONumber: `PO-2026-${String(i+1).padStart(3,"0")}`,
        Status: ["Approved","Draft","Pending"][i%3],
        TotalAmount: (Math.random()*50000+1000).toFixed(2),
        OrderDate: "2026-04-15",
      }))
    });
    setLoading(false);
  };

  const copyPayload = () => {
    navigator.clipboard?.writeText(JSON.stringify(buildPayload(), null, 2));
    setCopied(true);
    setTimeout(()=>setCopied(false), 2000);
  };

  const payload = entity ? buildPayload() : null;

  // ── Colors & theme ─────────────────────────────────────────────────────────
  const C = {
    bg0:"#060b17", bg1:"#0a1020", bg2:"#0d1526",
    bg3:"#111827", border:"#1a2a50", border2:"#1f3060",
    text:"#c8d8f0", dim:"#475569", accent:"#3b82f6",
    accentDim:"#1e3a8a", success:"#22c55e"
  };

  const sectionTabs = [
    { id:"columns", label:"Columns", icon:"⊞" },
    { id:"filters", label:"Filters", icon:"⊟", badge: filters.filter(f=>f.column).length || null },
    { id:"search", label:"Search", icon:"⊙", badge: searchTerm ? "•" : null },
    { id:"sort", label:"Sort", icon:"⇅", badge: sortCol ? "•" : null },
    { id:"nodes", label:"Joins", icon:"⊃", badge: nodes.length || null },
    { id:"pagination", label:"Pagination", icon:"⋯" },
  ];

  const inputSx = {
    background:C.bg2, border:`1px solid ${C.border}`, borderRadius:7,
    color:C.text, padding:"8px 12px", fontSize:12,
    fontFamily:"'DM Mono',monospace", outline:"none", width:"100%",
    boxSizing:"border-box"
  };

  return (
    <div style={{ minHeight:"100vh", background:C.bg0, fontFamily:"'Sora',sans-serif", display:"flex", flexDirection:"column" }}>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Sora:wght@400;500;600;700&family=DM+Mono:wght@400;500;600&display=swap');
        * { box-sizing:border-box; margin:0; }
        select option { background:#0d1526; color:#c8d8f0; }
        ::-webkit-scrollbar { width:4px; height:4px; }
        ::-webkit-scrollbar-track { background:#060b17; }
        ::-webkit-scrollbar-thumb { background:#1e3060; border-radius:4px; }
        input:focus, select:focus { border-color:#3b82f6 !important; outline:none; }
        @keyframes fadeIn { from{opacity:0;transform:translateY(6px)} to{opacity:1;transform:none} }
        @keyframes spin { to{transform:rotate(360deg)} }
      `}</style>

      {/* Top bar */}
      <div style={{
        background:`linear-gradient(90deg,${C.bg1},#0d1630)`,
        borderBottom:`1px solid ${C.border}`,
        padding:"0 24px", height:52,
        display:"flex", alignItems:"center", gap:16
      }}>
        <div style={{ display:"flex", alignItems:"center", gap:9 }}>
          <div style={{
            width:26, height:26, background:"linear-gradient(135deg,#1d4ed8,#6366f1)",
            borderRadius:6, display:"flex", alignItems:"center", justifyContent:"center", fontSize:13
          }}>⚡</div>
          <span style={{ fontWeight:700, fontSize:14, color:"#e2e8f0", letterSpacing:"-0.01em" }}>
            Query Builder
          </span>
          <span style={{
            fontSize:9, background:"#0c1a3a", color:"#60a5fa",
            padding:"2px 7px", borderRadius:10, fontWeight:700,
            border:"1px solid #1e3a8a"
          }}>/fetch</span>
        </div>

        <div style={{ flex:1 }} />

        {/* Entity selector */}
        <div style={{ display:"flex", alignItems:"center", gap:10 }}>
          <span style={{ fontSize:11, color:C.dim, fontWeight:600 }}>Entity</span>
          <select
            style={{ ...inputSx, width:200, cursor:"pointer" }}
            value={entity}
            onChange={e=>handleEntityChange(e.target.value)}>
            <option value="">— select table —</option>
            {ENTITIES.map(e=><option key={e}>{e}</option>)}
          </select>
        </div>
      </div>

      {/* Body */}
      <div style={{ display:"flex", flex:1, overflow:"hidden", height:"calc(100vh - 52px)" }}>

        {/* LEFT — Builder panel */}
        <div style={{
          width:420, background:C.bg1,
          borderRight:`1px solid ${C.border}`,
          display:"flex", flexDirection:"column",
          overflow:"hidden"
        }}>
          {/* Section tabs */}
          <div style={{
            display:"flex", overflowX:"auto",
            borderBottom:`1px solid ${C.border}`,
            background:C.bg0
          }}>
            {sectionTabs.map(t => (
              <button key={t.id} onClick={()=>setActiveSection(t.id)} style={{
                padding:"10px 14px", background:"none", border:"none",
                borderBottom:`2px solid ${activeSection===t.id ? C.accent : "transparent"}`,
                color: activeSection===t.id ? "#93c5fd" : C.dim,
                cursor:"pointer", fontSize:11, fontWeight:600,
                fontFamily:"'Sora',sans-serif", whiteSpace:"nowrap",
                display:"flex", alignItems:"center", gap:5,
                transition:"all 0.15s"
              }}>
                <span>{t.icon}</span>
                {t.label}
                {t.badge && (
                  <span style={{
                    background:C.accentDim, color:"#93c5fd",
                    fontSize:9, borderRadius:10, padding:"1px 5px",
                    fontWeight:700, border:`1px solid ${C.border2}`
                  }}>{t.badge}</span>
                )}
              </button>
            ))}
          </div>

          {/* Section content */}
          <div style={{ flex:1, overflowY:"auto", padding:16 }}>

            {/* COLUMNS */}
            {activeSection==="columns" && schema && (
              <div style={{ animation:"fadeIn 0.2s ease" }}>
                <div style={{ display:"flex", alignItems:"center", justifyContent:"space-between", marginBottom:12 }}>
                  <span style={{ fontSize:11, color:C.dim, fontWeight:700, letterSpacing:"0.08em", textTransform:"uppercase" }}>
                    Select Columns
                  </span>
                  <div style={{ display:"flex", gap:6 }}>
                    <button onClick={()=>setSelectedCols(columns.map(c=>c.name))} style={{
                      fontSize:10, background:C.bg2, color:C.dim,
                      border:`1px solid ${C.border}`, borderRadius:5,
                      padding:"3px 8px", cursor:"pointer"
                    }}>All</button>
                    <button onClick={()=>setSelectedCols([])} style={{
                      fontSize:10, background:C.bg2, color:C.dim,
                      border:`1px solid ${C.border}`, borderRadius:5,
                      padding:"3px 8px", cursor:"pointer"
                    }}>None</button>
                  </div>
                </div>
                <div style={{ display:"flex", flexDirection:"column", gap:4 }}>
                  {columns.map(col => (
                    <ColCheckbox key={col.name} col={col}
                      selected={selectedCols.includes(col.name)}
                      onToggle={()=>toggleCol(col.name)} />
                  ))}
                </div>
                {selectedCols.length === 0 && (
                  <div style={{ fontSize:11, color:"#475569", marginTop:8, fontStyle:"italic" }}>
                    No columns selected → all columns returned
                  </div>
                )}
              </div>
            )}

            {/* FILTERS */}
            {activeSection==="filters" && schema && (
              <div style={{ animation:"fadeIn 0.2s ease" }}>
                <div style={{ marginBottom:12 }}>
                  <span style={{ fontSize:11, color:C.dim, fontWeight:700, letterSpacing:"0.08em", textTransform:"uppercase" }}>
                    Filter Conditions
                  </span>
                </div>
                {filters.map((f,i) => (
                  <FilterRow key={f.id} filter={f} columns={columns}
                    onChange={updated=>setFilters(p=>p.map(x=>x.id===f.id?updated:x))}
                    onRemove={()=>setFilters(p=>p.filter(x=>x.id!==f.id))} />
                ))}
                <button onClick={addFilter} style={{
                  width:"100%", padding:"9px",
                  border:`1.5px dashed ${C.border2}`,
                  borderRadius:8, background:"transparent",
                  color:C.accent, cursor:"pointer", fontSize:12,
                  fontFamily:"'Sora',sans-serif", fontWeight:600, marginTop:4
                }}>+ Add Filter</button>
              </div>
            )}

            {/* SEARCH */}
            {activeSection==="search" && schema && (
              <div style={{ animation:"fadeIn 0.2s ease" }}>
                <div style={{ marginBottom:12 }}>
                  <span style={{ fontSize:11, color:C.dim, fontWeight:700, letterSpacing:"0.08em", textTransform:"uppercase" }}>
                    Full-Text Search
                  </span>
                </div>
                <div style={{ marginBottom:16 }}>
                  <label style={{ fontSize:11, color:C.dim, display:"block", marginBottom:6 }}>Search Term</label>
                  <input style={inputSx} placeholder="e.g. approved, PO-2026…"
                    value={searchTerm} onChange={e=>setSearchTerm(e.target.value)} />
                </div>
                <div>
                  <label style={{ fontSize:11, color:C.dim, display:"block", marginBottom:8 }}>
                    Search in columns <span style={{ color:"#334155" }}>(leave empty = all string columns)</span>
                  </label>
                  <div style={{ display:"flex", flexDirection:"column", gap:4 }}>
                    {columns.filter(c=>["string","number"].includes(c.type)).map(col=>(
                      <label key={col.name} style={{
                        display:"flex", alignItems:"center", gap:8, cursor:"pointer",
                        padding:"5px 8px", borderRadius:6,
                        background: searchCols.includes(col.name) ? "#0d1a3a" : "transparent"
                      }}>
                        <input type="checkbox" checked={searchCols.includes(col.name)}
                          onChange={()=>toggleSearchCol(col.name)} style={{ accentColor:C.accent }} />
                        <span style={{ fontFamily:"'DM Mono',monospace", fontSize:12,
                          color:searchCols.includes(col.name) ? "#93c5fd" : C.dim }}>
                          {col.name}
                        </span>
                      </label>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* SORT */}
            {activeSection==="sort" && schema && (
              <div style={{ animation:"fadeIn 0.2s ease" }}>
                <div style={{ marginBottom:12 }}>
                  <span style={{ fontSize:11, color:C.dim, fontWeight:700, letterSpacing:"0.08em", textTransform:"uppercase" }}>
                    Sort Order
                  </span>
                </div>
                <div style={{ marginBottom:12 }}>
                  <label style={{ fontSize:11, color:C.dim, display:"block", marginBottom:6 }}>Sort By Column</label>
                  <select style={{ ...inputSx, cursor:"pointer" }} value={sortCol} onChange={e=>setSortCol(e.target.value)}>
                    <option value="">— no sort —</option>
                    {columns.map(c=><option key={c.name} value={c.name}>{c.name}</option>)}
                  </select>
                </div>
                {sortCol && (
                  <div>
                    <label style={{ fontSize:11, color:C.dim, display:"block", marginBottom:8 }}>Direction</label>
                    <div style={{ display:"flex", gap:8 }}>
                      {["asc","desc"].map(d=>(
                        <button key={d} onClick={()=>setSortDir(d)} style={{
                          flex:1, padding:"9px",
                          background: sortDir===d ? "#1e3a8a" : C.bg2,
                          color: sortDir===d ? "#93c5fd" : C.dim,
                          border:`1px solid ${sortDir===d ? "#1d4ed8" : C.border}`,
                          borderRadius:7, cursor:"pointer", fontWeight:700, fontSize:12,
                          fontFamily:"'Sora',sans-serif"
                        }}>
                          {d==="asc"?"↑ ASC":"↓ DESC"}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* NODES / JOINS */}
            {activeSection==="nodes" && schema && (
              <div style={{ animation:"fadeIn 0.2s ease" }}>
                <div style={{ marginBottom:12 }}>
                  <span style={{ fontSize:11, color:C.dim, fontWeight:700, letterSpacing:"0.08em", textTransform:"uppercase" }}>
                    Related Tables (Joins)
                  </span>
                </div>
                {schema.nodes.length === 0 ? (
                  <div style={{ fontSize:12, color:"#334155", textAlign:"center", padding:"20px" }}>
                    No child tables registered for {entity}
                  </div>
                ) : schema.nodes.map(nodeEntity => {
                  const active = nodes.find(n=>n.nodeEntity===nodeEntity);
                  return (
                    <div key={nodeEntity} style={{
                      padding:"12px 14px", borderRadius:8, marginBottom:8,
                      background: active ? "#0a1a3a" : C.bg2,
                      border:`1px solid ${active ? "#1e3a8a" : C.border}`,
                      transition:"all 0.15s"
                    }}>
                      <div style={{ display:"flex", alignItems:"center", justifyContent:"space-between", marginBottom: active ? 10 : 0 }}>
                        <span style={{ fontFamily:"'DM Mono',monospace", fontSize:13, color: active ? "#93c5fd" : C.dim, fontWeight:600 }}>
                          {nodeEntity}
                        </span>
                        <button onClick={()=>{
                          if(active) setNodes(p=>p.filter(n=>n.nodeEntity!==nodeEntity));
                          else setNodes(p=>[...p, { nodeEntity, parentLink:`${entity}Id` }]);
                        }} style={{
                          fontSize:11, padding:"4px 10px",
                          background: active ? "#7f1d1d20" : "#1e3a8a20",
                          color: active ? "#fca5a5" : "#93c5fd",
                          border:`1px solid ${active ? "#7f1d1d" : "#1e3a8a"}`,
                          borderRadius:5, cursor:"pointer", fontWeight:600
                        }}>
                          {active ? "Remove" : "+ Include"}
                        </button>
                      </div>
                      {active && (
                        <div>
                          <label style={{ fontSize:10, color:C.dim, display:"block", marginBottom:4 }}>FK Column</label>
                          <input style={{ ...inputSx }}
                            value={active.parentLink}
                            onChange={e=>setNodes(p=>p.map(n=>n.nodeEntity===nodeEntity?{...n,parentLink:e.target.value}:n))} />
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}

            {/* PAGINATION */}
            {activeSection==="pagination" && (
              <div style={{ animation:"fadeIn 0.2s ease" }}>
                <div style={{ marginBottom:12 }}>
                  <span style={{ fontSize:11, color:C.dim, fontWeight:700, letterSpacing:"0.08em", textTransform:"uppercase" }}>
                    Pagination
                  </span>
                </div>
                <div style={{ marginBottom:14 }}>
                  <label style={{ fontSize:11, color:C.dim, display:"block", marginBottom:6 }}>Page Number</label>
                  <div style={{ display:"flex", alignItems:"center", gap:8 }}>
                    <button onClick={()=>setPage(p=>Math.max(1,p-1))} style={{
                      padding:"7px 12px", background:C.bg2,
                      border:`1px solid ${C.border}`, borderRadius:6,
                      color:C.dim, cursor:"pointer", fontSize:14
                    }}>‹</button>
                    <input style={{ ...inputSx, width:80, textAlign:"center" }}
                      type="number" min={1} value={page} onChange={e=>setPage(+e.target.value||1)} />
                    <button onClick={()=>setPage(p=>p+1)} style={{
                      padding:"7px 12px", background:C.bg2,
                      border:`1px solid ${C.border}`, borderRadius:6,
                      color:C.dim, cursor:"pointer", fontSize:14
                    }}>›</button>
                  </div>
                </div>
                <div style={{ marginBottom:14 }}>
                  <label style={{ fontSize:11, color:C.dim, display:"block", marginBottom:8 }}>Page Size</label>
                  <div style={{ display:"flex", gap:6, flexWrap:"wrap" }}>
                    {[10,25,50,100,500].map(s=>(
                      <button key={s} onClick={()=>setPageSize(s)} style={{
                        padding:"6px 14px",
                        background: pageSize===s ? "#1e3a8a" : C.bg2,
                        color: pageSize===s ? "#93c5fd" : C.dim,
                        border:`1px solid ${pageSize===s ? "#1d4ed8" : C.border}`,
                        borderRadius:6, cursor:"pointer", fontSize:12,
                        fontFamily:"'Sora',sans-serif", fontWeight:600
                      }}>{s}</button>
                    ))}
                  </div>
                </div>
                <label style={{
                  display:"flex", alignItems:"center", gap:8, cursor:"pointer",
                  padding:"10px 12px", background:C.bg2,
                  border:`1px solid ${C.border}`, borderRadius:7
                }}>
                  <input type="checkbox" checked={includeCount}
                    onChange={e=>setIncludeCount(e.target.checked)}
                    style={{ accentColor:C.accent }} />
                  <div>
                    <div style={{ fontSize:12, color:C.text, fontWeight:600 }}>Include Total Count</div>
                    <div style={{ fontSize:10, color:C.dim }}>Adds COUNT(*) query — slight overhead</div>
                  </div>
                </label>
              </div>
            )}

            {!entity && (
              <div style={{ textAlign:"center", padding:"40px 20px", color:"#334155" }}>
                <div style={{ fontSize:28, marginBottom:8 }}>⚡</div>
                <div style={{ fontSize:13, fontWeight:600, color:"#475569" }}>Select an entity to begin</div>
                <div style={{ fontSize:11, marginTop:4 }}>Choose a table from the top bar</div>
              </div>
            )}
          </div>
        </div>

        {/* CENTER — Payload preview */}
        <div style={{
          width:320, background:C.bg0,
          borderRight:`1px solid ${C.border}`,
          display:"flex", flexDirection:"column"
        }}>
          <div style={{
            padding:"12px 16px", borderBottom:`1px solid ${C.border}`,
            display:"flex", alignItems:"center", justifyContent:"space-between"
          }}>
            <span style={{ fontSize:11, color:C.dim, fontWeight:700, letterSpacing:"0.08em", textTransform:"uppercase" }}>
              Generated Payload
            </span>
            <button onClick={copyPayload} style={{
              fontSize:10, padding:"3px 10px",
              background: copied ? "#14532d20" : C.bg2,
              color: copied ? "#4ade80" : C.dim,
              border:`1px solid ${copied ? "#14532d" : C.border}`,
              borderRadius:5, cursor:"pointer", fontWeight:600
            }}>
              {copied ? "✓ Copied" : "Copy"}
            </button>
          </div>
          <pre style={{
            flex:1, overflow:"auto", padding:16, margin:0,
            fontSize:11, lineHeight:1.8,
            fontFamily:"'DM Mono',monospace",
            color:"#60a5fa"
          }}>
            {payload ? JSON.stringify(payload, null, 2) : "// select an entity\n// to see payload"}
          </pre>
        </div>

        {/* RIGHT — Results */}
        <div style={{ flex:1, display:"flex", flexDirection:"column", background:C.bg1, overflow:"hidden" }}>
          {/* Execute bar */}
          <div style={{
            padding:"12px 20px", borderBottom:`1px solid ${C.border}`,
            display:"flex", alignItems:"center", gap:14,
            background:C.bg0
          }}>
            <button onClick={handleFetch} disabled={!entity||loading} style={{
              padding:"9px 28px",
              background: entity && !loading
                ? "linear-gradient(135deg,#1e40af,#3b82f6)"
                : "#1a2030",
              color: entity && !loading ? "#fff" : "#334155",
              border:"none", borderRadius:8, cursor: entity ? "pointer" : "not-allowed",
              fontWeight:700, fontSize:13,
              fontFamily:"'Sora',sans-serif",
              boxShadow: entity && !loading ? "0 4px 16px #1d4ed840" : "none",
              display:"flex", alignItems:"center", gap:8
            }}>
              {loading
                ? <><span style={{ display:"inline-block", animation:"spin 0.8s linear infinite" }}>⟳</span> Fetching…</>
                : "▶ Execute Fetch"
              }
            </button>

            {result && (
              <div style={{ display:"flex", gap:16, fontSize:11, color:C.dim }}>
                <span>Total: <span style={{ color:"#93c5fd", fontWeight:700 }}>{result.totalCount}</span></span>
                <span>Page: <span style={{ color:"#93c5fd", fontWeight:700 }}>{result.page}/{result.totalPages}</span></span>
                <span>Showing: <span style={{ color:"#93c5fd", fontWeight:700 }}>{result.data.length}</span> rows</span>
              </div>
            )}
          </div>

          {/* Results area */}
          <div style={{ flex:1, overflowY:"auto", padding:20 }}>
            {!result && !loading && (
              <div style={{ textAlign:"center", padding:"60px 20px", color:"#1f2d4a" }}>
                <div style={{ fontSize:40, marginBottom:12 }}>⚡</div>
                <div style={{ fontSize:14, color:"#334155" }}>Build your query and hit Execute</div>
                <div style={{ fontSize:11, color:"#1f2d4a", marginTop:6 }}>
                  Results will appear here
                </div>
              </div>
            )}

            {result && (
              <div style={{ animation:"fadeIn 0.2s ease" }}>
                {/* Pagination controls */}
                <div style={{
                  display:"flex", justifyContent:"space-between", alignItems:"center",
                  marginBottom:14
                }}>
                  <span style={{ fontSize:11, color:C.dim }}>
                    {result.totalCount} records · Page {result.page} of {result.totalPages}
                  </span>
                  <div style={{ display:"flex", gap:6 }}>
                    <button onClick={()=>{setPage(p=>Math.max(1,p-1)); handleFetch();}}
                      disabled={result.page<=1} style={{
                        padding:"5px 12px", fontSize:11,
                        background:C.bg2, color:C.dim,
                        border:`1px solid ${C.border}`, borderRadius:5,
                        cursor: result.page>1 ? "pointer" : "not-allowed"
                      }}>‹ Prev</button>
                    <button onClick={()=>{setPage(p=>p+1); handleFetch();}}
                      disabled={result.page>=result.totalPages} style={{
                        padding:"5px 12px", fontSize:11,
                        background:C.bg2, color:C.dim,
                        border:`1px solid ${C.border}`, borderRadius:5,
                        cursor: result.page<result.totalPages ? "pointer" : "not-allowed"
                      }}>Next ›</button>
                  </div>
                </div>

                {/* Table */}
                <div style={{ overflowX:"auto", borderRadius:10, border:`1px solid ${C.border}` }}>
                  <table style={{ width:"100%", borderCollapse:"collapse", fontSize:12 }}>
                    <thead>
                      <tr style={{ background:C.bg0 }}>
                        {Object.keys(result.data[0]||{}).map(k=>(
                          <th key={k} style={{
                            padding:"10px 14px", textAlign:"left",
                            fontFamily:"'DM Mono',monospace", fontSize:11,
                            color:"#60a5fa", fontWeight:700,
                            borderBottom:`1px solid ${C.border}`,
                            letterSpacing:"0.04em"
                          }}>
                            {k}
                            {k===schema?.pk && <span style={{ color:"#fbbf24", marginLeft:4 }}>🔑</span>}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {result.data.map((row,i)=>(
                        <tr key={i} style={{
                          background: i%2===0 ? C.bg2 : C.bg1,
                          transition:"background 0.1s"
                        }}>
                          {Object.values(row).map((v,j)=>(
                            <td key={j} style={{
                              padding:"9px 14px",
                              fontFamily:"'DM Mono',monospace", fontSize:12,
                              color: j===0 ? "#fbbf24" : C.text,
                              borderBottom:`1px solid ${C.border}10`
                            }}>
                              {v !== null && v !== undefined
                                ? typeof v === "object" ? JSON.stringify(v) : String(v)
                                : <span style={{ color:"#1f2d4a", fontStyle:"italic" }}>null</span>}
                            </td>
                          ))}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}