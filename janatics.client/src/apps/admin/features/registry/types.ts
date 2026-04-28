import type { ColumnMeta, DbProfile, EntityMeta } from "@/services/registryApi";

export interface ProfileFormValues extends Omit<DbProfile, "profileId"> {}

export interface EntityFormValues extends Omit<EntityMeta, "entityId"> {}

export interface ColumnFormValues extends Omit<
  ColumnMeta,
  "columnId" | "entityId"
> {}

export const providers = ["Oracle", "SqlServer", "Sqlite"] as const;
export const statuses = ["active", "inactive", "error"] as const;
export const dataTypes = [
  "string",
  "number",
  "decimal",
  "date",
  "boolean",
  "guid",
] as const;

export const defaultProfileFormValues: ProfileFormValues = {
  profileName: "",
  provider: "SqlServer",
  host: "",
  port: 1433,
  database: "",
  username: "",
  status: "active",
};
