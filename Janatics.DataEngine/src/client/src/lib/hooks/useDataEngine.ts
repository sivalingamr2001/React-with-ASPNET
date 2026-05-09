import { useState, useCallback } from 'react';
import { useDataEngineStore } from '@/lib/stores/dataEngineStore';
import { apiClient } from '@/lib/api/client';
import type { DatabaseConfig, Table, Query } from '@/types/dataEngine';
import { toast } from 'sonner';

export function useDataEngine() {
  const {
    dbConfigs,
    tables,
    queries,
    logs,
    stats,
    selectedConfigId,
    setDbConfigs,
    addDbConfig,
    updateDbConfig,
    deleteDbConfig,
    setSelectedConfigId,
    setTables,
    addTable,
    deleteTable,
    setQueries,
    addQuery,
    deleteQuery,
    setLogs,
    addLog,
    clearLogs,
    setStats,
  } = useDataEngineStore();

  const [isLoading, setIsLoading] = useState(false);

  // Database Config operations
  const loadDatabaseConfigs = useCallback(async () => {
    setIsLoading(true);
    try {
      const configs = await apiClient.getDatabaseConfigs();
      setDbConfigs(configs);
      // Save to localStorage
      localStorage.setItem('dbConfigs', JSON.stringify(configs));
    } catch (error) {
      toast.error('Failed to load database configs');
    } finally {
      setIsLoading(false);
    }
  }, [setDbConfigs]);

  const createDatabaseConfig = useCallback(
    async (config: DatabaseConfig) => {
      setIsLoading(true);
      try {
        const newConfig = await apiClient.createDatabaseConfig(config);
        if (newConfig) {
          addDbConfig(newConfig);
          localStorage.setItem('dbConfigs', JSON.stringify([...dbConfigs, newConfig]));
          toast.success('Database config created successfully');
          return newConfig;
        }
      } catch (error) {
        toast.error('Failed to create database config');
      } finally {
        setIsLoading(false);
      }
    },
    [addDbConfig, dbConfigs]
  );

  const removeDatabaseConfig = useCallback(
    async (id: string) => {
      try {
        const success = await apiClient.deleteDatabaseConfig(id);
        if (success) {
          deleteDbConfig(id);
          const updated = dbConfigs.filter((c) => c.id !== id);
          localStorage.setItem('dbConfigs', JSON.stringify(updated));
          toast.success('Database config deleted');
        }
      } catch (error) {
        toast.error('Failed to delete database config');
      }
    },
    [deleteDbConfig, dbConfigs]
  );

  const testConnection = useCallback(async (config: DatabaseConfig) => {
    try {
      const success = await apiClient.testDatabaseConnection(config);
      if (success) {
        toast.success('Connection successful');
      } else {
        toast.error('Connection failed');
      }
      return success;
    } catch (error) {
      toast.error('Connection test error');
      return false;
    }
  }, []);

  const updateDatabaseConfig = useCallback(
    async (id: string, config: Partial<DatabaseConfig>) => {
      setIsLoading(true);
      try {
        const updatedConfig = await apiClient.updateDatabaseConfig(id, config);
        if (updatedConfig) {
          updateDbConfig(id, updatedConfig);
          const updatedConfigs = dbConfigs.map((c) => (c.id === id ? updatedConfig : c));
          localStorage.setItem('dbConfigs', JSON.stringify(updatedConfigs));
          toast.success('Database config updated successfully');
          return updatedConfig;
        }
      } catch (error) {
        toast.error('Failed to update database config');
      } finally {
        setIsLoading(false);
      }
      return null;
    },
    [dbConfigs, updateDbConfig]
  );

  // Table operations
  const loadTables = useCallback(
    async (configId: string) => {
      setIsLoading(true);
      try {
        const tables = await apiClient.getTables(configId);
        setTables(tables);
        localStorage.setItem('tables', JSON.stringify(tables));
      } catch (error) {
        toast.error('Failed to load tables');
      } finally {
        setIsLoading(false);
      }
    },
    [setTables]
  );

  const createTable = useCallback(
    async (table: Table) => {
      setIsLoading(true);
      try {
        const newTable = await apiClient.createTable(table);
        if (newTable) {
          addTable(newTable);
          toast.success('Table created successfully');
          return newTable;
        }
      } catch (error) {
        toast.error('Failed to create table');
      } finally {
        setIsLoading(false);
      }
    },
    [addTable]
  );

  const removeTable = useCallback(
    async (id: string) => {
      try {
        const success = await apiClient.deleteTable(id);
        if (success) {
          deleteTable(id);
          toast.success('Table deleted');
        }
      } catch (error) {
        toast.error('Failed to delete table');
      }
    },
    [deleteTable]
  );

  // Query operations
  const loadQueries = useCallback(
    async (configId: string) => {
      setIsLoading(true);
      try {
        const queries = await apiClient.getQueries(configId);
        setQueries(queries);
        localStorage.setItem('queries', JSON.stringify(queries));
      } catch (error) {
        toast.error('Failed to load queries');
      } finally {
        setIsLoading(false);
      }
    },
    [setQueries]
  );

  const executeQuery = useCallback(async (query: Query) => {
    try {
      const result = await apiClient.executeQuery(query);
      return result;
    } catch (error) {
      toast.error('Failed to execute query');
      return null;
    }
  }, []);

  const saveQuery = useCallback(
    async (query: Query) => {
      setIsLoading(true);
      try {
        const savedQuery = await apiClient.saveQuery(query);
        if (savedQuery) {
          addQuery(savedQuery);
          toast.success('Query saved successfully');
          return savedQuery;
        }
      } catch (error) {
        toast.error('Failed to save query');
      } finally {
        setIsLoading(false);
      }
    },
    [addQuery]
  );

  const removeQuery = useCallback(
    async (id: string) => {
      try {
        const success = await apiClient.deleteQuery(id);
        if (success) {
          deleteQuery(id);
          toast.success('Query deleted');
        }
      } catch (error) {
        toast.error('Failed to delete query');
      }
    },
    [deleteQuery]
  );

  // Stats and monitoring
  const loadStats = useCallback(async () => {
    try {
      const stats = await apiClient.getStats();
      if (stats) {
        setStats(stats);
      }
    } catch (error) {
      console.error('Failed to load stats');
    }
  }, [setStats]);

  const loadLogs = useCallback(
    async (filters?: { level?: string; limit?: number }) => {
      try {
        const logs = await apiClient.getLogs(filters);
        setLogs(logs);
      } catch (error) {
        toast.error('Failed to load logs');
      }
    },
    [setLogs]
  );

  return {
    // State
    dbConfigs,
    tables,
    queries,
    logs,
    stats,
    selectedConfigId,
    isLoading,

    // Config operations
    loadDatabaseConfigs,
    createDatabaseConfig,
    removeDatabaseConfig,
    setSelectedConfigId,
    testConnection,
    updateDatabaseConfig,

    // Table operations
    loadTables,
    createTable,
    removeTable,

    // Query operations
    loadQueries,
    executeQuery,
    saveQuery,
    removeQuery,

    // Log operations
    loadLogs,
    clearLogs,
    addLog,

    // Stats
    loadStats,
  };
}
