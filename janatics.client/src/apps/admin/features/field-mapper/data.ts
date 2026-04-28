import type { SchemaEntity } from "./types";

export const FIELD_SCHEMA: Record<string, SchemaEntity> = {
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

export const FIELD_ENTITIES = Object.keys(FIELD_SCHEMA);
