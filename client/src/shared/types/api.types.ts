export interface PaginatedResponse<T> {
  data: T[];
  meta: {
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
  };
}

export interface ApiErrorResponse {
  message: string;
  code: string;
  errors?: Record<string, string[]>;
}

// Generic Result type — used in service layers
export type Result<T, E = Error> =
  | { success: true; data: T }
  | { success: false; error: E };