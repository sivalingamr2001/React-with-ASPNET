import axios, { type AxiosInstance, type AxiosError } from 'axios';
import type {
  DatabaseConfig,
  Table,
  Query,
  LogEntry,
  DataEngineStats,
  ApiResponse,
} from '@/types/dataEngine';

class DataEngineApiClient {
  private client: AxiosInstance;
  private baseURL: string;

  constructor() {
    this.baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:3001/api';

    this.client = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        console.error('[v0] API Error:', error.message);
        return Promise.reject(error);
      }
    );
  }

  // Database Config endpoints
  async getDatabaseConfigs(): Promise<DatabaseConfig[]> {
    try {
      const response = await this.client.get<ApiResponse<DatabaseConfig[]>>(
        '/database-configs'
      );
      return response.data.data || [];
    } catch (error) {
      console.error('[v0] Failed to fetch database configs:', error);
      return [];
    }
  }

  async getDatabaseConfig(id: string): Promise<DatabaseConfig | null> {
    try {
      const response = await this.client.get<ApiResponse<DatabaseConfig>>(
        `/database-configs/${id}`
      );
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to fetch database config:', error);
      return null;
    }
  }

  async createDatabaseConfig(config: DatabaseConfig): Promise<DatabaseConfig | null> {
    try {
      const response = await this.client.post<ApiResponse<DatabaseConfig>>(
        '/database-configs',
        config
      );
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to create database config:', error);
      return null;
    }
  }

  async updateDatabaseConfig(
    id: string,
    config: Partial<DatabaseConfig>
  ): Promise<DatabaseConfig | null> {
    try {
      const response = await this.client.put<ApiResponse<DatabaseConfig>>(
        `/database-configs/${id}`,
        config
      );
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to update database config:', error);
      return null;
    }
  }

  async deleteDatabaseConfig(id: string): Promise<boolean> {
    try {
      await this.client.delete(`/database-configs/${id}`);
      return true;
    } catch (error) {
      console.error('[v0] Failed to delete database config:', error);
      return false;
    }
  }

  async testDatabaseConnection(config: DatabaseConfig): Promise<boolean> {
    try {
      const response = await this.client.post<ApiResponse<{ success: boolean }>>(
        '/database-configs/test-connection',
        config
      );
      return response.data.data?.success || false;
    } catch (error) {
      console.error('[v0] Database connection test failed:', error);
      return false;
    }
  }

  // Table endpoints
  async getTables(configId: string): Promise<Table[]> {
    try {
      const response = await this.client.get<ApiResponse<Table[]>>(
        `/tables?configId=${configId}`
      );
      return response.data.data || [];
    } catch (error) {
      console.error('[v0] Failed to fetch tables:', error);
      return [];
    }
  }

  async getTable(id: string): Promise<Table | null> {
    try {
      const response = await this.client.get<ApiResponse<Table>>(`/tables/${id}`);
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to fetch table:', error);
      return null;
    }
  }

  async createTable(table: Table): Promise<Table | null> {
    try {
      const response = await this.client.post<ApiResponse<Table>>('/tables', table);
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to create table:', error);
      return null;
    }
  }

  async updateTable(id: string, table: Partial<Table>): Promise<Table | null> {
    try {
      const response = await this.client.put<ApiResponse<Table>>(`/tables/${id}`, table);
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to update table:', error);
      return null;
    }
  }

  async deleteTable(id: string): Promise<boolean> {
    try {
      await this.client.delete(`/tables/${id}`);
      return true;
    } catch (error) {
      console.error('[v0] Failed to delete table:', error);
      return false;
    }
  }

  // Query endpoints
  async getQueries(configId: string): Promise<Query[]> {
    try {
      const response = await this.client.get<ApiResponse<Query[]>>(
        `/queries?configId=${configId}`
      );
      return response.data.data || [];
    } catch (error) {
      console.error('[v0] Failed to fetch queries:', error);
      return [];
    }
  }

  async executeQuery(query: Query): Promise<any> {
    try {
      const response = await this.client.post<ApiResponse<any>>('/queries/execute', query);
      return response.data.data || [];
    } catch (error) {
      console.error('[v0] Failed to execute query:', error);
      return null;
    }
  }

  async saveQuery(query: Query): Promise<Query | null> {
    try {
      const response = await this.client.post<ApiResponse<Query>>('/queries', query);
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to save query:', error);
      return null;
    }
  }

  async deleteQuery(id: string): Promise<boolean> {
    try {
      await this.client.delete(`/queries/${id}`);
      return true;
    } catch (error) {
      console.error('[v0] Failed to delete query:', error);
      return false;
    }
  }

  // Logs endpoints
  async getLogs(filters?: {
    level?: string;
    limit?: number;
    offset?: number;
  }): Promise<LogEntry[]> {
    try {
      const params = new URLSearchParams();
      if (filters?.level) params.append('level', filters.level);
      if (filters?.limit) params.append('limit', filters.limit.toString());
      if (filters?.offset) params.append('offset', filters.offset.toString());

      const response = await this.client.get<ApiResponse<LogEntry[]>>(
        `/logs?${params.toString()}`
      );
      return response.data.data || [];
    } catch (error) {
      console.error('[v0] Failed to fetch logs:', error);
      return [];
    }
  }

  async clearLogs(): Promise<boolean> {
    try {
      await this.client.delete('/logs');
      return true;
    } catch (error) {
      console.error('[v0] Failed to clear logs:', error);
      return false;
    }
  }

  // Stats/Monitoring endpoints
  async getStats(): Promise<DataEngineStats | null> {
    try {
      const response = await this.client.get<ApiResponse<DataEngineStats>>('/stats');
      return response.data.data || null;
    } catch (error) {
      console.error('[v0] Failed to fetch stats:', error);
      return null;
    }
  }

  async getHealthStatus(): Promise<{ status: string; uptime: number }> {
    try {
      const response = await this.client.get('/health');
      return response.data;
    } catch (error) {
      console.error('[v0] Health check failed:', error);
      return { status: 'offline', uptime: 0 };
    }
  }
}

export const apiClient = new DataEngineApiClient();
