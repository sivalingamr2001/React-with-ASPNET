export const SEED_PROFILES = [
  {
    id: "p1",
    name: "OracleERP",
    provider: "Oracle",
    host: "erp.janatics.local",
    port: "1521",
    database: "ERPDB",
    username: "erp_user",
    connectionString:
      "Data Source=erp.janatics.local:1521/ERPDB;User Id=erp_user;Password=••••••;",
    status: "connected",
    entityCount: 4,
    lastTested: "2 min ago",
  },
  {
    id: "p2",
    name: "SqlServerHR",
    provider: "SqlServer",
    host: "hr.janatics.local",
    port: "1433",
    database: "HrDB",
    username: "hr_admin",
    connectionString:
      "Server=hr.janatics.local;Database=HrDB;User Id=hr_admin;Password=••••••;",
    status: "connected",
    entityCount: 2,
    lastTested: "5 min ago",
  },
  {
    id: "p3",
    name: "SqliteLogs",
    provider: "Sqlite",
    host: "",
    port: "",
    database: "logs.db",
    username: "",
    connectionString: "Data Source=/data/logs.db;",
    status: "idle",
    entityCount: 1,
    lastTested: "1 hr ago",
  },
];

export const SEED_ENTITIES = {
  p1: [
    {
      id: "e1",
      name: "PurchaseOrder",
      schema: "JANATICS",
      pkColumn: "Id",
      isReadOnly: false,
      roles: ["admin", "user"],
      columnCount: 5,
    },
    {
      id: "e2",
      name: "PurchaseOrderLine",
      schema: "JANATICS",
      pkColumn: "Id",
      isReadOnly: false,
      roles: ["admin", "user"],
      columnCount: 4,
    },
    {
      id: "e3",
      name: "Vendor",
      schema: "JANATICS",
      pkColumn: "VendorId",
      isReadOnly: false,
      roles: ["admin"],
      columnCount: 7,
    },
    {
      id: "e4",
      name: "AuditLog",
      schema: "JANATICS",
      pkColumn: "LogId",
      isReadOnly: true,
      roles: ["admin"],
      columnCount: 6,
    },
  ],
  p2: [
    {
      id: "e5",
      name: "Employee",
      schema: "HR",
      pkColumn: "EmpId",
      isReadOnly: false,
      roles: ["admin", "hr"],
      columnCount: 8,
    },
    {
      id: "e6",
      name: "Department",
      schema: "HR",
      pkColumn: "DeptId",
      isReadOnly: false,
      roles: ["admin", "hr"],
      columnCount: 3,
    },
  ],
  p3: [
    {
      id: "e7",
      name: "SystemLog",
      schema: "dbo",
      pkColumn: "Id",
      isReadOnly: true,
      roles: ["admin"],
      columnCount: 5,
    },
  ],
};

export const SEED_COLUMNS = {
  e1: [
    {
      id: "c1",
      name: "Id",
      type: "number",
      required: true,
      isPK: true,
      isFk: false,
      fkRef: "",
    },
    {
      id: "c2",
      name: "PONumber",
      type: "string",
      required: true,
      isPK: false,
      isFk: false,
      fkRef: "",
    },
    {
      id: "c3",
      name: "VendorId",
      type: "number",
      required: true,
      isPK: false,
      isFk: true,
      fkRef: "Vendor.VendorId",
    },
    {
      id: "c4",
      name: "Status",
      type: "string",
      required: false,
      isPK: false,
      isFk: false,
      fkRef: "",
    },
    {
      id: "c5",
      name: "OrderDate",
      type: "date",
      required: false,
      isPK: false,
      isFk: false,
      fkRef: "",
    },
  ],
};

export const DISCOVERED_TABLES = [
  "ItemMaster",
  "StockLedger",
  "GRN",
  "Invoice",
  "CostCenter",
];

export const PROVIDERS = ["Oracle", "SqlServer", "Sqlite"];
export const PROVIDER_COLORS: Record<
  string,
  { bg: string; text: string; border: string }
> = {
  Oracle: { bg: "#fff3e0", text: "#b35c00", border: "#ffcc80" },
  SqlServer: { bg: "#e3f2fd", text: "#0d47a1", border: "#90caf9" },
  Sqlite: { bg: "#f3e5f5", text: "#6a1b9a", border: "#ce93d8" },
};
export const STATUS_COLORS: Record<
  string,
  { dot: string; text: string; bg: string }
> = {
  connected: { dot: "#22c55e", text: "#166534", bg: "#f0fdf4" },
  idle: { dot: "#94a3b8", text: "#475569", bg: "#f8fafc" },
  error: { dot: "#ef4444", text: "#991b1b", bg: "#fef2f2" },
};
export const COLUMN_TYPES = [
  "string",
  "number",
  "decimal",
  "date",
  "boolean",
  "guid",
];
