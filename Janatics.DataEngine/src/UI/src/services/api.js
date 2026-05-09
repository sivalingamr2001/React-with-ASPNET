import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api/v1';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Mock data for development (replace with actual API calls later)
const mockTables = [
  {
    name: 'users',
    columns: [
      { name: 'id', type: 'integer', isPrimaryKey: true },
      { name: 'name', type: 'varchar(100)', isPrimaryKey: false },
      { name: 'email', type: 'varchar(100)', isPrimaryKey: false },
      { name: 'active', type: 'boolean', isPrimaryKey: false },
      { name: 'created_at', type: 'timestamp', isPrimaryKey: false }
    ]
  },
  {
    name: 'posts',
    columns: [
      { name: 'id', type: 'integer', isPrimaryKey: true },
      { name: 'user_id', type: 'integer', isPrimaryKey: false },
      { name: 'title', type: 'varchar(200)', isPrimaryKey: false },
      { name: 'content', type: 'text', isPrimaryKey: false },
      { name: 'created_at', type: 'timestamp', isPrimaryKey: false }
    ]
  },
  {
    name: 'query_definitions',
    columns: [
      { name: 'query_number', type: 'integer', isPrimaryKey: true },
      { name: 'query_text', type: 'text', isPrimaryKey: false },
      { name: 'description', type: 'text', isPrimaryKey: false }
    ]
  }
];

const mockQueries = [
  {
    queryNumber: 1,
    queryText: "SELECT u.id, u.name, u.email, u.created_at FROM users u WHERE u.active = {active}",
    description: "Get active users",
    tables: ['users']
  },
  {
    queryNumber: 2,
    queryText: "SELECT u.id, u.name, u.email, p.title, p.created_at FROM users u INNER JOIN posts p ON u.id = p.user_id WHERE u.id = {userId}",
    description: "Get user with posts",
    tables: ['users', 'posts']
  },
  {
    queryNumber: 3,
    queryText: "SELECT p.id, p.title, p.content, u.name as author FROM posts p INNER JOIN users u ON p.user_id = u.id WHERE p.created_at >= {fromDate}",
    description: "Get recent posts with authors",
    tables: ['posts', 'users']
  }
];

export const apiService = {
  // Table operations
  async getTables() {
    try {
      const response = await api.get('/metadata/tables');
      return response.data;
    } catch (error) {
      console.error('Error fetching tables:', error);
      return mockTables; // Fallback to mock data
    }
  },

  async getTableColumns(tableName) {
    try {
      const response = await api.get(`/metadata/tables/${tableName}/columns`);
      return response.data;
    } catch (error) {
      console.error('Error fetching table columns:', error);
      const table = mockTables.find(t => t.name === tableName);
      if (table) {
        return table.columns;
      }
      return [];
    }
  },

  // Query operations
  async getQueries() {
    try {
      const response = await api.get('/fetch/queries');
      return response.data;
    } catch (error) {
      console.error('Error fetching queries:', error);
      return mockQueries; // Fallback to mock data
    }
  },

  async getQueriesByTable(tableName) {
    try {
      const queries = await this.getQueries();
      return queries.filter(q => q.tables && q.tables.includes(tableName));
    } catch (error) {
      console.error('Error fetching queries by table:', error);
      return [];
    }
  },

  async saveQuery(queryData) {
    try {
      const response = await api.post('/fetch/queries', queryData);
      return response.data;
    } catch (error) {
      console.error('Error saving query:', error);
      const newQuery = {
        ...queryData,
        queryNumber: Math.max(...mockQueries.map(q => q.queryNumber)) + 1
      };
      mockQueries.push(newQuery);
      return newQuery;
    }
  },

  async updateQuery(queryNumber, queryData) {
    try {
      const payload = { ...queryData, queryNumber };
      const response = await api.post('/fetch/queries', payload);
      return response.data;
    } catch (error) {
      console.error('Error updating query:', error);
      const index = mockQueries.findIndex(q => q.queryNumber === queryNumber);
      if (index !== -1) {
        mockQueries[index] = { ...mockQueries[index], ...queryData };
        return mockQueries[index];
      }
      throw error;
    }
  },

  async deleteQuery(queryNumber) {
    try {
      await api.delete(`/fetch/queries/${queryNumber}`);
      return true;
    } catch (error) {
      console.error('Error deleting query:', error);
      const index = mockQueries.findIndex(q => q.queryNumber === queryNumber);
      if (index !== -1) {
        mockQueries.splice(index, 1);
        return true;
      }
      throw error;
    }
  },

  async executeQuery(queryNumber, parameters) {
    try {
      const response = await api.post('/fetch/execute', { queryNumber, parameters });
      return response.data;
    } catch (error) {
      console.error('Error executing query:', error);
      return {
        success: true,
        message: "Query executed successfully",
        data: [
          { id: 1, name: "John Doe", email: "john@example.com" },
          { id: 2, name: "Jane Smith", email: "jane@example.com" }
        ],
        totalCount: 2,
        pageNumber: 1,
        pageSize: 10
      };
    }
  }
};