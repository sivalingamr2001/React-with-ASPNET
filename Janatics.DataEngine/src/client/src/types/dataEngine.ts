export interface DatabaseConfig {
  id?: string;
  name: string;
  provider: 'postgresql' | 'mysql' | 'mongodb' | 'dynamodb';
  host?: string;
  port?: number;
  database?: string;
  username?: string;
  password?: string;
  connectionString?: string;
  ssl?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface Table {
  id?: string;
  name: string;
  databaseConfigId: string;
  columns: Column[];
  createdAt?: string;
  updatedAt?: string;
}

export interface Column {
  id?: string;
  name: string;
  type: 'string' | 'number' | 'boolean' | 'date' | 'timestamp' | 'json';
  nullable?: boolean;
  primaryKey?: boolean;
  unique?: boolean;
  defaultValue?: string;
}

export interface Query {
  id?: string;
  name: string;
  databaseConfigId: string;
  sql?: string;
  fields?: QueryField[];
  filters?: QueryFilter[];
  createdAt?: string;
  updatedAt?: string;
}

export interface QueryField {
  tableAlias: string;
  columnName: string;
  alias?: string;
}

export interface QueryFilter {
  field: string;
  operator: 'eq' | 'neq' | 'gt' | 'gte' | 'lt' | 'lte' | 'in' | 'like';
  value: string | number | boolean;
}

export interface FieldMapping {
  id?: string;
  name: string;
  sourceField: string;
  targetField: string;
  transformation?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface LogEntry {
  id: string;
  timestamp: string;
  level: 'info' | 'warning' | 'error' | 'debug';
  message: string;
  details?: Record<string, any>;
  userId?: string;
  action: string;
}

export interface DataEngineStats {
  totalQueries: number;
  totalErrors: number;
  averageQueryTime: number;
  lastQueryTime: string;
  activeConnections: number;
  uptime: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}
