import type { ProfileFormValues } from "../types";

export function getConnectionString(values: ProfileFormValues) {
  if (!values.database) return "";
  if (values.provider === "Oracle") {
    return `Data Source=${values.host}:${values.port || 1521}/${values.database}`;
  }
  if (values.provider === "SqlServer") {
    return `Server=${values.host};Database=${values.database}`;
  }
  return `Data Source=${values.database}`;
}
