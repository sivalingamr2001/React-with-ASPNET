export enum UserRole {
  MASTER = 'master',
  ADMIN = 'admin',
  USER = 'user',
  OUTSIDE_FIRM_VENDOR = 'outside_firm_vendor',
}

export interface User {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  avatar?: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  setUser: (user: User | null) => void;
}

export type PermissionKey =
  | 'view_dashboard'
  | 'manage_db_config'
  | 'use_query_builder'
  | 'use_field_mapper'
  | 'view_logs'
  | 'manage_tables'
  | 'manage_users'
  | 'access_data_engine';

export const RolePermissions: Record<UserRole, PermissionKey[]> = {
  [UserRole.MASTER]: [
    'view_dashboard',
    'manage_db_config',
    'use_query_builder',
    'use_field_mapper',
    'view_logs',
    'manage_tables',
    'manage_users',
    'access_data_engine',
  ],
  [UserRole.ADMIN]: [
    'view_dashboard',
    'manage_db_config',
    'use_query_builder',
    'use_field_mapper',
    'view_logs',
    'manage_tables',
    'access_data_engine',
  ],
  [UserRole.USER]: [
    'view_dashboard',
    'use_query_builder',
    'use_field_mapper',
    'access_data_engine',
  ],
  [UserRole.OUTSIDE_FIRM_VENDOR]: [
    'view_dashboard',
    'use_query_builder',
    'access_data_engine',
  ],
};
