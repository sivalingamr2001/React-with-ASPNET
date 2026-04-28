export interface QueryColumn {
  name: string;
  type: "string" | "number" | "decimal" | "date" | "boolean";
}

export interface QueryEntity {
  columns: QueryColumn[];
  nodes: string[];
  pk: string;
  schema: string;
}

export interface QueryFilter {
  column: string;
  id: string;
  op: string;
  value: string;
  value2: string;
}
