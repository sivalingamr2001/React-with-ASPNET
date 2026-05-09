import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  Grid,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Card,
  CardContent,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  CircularProgress,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  Save,
  PlayArrow,
  ArrowBack,
  TableChart,
  Key,
  Storage,
  ExpandMore,
  Add,
  Delete as DeleteIcon
} from '@mui/icons-material';
import Editor from '@monaco-editor/react';
import { apiService } from '../services/api';

const QueryEditor = () => {
  const { queryNumber } = useParams();
  const navigate = useNavigate();
  const isEditMode = Boolean(queryNumber);

  // State for query data
  const [queryData, setQueryData] = useState({
    queryNumber: null,
    queryText: '',
    description: '',
    tables: []
  });

  // State for UI
  const [tables, setTables] = useState([]);
  const [selectedTable, setSelectedTable] = useState('');
  const [selectedTableDetails, setSelectedTableDetails] = useState(null);
  const [selectedColumns, setSelectedColumns] = useState([]);
  const [joins, setJoins] = useState([]);
  const [whereConditions, setWhereConditions] = useState([]);
  const [executionResult, setExecutionResult] = useState(null);
  const [executeDialogOpen, setExecuteDialogOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadTables();
    if (isEditMode) {
      loadQuery();
    }
  }, [queryNumber]);

  useEffect(() => {
    if (selectedTable && tables.length > 0) {
      loadTableDetails();
    }
  }, [selectedTable, tables]);

  useEffect(() => {
    generateQuery();
  }, [selectedColumns, joins, whereConditions]);

  const loadTables = async () => {
    try {
      const tablesData = await apiService.getTables();
      setTables(tablesData);
    } catch (error) {
      setError('Failed to load tables');
    }
  };

  const loadQuery = async () => {
    try {
      setLoading(true);
      const queries = await apiService.getQueries();
      const query = queries.find(q => q.queryNumber === parseInt(queryNumber));
      if (query) {
        setQueryData(query);
        if (query.tables && query.tables.length > 0) {
          setSelectedTable(query.tables[0]);
        }
      } else {
        setError('Query not found');
      }
    } catch (error) {
      setError('Failed to load query');
    } finally {
      setLoading(false);
    }
  };

  const loadTableDetails = async () => {
    const table = tables.find(t => t.name === selectedTable);
    if (table) {
      setSelectedTableDetails(table);
      // Initialize selected columns for new queries
      if (!isEditMode && selectedColumns.length === 0) {
        const tableColumns = table.columns.map(col => ({
          table: selectedTable,
          column: col.name,
          type: col.type,
          isPrimaryKey: col.isPrimaryKey,
          selected: true
        }));
        setSelectedColumns(tableColumns);
      }
    }
  };

  const handleTableSelect = (tableName) => {
    setSelectedTable(tableName);
    if (!queryData.tables.includes(tableName)) {
      setQueryData(prev => ({
        ...prev,
        tables: [...prev.tables, tableName]
      }));
    }
  };

  const removeTable = (tableName) => {
    setQueryData(prev => ({
      ...prev,
      tables: prev.tables.filter(t => t !== tableName)
    }));
    setSelectedColumns(prev => prev.filter(col => col.table !== tableName));
    setJoins(prev => prev.filter(join => 
      join.leftTable !== tableName && join.rightTable !== tableName
    ));
    if (selectedTable === tableName) {
      setSelectedTable('');
      setSelectedTableDetails(null);
    }
  };

  const toggleColumnSelection = (columnName) => {
    setSelectedColumns(prev => prev.map(col => 
      col.column === columnName && col.table === selectedTable
        ? { ...col, selected: !col.selected }
        : col
    ));
  };

  const addJoin = () => {
    if (queryData.tables.length >= 2) {
      const newJoin = {
        id: Date.now(),
        type: 'INNER JOIN',
        leftTable: queryData.tables[0],
        leftColumn: '',
        rightTable: queryData.tables[1],
        rightColumn: ''
      };
      setJoins(prev => [...prev, newJoin]);
    }
  };

  const updateJoin = (joinId, field, value) => {
    setJoins(prev => prev.map(join => 
      join.id === joinId ? { ...join, [field]: value } : join
    ));
  };

  const removeJoin = (joinId) => {
    setJoins(prev => prev.filter(join => join.id !== joinId));
  };

  const addWhereCondition = () => {
    const newCondition = {
      id: Date.now(),
      table: selectedTable || queryData.tables[0] || '',
      column: '',
      operator: '=',
      value: '',
      isParameter: false
    };
    setWhereConditions(prev => [...prev, newCondition]);
  };

  const updateWhereCondition = (conditionId, field, value) => {
    setWhereConditions(prev => prev.map(condition => 
      condition.id === conditionId ? { ...condition, [field]: value } : condition
    ));
  };

  const removeWhereCondition = (conditionId) => {
    setWhereConditions(prev => prev.filter(condition => condition.id !== conditionId));
  };

  const generateQuery = () => {
    if (queryData.tables.length === 0) {
      return;
    }

    // SELECT clause
    const selectColumns = selectedColumns
      .filter(col => col.selected !== false)
      .map(col => `${col.table}.${col.column}`)
      .join(', ') || '*';

    let query = `SELECT ${selectColumns}`;

    // FROM clause
    query += `\nFROM ${queryData.tables[0]}`;

    // JOIN clauses
    joins.forEach(join => {
      if (join.leftColumn && join.rightColumn) {
        query += `\n${join.type} ${join.rightTable} ON ${join.leftTable}.${join.leftColumn} = ${join.rightTable}.${join.rightColumn}`;
      }
    });

    // WHERE clause
    if (whereConditions.length > 0) {
      const conditions = whereConditions
        .filter(condition => condition.column && condition.value)
        .map(condition => {
          const value = condition.isParameter 
            ? `{${condition.value}}` 
            : `'${condition.value}'`;
          return `${condition.table}.${condition.column} ${condition.operator} ${value}`;
        });
      
      if (conditions.length > 0) {
        query += `\nWHERE ${conditions.join(' AND ')}`;
      }
    }

    setQueryData(prev => ({ ...prev, queryText: query }));
  };

  const handleSave = async () => {
    try {
      setLoading(true);
      if (isEditMode) {
        await apiService.updateQuery(queryData.queryNumber, queryData);
      } else {
        await apiService.saveQuery(queryData);
      }
      navigate('/');
    } catch (error) {
      setError('Failed to save query');
    } finally {
      setLoading(false);
    }
  };

  const handleExecute = async () => {
    try {
      setLoading(true);
      // Create mock parameters for execution
      const mockParameters = {};
      const paramMatches = queryData.queryText.match(/\{(\w+)\}/g);
      if (paramMatches) {
        paramMatches.forEach(match => {
          const paramName = match.slice(1, -1);
          mockParameters[paramName] = `sample_${paramName}`;
        });
      }

      const result = await apiService.executeQuery(queryData.queryNumber || 999, mockParameters);
      setExecutionResult(result);
      setExecuteDialogOpen(true);
    } catch (error) {
      setError('Failed to execute query');
    } finally {
      setLoading(false);
    }
  };

  const getColumnTypeColor = (type) => {
    if (type.includes('int') || type.includes('serial')) return 'primary';
    if (type.includes('varchar') || type.includes('text')) return 'secondary';
    if (type.includes('boolean')) return 'success';
    if (type.includes('timestamp') || type.includes('date')) return 'warning';
    return 'default';
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Typography variant="h4">
          {isEditMode ? `Edit Fetch #${queryNumber}` : 'Create New Fetch'}
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Left Panel - Table Selection & Structure */}
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 2, mb: 2 }}>
            <Typography variant="h6" gutterBottom>
              Table Selection
            </Typography>
            
            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel>Add Table</InputLabel>
              <Select
                value=""
                onChange={(e) => handleTableSelect(e.target.value)}
              >
                {tables.map((table) => (
                  <MenuItem key={table.name} value={table.name}>
                    {table.name} ({table.columns.length} columns)
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <Box sx={{ mb: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Selected Tables:
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {queryData.tables.map((table) => (
                  <Chip
                    key={table}
                    label={table}
                    onDelete={() => removeTable(table)}
                    color="primary"
                    variant="outlined"
                  />
                ))}
              </Box>
            </Box>
          </Paper>

          {selectedTableDetails && (
            <Paper sx={{ p: 2 }}>
              <Typography variant="h6" gutterBottom>
                <TableChart sx={{ mr: 1, verticalAlign: 'middle' }} />
                {selectedTable} Structure
              </Typography>
              
              <List dense sx={{ maxHeight: 300, overflow: 'auto' }}>
                {selectedTableDetails.columns.map((column) => (
                  <ListItem 
                    key={column.name} 
                    button
                    onClick={() => toggleColumnSelection(column.name)}
                    sx={{ 
                      py: 0.5,
                      bgcolor: selectedColumns.find(col => 
                        col.column === column.name && col.table === selectedTable && col.selected
                      ) ? 'action.selected' : 'transparent'
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 32 }}>
                      {column.isPrimaryKey ? (
                        <Key color="warning" fontSize="small" />
                      ) : (
                        <Storage color="action" fontSize="small" />
                      )}
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="body2">
                            {column.name}
                          </Typography>
                          <Chip
                            label={column.type}
                            size="small"
                            color={getColumnTypeColor(column.type)}
                            variant="outlined"
                          />
                        </Box>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            </Paper>
          )}
        </Grid>

        {/* Right Panel - Query Configuration */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 2, mb: 2 }}>
            <Typography variant="h6" gutterBottom>
              Fetch Details
            </Typography>
            
            <TextField
              fullWidth
              label="Fetch Description"
              value={queryData.description}
              onChange={(e) => setQueryData(prev => ({ ...prev, description: e.target.value }))}
              sx={{ mb: 2 }}
            />

            {/* Joins Configuration */}
            {queryData.tables.length >= 2 && (
              <Accordion sx={{ mb: 2 }}>
                <AccordionSummary expandIcon={<ExpandMore />}>
                  <Typography variant="subtitle1">
                    Joins ({joins.length})
                  </Typography>
                </AccordionSummary>
                <AccordionDetails>
                  <Button
                    startIcon={<Add />}
                    onClick={addJoin}
                    sx={{ mb: 2 }}
                  >
                    Add Join
                  </Button>
                  {joins.map((join) => (
                    <Box key={join.id} sx={{ mb: 2, p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
                      <Grid container spacing={2} alignItems="center">
                        <Grid item xs={2}>
                          <FormControl fullWidth size="small">
                            <InputLabel>Type</InputLabel>
                            <Select
                              value={join.type}
                              onChange={(e) => updateJoin(join.id, 'type', e.target.value)}
                            >
                              <MenuItem value="INNER JOIN">INNER</MenuItem>
                              <MenuItem value="LEFT JOIN">LEFT</MenuItem>
                              <MenuItem value="RIGHT JOIN">RIGHT</MenuItem>
                            </Select>
                          </FormControl>
                        </Grid>
                        <Grid item xs={3}>
                          <FormControl fullWidth size="small">
                            <InputLabel>Left Table</InputLabel>
                            <Select
                              value={join.leftTable}
                              onChange={(e) => updateJoin(join.id, 'leftTable', e.target.value)}
                            >
                              {queryData.tables.map(table => (
                                <MenuItem key={table} value={table}>{table}</MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                        </Grid>
                        <Grid item xs={3}>
                          <TextField
                            size="small"
                            label="Left Column"
                            value={join.leftColumn}
                            onChange={(e) => updateJoin(join.id, 'leftColumn', e.target.value)}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={3}>
                          <TextField
                            size="small"
                            label="Right Column"
                            value={join.rightColumn}
                            onChange={(e) => updateJoin(join.id, 'rightColumn', e.target.value)}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={1}>
                          <IconButton onClick={() => removeJoin(join.id)} color="error">
                            <DeleteIcon />
                          </IconButton>
                        </Grid>
                      </Grid>
                    </Box>
                  ))}
                </AccordionDetails>
              </Accordion>
            )}

            {/* Where Conditions */}
            <Accordion sx={{ mb: 2 }}>
              <AccordionSummary expandIcon={<ExpandMore />}>
                <Typography variant="subtitle1">
                  Where Conditions ({whereConditions.length})
                </Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Button
                  startIcon={<Add />}
                  onClick={addWhereCondition}
                  sx={{ mb: 2 }}
                >
                  Add Condition
                </Button>
                {whereConditions.map((condition) => (
                  <Box key={condition.id} sx={{ mb: 2, p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
                    <Grid container spacing={2} alignItems="center">
                      <Grid item xs={3}>
                        <FormControl fullWidth size="small">
                          <InputLabel>Table</InputLabel>
                          <Select
                            value={condition.table}
                            onChange={(e) => updateWhereCondition(condition.id, 'table', e.target.value)}
                          >
                            {queryData.tables.map(table => (
                              <MenuItem key={table} value={table}>{table}</MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      </Grid>
                      <Grid item xs={3}>
                        <TextField
                          size="small"
                          label="Column"
                          value={condition.column}
                          onChange={(e) => updateWhereCondition(condition.id, 'column', e.target.value)}
                          fullWidth
                        />
                      </Grid>
                      <Grid item xs={2}>
                        <FormControl fullWidth size="small">
                          <InputLabel>Operator</InputLabel>
                          <Select
                            value={condition.operator}
                            onChange={(e) => updateWhereCondition(condition.id, 'operator', e.target.value)}
                          >
                            <MenuItem value="=">=</MenuItem>
                            <MenuItem value="!=">!=</MenuItem>
                            <MenuItem value=">">&gt;</MenuItem>
                            <MenuItem value="<">&lt;</MenuItem>
                            <MenuItem value=">=">&gt;=</MenuItem>
                            <MenuItem value="<=">&lt;=</MenuItem>
                            <MenuItem value="LIKE">LIKE</MenuItem>
                          </Select>
                        </FormControl>
                      </Grid>
                      <Grid item xs={3}>
                        <TextField
                          size="small"
                          label="Value/Parameter"
                          value={condition.value}
                          onChange={(e) => updateWhereCondition(condition.id, 'value', e.target.value)}
                          fullWidth
                          placeholder="value or {paramName}"
                        />
                      </Grid>
                      <Grid item xs={1}>
                        <IconButton onClick={() => removeWhereCondition(condition.id)} color="error">
                          <DeleteIcon />
                        </IconButton>
                      </Grid>
                    </Grid>
                  </Box>
                ))}
              </AccordionDetails>
            </Accordion>

            {/* Generated SQL */}
            <Typography variant="subtitle1" gutterBottom>
              Generated SQL:
            </Typography>
            <Box sx={{ height: 300, border: 1, borderColor: 'divider', borderRadius: 1, mb: 2 }}>
              <Editor
                height="300px"
                defaultLanguage="sql"
                value={queryData.queryText}
                onChange={(value) => setQueryData(prev => ({ ...prev, queryText: value }))}
                options={{
                  minimap: { enabled: false },
                  scrollBeyondLastLine: false,
                  fontSize: 14
                }}
              />
            </Box>

            {/* Action Buttons */}
            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button
                variant="contained"
                startIcon={<Save />}
                onClick={handleSave}
                disabled={!queryData.queryText || !queryData.description || loading}
              >
                {isEditMode ? 'Update Fetch' : 'Save Fetch'}
              </Button>
              <Button
                variant="outlined"
                startIcon={<PlayArrow />}
                onClick={handleExecute}
                disabled={!queryData.queryText || loading}
              >
                Test Execute
              </Button>
              {loading && <CircularProgress size={24} />}
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Execute Results Dialog */}
      <Dialog open={executeDialogOpen} onClose={() => setExecuteDialogOpen(false)} maxWidth="lg" fullWidth>
        <DialogTitle>Fetch Execution Result</DialogTitle>
        <DialogContent>
          {executionResult && (
            <Box sx={{ 
              maxHeight: 400, 
              overflow: 'auto',
              backgroundColor: '#1e1e1e',
              color: '#fff',
              padding: 2,
              borderRadius: 1,
              fontFamily: 'monospace',
              fontSize: '0.875rem'
            }}>
              <pre>{JSON.stringify(executionResult, null, 2)}</pre>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setExecuteDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default QueryEditor;