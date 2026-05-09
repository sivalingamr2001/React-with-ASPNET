import React, { useState, useEffect } from 'react';
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
  ListItemButton,

  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions
} from '@mui/material';
import { Add, Save, PlayArrow, TableChart } from '@mui/icons-material';
import Editor from '@monaco-editor/react';

import { apiService } from '../services/api';

const QueryBuilder = () => {
  const [tables, setTables] = useState([]);
  const [selectedTables, setSelectedTables] = useState([]);
  const [selectedColumns, setSelectedColumns] = useState([]);
  const [joins, setJoins] = useState([]);
  const [whereConditions, setWhereConditions] = useState([]);
  const [queryText, setQueryText] = useState('');
  const [queryDescription, setQueryDescription] = useState('');
  const [parameters, setParameters] = useState({});
  const [executionResult, setExecutionResult] = useState(null);
  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadTables();
  }, []);

  useEffect(() => {
    generateQuery();
  }, [selectedTables, selectedColumns, joins, whereConditions]);

  const loadTables = async () => {
    try {
      const tablesData = await apiService.getTables();
      setTables(tablesData);
    } catch (error) {
      setError('Failed to load tables');
    }
  };

  const handleTableSelect = async (tableName) => {
    if (!selectedTables.includes(tableName)) {
      const newSelectedTables = [...selectedTables, tableName];
      setSelectedTables(newSelectedTables);
      
      // Load columns for the selected table
      const columns = await apiService.getTableColumns(tableName);
      const tableColumns = columns.map(col => ({
        table: tableName,
        column: col.name,
        type: col.type,
        isPrimaryKey: col.isPrimaryKey
      }));
      
      setSelectedColumns(prev => [...prev, ...tableColumns]);
    }
  };

  const removeTable = (tableName) => {
    setSelectedTables(prev => prev.filter(t => t !== tableName));
    setSelectedColumns(prev => prev.filter(col => col.table !== tableName));
    setJoins(prev => prev.filter(join => 
      join.leftTable !== tableName && join.rightTable !== tableName
    ));
  };

  const addJoin = () => {
    if (selectedTables.length >= 2) {
      const newJoin = {
        id: Date.now(),
        type: 'INNER JOIN',
        leftTable: selectedTables[0],
        leftColumn: '',
        rightTable: selectedTables[1],
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
      table: selectedTables[0] || '',
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
    if (selectedTables.length === 0) {
      setQueryText('');
      return;
    }

    // SELECT clause
    const selectColumns = selectedColumns
      .filter(col => col.selected !== false)
      .map(col => `${col.table}.${col.column}`)
      .join(', ') || '*';

    let query = `SELECT ${selectColumns}`;

    // FROM clause
    query += `\nFROM ${selectedTables[0]}`;
    
    // Add table aliases
    const tableAliases = {};
    selectedTables.forEach((table, index) => {
      tableAliases[table] = table.charAt(0) + (index > 0 ? index : '');
    });

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

    setQueryText(query);
  };

  const handleSaveQuery = async () => {
    try {
      setLoading(true);
      const queryData = {
        queryText,
        description: queryDescription,
        tables: selectedTables
      };
      
      await apiService.saveQuery(queryData);
      setSaveDialogOpen(false);
      setError('');
      // Show success message or redirect
    } catch (error) {
      setError('Failed to save query');
    } finally {
      setLoading(false);
    }
  };

  const handleExecuteQuery = async () => {
    try {
      setLoading(true);
      // For now, just show the query as JSON
      const result = {
        query: queryText,
        parameters: parameters,
        timestamp: new Date().toISOString()
      };
      setExecutionResult(result);
    } catch (error) {
      setError('Failed to execute query');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Visual Query Builder
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Tables Panel */}
        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 2, height: 'fit-content' }}>
            <Typography variant="h6" gutterBottom>
              <TableChart sx={{ mr: 1, verticalAlign: 'middle' }} />
              Available Tables
            </Typography>
            <List dense>
              {tables.map((table) => (
                <ListItem key={table.name} disablePadding>
                  <ListItemButton 
                    onClick={() => handleTableSelect(table.name)}
                    disabled={selectedTables.includes(table.name)}
                  >
                    <ListItemText 
                      primary={table.name}
                      secondary={`${table.columns.length} columns`}
                    />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>

        {/* Query Builder Panel */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Query Configuration
            </Typography>

            {/* Selected Tables */}
            <Box sx={{ mb: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Selected Tables:
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {selectedTables.map((table) => (
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

            {/* Joins */}
            {selectedTables.length >= 2 && (
              <Box sx={{ mb: 2 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                  <Typography variant="subtitle2">
                    Joins:
                  </Typography>
                  <Button size="small" onClick={addJoin} startIcon={<Add />}>
                    Add Join
                  </Button>
                </Box>
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
                            {selectedTables.map(table => (
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
                        <Button size="small" onClick={() => removeJoin(join.id)} color="error">
                          ×
                        </Button>
                      </Grid>
                    </Grid>
                  </Box>
                ))}
              </Box>
            )}

            {/* Where Conditions */}
            <Box sx={{ mb: 2 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                <Typography variant="subtitle2">
                  Where Conditions:
                </Typography>
                <Button size="small" onClick={addWhereCondition} startIcon={<Add />}>
                  Add Condition
                </Button>
              </Box>
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
                          {selectedTables.map(table => (
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
                        placeholder="value or paramName"
                      />
                    </Grid>
                    <Grid item xs={1}>
                      <Button size="small" onClick={() => removeWhereCondition(condition.id)} color="error">
                        ×
                      </Button>
                    </Grid>
                  </Grid>
                </Box>
              ))}
            </Box>

            {/* Generated Query */}
            <Box sx={{ mb: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Generated SQL:
              </Typography>
              <Box sx={{ height: 200, border: 1, borderColor: 'divider', borderRadius: 1 }}>
                <Editor
                  height="200px"
                  defaultLanguage="sql"
                  value={queryText}
                  onChange={setQueryText}
                  options={{
                    minimap: { enabled: false },
                    scrollBeyondLastLine: false,
                    fontSize: 14
                  }}
                />
              </Box>
            </Box>

            {/* Actions */}
            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button
                variant="contained"
                startIcon={<PlayArrow />}
                onClick={handleExecuteQuery}
                disabled={!queryText || loading}
              >
                Execute Query
              </Button>
              <Button
                variant="outlined"
                startIcon={<Save />}
                onClick={() => setSaveDialogOpen(true)}
                disabled={!queryText}
              >
                Save Query
              </Button>
            </Box>
          </Paper>
        </Grid>

        {/* Results Panel */}
        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Query Output (JSON)
            </Typography>
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
          </Paper>
        </Grid>
      </Grid>

      {/* Save Dialog */}
      <Dialog open={saveDialogOpen} onClose={() => setSaveDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Save Query</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Query Description"
            fullWidth
            variant="outlined"
            value={queryDescription}
            onChange={(e) => setQueryDescription(e.target.value)}
            sx={{ mb: 2 }}
          />
          <Typography variant="subtitle2" gutterBottom>
            Query Preview:
          </Typography>
          <Box sx={{ p: 2, bgcolor: 'grey.100', borderRadius: 1, fontFamily: 'monospace', fontSize: '0.875rem' }}>
            {queryText}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSaveDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSaveQuery} variant="contained" disabled={loading || !queryDescription}>
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default QueryBuilder;