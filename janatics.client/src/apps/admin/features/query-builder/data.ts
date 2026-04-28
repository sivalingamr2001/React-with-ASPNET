import type { QueryEntity } from "./types";

export const QUERY_REGISTRY: Record<string, QueryEntity> = {
  PurchaseOrder: {
    schema: "JANATICS",
    pk: "Id",
    columns: [
      { name: "Id", type: "number" },
      { name: "PONumber", type: "string" },
      { name: "VendorId", type: "number" },
      { name: "Status", type: "string" },
      { name: "OrderDate", type: "date" },
      { name: "TotalAmount", type: "decimal" },
    ],
    nodes: ["PurchaseOrderLine"],
  },
  Employee: {
    schema: "HR",
    pk: "EmpId",
    columns: [
      { name: "EmpId", type: "number" },
      { name: "Name", type: "string" },
      { name: "Department", type: "string" },
      { name: "JoiningDate", type: "date" },
      { name: "Salary", type: "decimal" },
      { name: "IsActive", type: "boolean" },
    ],
    nodes: [],
  },
  Vendor: {
    schema: "JANATICS",
    pk: "VendorId",
    columns: [
      { name: "VendorId", type: "number" },
      { name: "VendorCode", type: "string" },
      { name: "VendorName", type: "string" },
      { name: "City", type: "string" },
      { name: "IsActive", type: "boolean" },
      { name: "CreditLimit", type: "decimal" },
    ],
    nodes: [],
  },
};

export const QUERY_ENTITIES = Object.keys(QUERY_REGISTRY);
export const OPS_BY_TYPE = {
  string: [
    "eq",
    "neq",
    "contains",
    "startsWith",
    "endsWith",
    "in",
    "isNull",
    "isNotNull",
  ],
  number: [
    "eq",
    "neq",
    "gt",
    "gte",
    "lt",
    "lte",
    "between",
    "in",
    "isNull",
    "isNotNull",
  ],
  decimal: [
    "eq",
    "neq",
    "gt",
    "gte",
    "lt",
    "lte",
    "between",
    "isNull",
    "isNotNull",
  ],
  date: [
    "eq",
    "neq",
    "gt",
    "gte",
    "lt",
    "lte",
    "between",
    "isNull",
    "isNotNull",
  ],
  boolean: ["eq", "isNull", "isNotNull"],
};
