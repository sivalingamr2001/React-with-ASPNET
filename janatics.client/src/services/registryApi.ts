import axios from "axios";

export interface DbProfile {
  profileId: string;
  profileName: string;
  provider: "Oracle" | "SqlServer" | "Sqlite";
  host?: string;
  port?: number;
  database: string;
  username?: string;
  status: "active" | "inactive" | "error";
}

export interface ColumnMeta {
  columnId: string;
  entityId: string;
  columnName: string;
  dataType: "string" | "number" | "decimal" | "date" | "boolean" | "guid";
  isRequired: boolean;
  isPrimaryKey: boolean;
  isForeignKey: boolean;
  fkReference?: string;
}

export interface EntityMeta {
  entityId: string;
  profileId: string;
  entityName: string;
  schemaName: string;
  pkColumn: string;
  isReadOnly: boolean;
  allowedRoles?: string;
  softDeleteColumn?: string;
  softDeleteValue?: string;
  columns?: ColumnMeta[];
}

export interface TestConnectionResponse {
  status: "success" | "error";
  latencyMs: number;
  message?: string;
}

export interface DiscoverTablesResponse {
  tables: string[];
}

const API_BASE =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api";

// DB Profile endpoints
export async function getProfiles(): Promise<DbProfile[]> {
  const { data } = await axios.get<DbProfile[]>(
    `${API_BASE}/registry/profiles`,
  );
  return data;
}

export async function createProfile(
  profile: Omit<DbProfile, "profileId">,
): Promise<DbProfile> {
  const { data } = await axios.post<DbProfile>(
    `${API_BASE}/registry/profiles`,
    profile,
  );
  return data;
}

export async function updateProfile(
  profileId: string,
  profile: Partial<DbProfile>,
): Promise<DbProfile> {
  const { data } = await axios.put<DbProfile>(
    `${API_BASE}/registry/profiles/${profileId}`,
    profile,
  );
  return data;
}

export async function deleteProfile(profileId: string): Promise<void> {
  await axios.delete(`${API_BASE}/registry/profiles/${profileId}`);
}

export async function testConnection(
  profileId: string,
): Promise<TestConnectionResponse> {
  const { data } = await axios.post<TestConnectionResponse>(
    `${API_BASE}/registry/profiles/${profileId}/test`,
  );
  return data;
}

export async function discoverTables(profileId: string): Promise<string[]> {
  const { data } = await axios.post<DiscoverTablesResponse>(
    `${API_BASE}/registry/profiles/${profileId}/discover`,
  );
  return data.tables;
}

// Entity endpoints
export async function getEntities(profileId: string): Promise<EntityMeta[]> {
  const { data } = await axios.get<EntityMeta[]>(
    `${API_BASE}/registry/entities?profileId=${profileId}`,
  );
  return data;
}

export async function createEntity(
  entity: Omit<EntityMeta, "entityId">,
): Promise<EntityMeta> {
  const { data } = await axios.post<EntityMeta>(
    `${API_BASE}/registry/entities`,
    entity,
  );
  return data;
}

export async function updateEntity(
  entityId: string,
  entity: Partial<EntityMeta>,
): Promise<EntityMeta> {
  const { data } = await axios.put<EntityMeta>(
    `${API_BASE}/registry/entities/${entityId}`,
    entity,
  );
  return data;
}

export async function deleteEntity(entityId: string): Promise<void> {
  await axios.delete(`${API_BASE}/registry/entities/${entityId}`);
}

// Column endpoints
export async function getColumns(entityId: string): Promise<ColumnMeta[]> {
  const { data } = await axios.get<ColumnMeta[]>(
    `${API_BASE}/registry/entities/${entityId}/columns`,
  );
  return data;
}

export async function createColumn(
  column: Omit<ColumnMeta, "columnId">,
): Promise<ColumnMeta> {
  const { data } = await axios.post<ColumnMeta>(
    `${API_BASE}/registry/columns`,
    column,
  );
  return data;
}

export async function deleteColumn(columnId: string): Promise<void> {
  await axios.delete(`${API_BASE}/registry/columns/${columnId}`);
}
