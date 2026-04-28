export interface SchemaColumn {
  isPK: boolean;
  name: string;
  required: boolean;
  type: "string" | "number" | "decimal" | "date";
}

export interface SchemaEntity {
  children: string[];
  columns: SchemaColumn[];
}

export interface ChildRecord {
  entity: string;
  fkColumn: string;
  id: number;
  values: Record<string, string>;
}
