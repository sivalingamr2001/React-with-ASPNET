import { create } from 'zustand';
import type { DatabaseConfig, Table, Query, LogEntry, DataEngineStats } from '@/types/dataEngine';

interface DataEngineState {
  // Data
  dbConfigs: DatabaseConfig[];
  tables: Table[];
  queries: Query[];
  logs: LogEntry[];
  stats: DataEngineStats | null;
  selectedConfigId: string | null;

  // Actions
  setDbConfigs: (configs: DatabaseConfig[]) => void;
  addDbConfig: (config: DatabaseConfig) => void;
  updateDbConfig: (id: string, config: DatabaseConfig) => void;
  deleteDbConfig: (id: string) => void;
  setSelectedConfigId: (id: string | null) => void;

  setTables: (tables: Table[]) => void;
  addTable: (table: Table) => void;
  updateTable: (id: string, table: Table) => void;
  deleteTable: (id: string) => void;

  setQueries: (queries: Query[]) => void;
  addQuery: (query: Query) => void;
  updateQuery: (id: string, query: Query) => void;
  deleteQuery: (id: string) => void;

  setLogs: (logs: LogEntry[]) => void;
  addLog: (log: LogEntry) => void;
  clearLogs: () => void;

  setStats: (stats: DataEngineStats) => void;
}

export const useDataEngineStore = create<DataEngineState>((set) => ({
  dbConfigs: [],
  tables: [],
  queries: [],
  logs: [],
  stats: null,
  selectedConfigId: null,

  setDbConfigs: (configs) => set({ dbConfigs: configs }),
  addDbConfig: (config) =>
    set((state) => ({
      dbConfigs: [...state.dbConfigs, { ...config, id: Date.now().toString() }],
    })),
  updateDbConfig: (id, config) =>
    set((state) => ({
      dbConfigs: state.dbConfigs.map((c) => (c.id === id ? { ...config, id } : c)),
    })),
  deleteDbConfig: (id) =>
    set((state) => ({
      dbConfigs: state.dbConfigs.filter((c) => c.id !== id),
    })),
  setSelectedConfigId: (id) => set({ selectedConfigId: id }),

  setTables: (tables) => set({ tables }),
  addTable: (table) =>
    set((state) => ({
      tables: [...state.tables, { ...table, id: Date.now().toString() }],
    })),
  updateTable: (id, table) =>
    set((state) => ({
      tables: state.tables.map((t) => (t.id === id ? { ...table, id } : t)),
    })),
  deleteTable: (id) =>
    set((state) => ({
      tables: state.tables.filter((t) => t.id !== id),
    })),

  setQueries: (queries) => set({ queries }),
  addQuery: (query) =>
    set((state) => ({
      queries: [...state.queries, { ...query, id: Date.now().toString() }],
    })),
  updateQuery: (id, query) =>
    set((state) => ({
      queries: state.queries.map((q) => (q.id === id ? { ...query, id } : q)),
    })),
  deleteQuery: (id) =>
    set((state) => ({
      queries: state.queries.filter((q) => q.id !== id),
    })),

  setLogs: (logs) => set({ logs }),
  addLog: (log) =>
    set((state) => ({
      logs: [log, ...state.logs.slice(0, 999)], // Keep last 1000 logs
    })),
  clearLogs: () => set({ logs: [] }),

  setStats: (stats) => set({ stats }),
}));

// Load from localStorage on mount
if (typeof window !== 'undefined') {
  const savedConfigs = localStorage.getItem('dbConfigs');
  const savedTables = localStorage.getItem('tables');
  const savedQueries = localStorage.getItem('queries');

  if (savedConfigs) {
    try {
      useDataEngineStore.setState({ dbConfigs: JSON.parse(savedConfigs) });
    } catch {}
  }
  if (savedTables) {
    try {
      useDataEngineStore.setState({ tables: JSON.parse(savedTables) });
    } catch {}
  }
  if (savedQueries) {
    try {
      useDataEngineStore.setState({ queries: JSON.parse(savedQueries) });
    } catch {}
  }
}
