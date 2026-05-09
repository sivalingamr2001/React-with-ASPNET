import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Card,
  CardContent,
  CardActions,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemButton,
  Chip,
  Divider,
  Alert,
  Badge,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  CircularProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails
} from '@mui/material';
import {
  TableChart,
  Key,
  Storage,
  QueryBuilder,
  Visibility,
  ExpandMore,
  Search
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { apiService } from '../services/api';

const TableExplorer = () => {
  const navigate = useNavigate();
  const [tables, setTables] = useState([]);
  const [selectedTable, setSelectedTable] = useState('');
  const [selectedTableDetails, setSelectedTableDetails] = useState(null);
  const [tableQueries, setTableQueries] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingQueries, setLoadingQueries] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadTables();
  }, []);

  const loadTables = async () => {
    try {
      setLoading(true);
      const tablesData = await apiService.getTables();
      setTables(tablesData);
    } catch (error) {
      setError('Failed to load tables');
    } finally {
      setLoading(false);
    }
  };

  const handleTableSelect = async (tableName) => {
    if (tableName === selectedTable) return;
    
    setSelectedTable(tableName);
    setSelectedTableDetails(null);
    setTableQueries([]);
    
    if (!tableName) return;

    try {
      setLoadingQueries(true);
      
      // Get table details
      const table = tables.find(t => t.name === tableName);
      setSelectedTableDetails(table);
      
      // Load queries for the selected table
      const queries = await apiService.getQueriesByTable(tableName);
      setTableQueries(queries);
    } catch (error) {
      setError(`Failed to load data for table: ${tableName}`);
    } finally {
      setLoadingQueries(false);
    }
  };

  const getColumnTypeColor = (type) => {
    if (type.includes('int') || type.includes('serial')) return 'primary';
    if (type.includes('varchar') || type.includes('text')) return 'secondary';
    if (type.includes('boolean')) return 'success';
    if (type.includes('timestamp') || type.includes('date')) return 'warning';
    return 'default';
  };

  const handleCreateQuery = () => {
    if (selectedTable) {
      navigate('/builder', { state: { selectedTable } });
    }
  };

  const handleViewAllQueries = () => {
    if (selectedTable) {
      navigate('/', { state: { filterTable: selectedTable } });
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Database Tables Explorer
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Table Selection Panel */}
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 2, height: 'fit-content' }}>
            <Typography variant="h6" gutterBottom>
              <Search sx={{ mr: 1, verticalAlign: 'middle' }} />
              Select Table
            </Typography>
            
            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel>Choose a table</InputLabel>
              <Select
                value={selectedTable}
                onChange={(e) => handleTableSelect(e.target.value)}
                disabled={loading}
              >
                <MenuItem value="">
                  <em>Select a table...</em>
                </MenuItem>
                {tables.map((table) => (
                  <MenuItem key={table.name} value={table.name}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', width: '100%' }}>
                      <Typography>{table.name}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {table.columns.length} cols
                      </Typography>
                    </Box>
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            {loading && (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 2 }}>
                <CircularProgress size={24} />
                <Typography variant="body2" sx={{ ml: 1 }}>
                  Loading tables...
                </Typography>
              </Box>
            )}

            {tables.length === 0 && !loading && (
              <Alert severity="info">
                No tables found. Make sure your database connection is configured correctly.
              </Alert>
            )}
          </Paper>
        </Grid>

        {/* Table Details Panel */}
        <Grid item xs={12} md={8}>
          {selectedTable && selectedTableDetails ? (
            <Paper sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <TableChart color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">
                  {selectedTable}
                </Typography>
                <Chip 
                  label={`${selectedTableDetails.columns.length} columns`}
                  size="small"
                  sx={{ ml: 2 }}
                />
              </Box>

              {/* Table Columns */}
              <Accordion defaultExpanded>
                <AccordionSummary expandIcon={<ExpandMore />}>
                  <Typography variant="subtitle1">
                    Table Structure
                  </Typography>
                </AccordionSummary>
                <AccordionDetails>
                  <List dense>
                    {selectedTableDetails.columns.map((column) => (
                      <ListItem key={column.name} sx={{ py: 0.5 }}>
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
                              <Typography variant="body2" component="span">
                                {column.name}
                              </Typography>
                              <Chip
                                label={column.type}
                                size="small"
                                color={getColumnTypeColor(column.type)}
                                variant="outlined"
                              />
                              {column.isPrimaryKey && (
                                <Chip
                                  label="PK"
                                  size="small"
                                  color="warning"
                                  variant="filled"
                                />
                              )}
                            </Box>
                          }
                        />
                      </ListItem>
                    ))}
                  </List>
                </AccordionDetails>
              </Accordion>

              {/* Related Queries */}
              <Accordion sx={{ mt: 2 }}>
                <AccordionSummary expandIcon={<ExpandMore />}>
                  <Typography variant="subtitle1">
                    Related Queries
                    {loadingQueries ? (
                      <CircularProgress size={16} sx={{ ml: 1 }} />
                    ) : (
                      <Badge badgeContent={tableQueries.length} color="secondary" sx={{ ml: 1 }}>
                        <QueryBuilder fontSize="small" />
                      </Badge>
                    )}
                  </Typography>
                </AccordionSummary>
                <AccordionDetails>
                  {loadingQueries ? (
                    <Box sx={{ display: 'flex', justifyContent: 'center', p: 2 }}>
                      <CircularProgress size={24} />
                      <Typography variant="body2" sx={{ ml: 1 }}>
                        Loading queries...
                      </Typography>
                    </Box>
                  ) : tableQueries.length > 0 ? (
                    <List dense>
                      {tableQueries.map((query) => (
                        <ListItem key={query.queryNumber} sx={{ py: 1, pl: 0 }}>
                          <ListItemText
                            primary={
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <Chip
                                  label={`#${query.queryNumber}`}
                                  size="small"
                                  color="primary"
                                  variant="outlined"
                                />
                                <Typography variant="body2">
                                  {query.description}
                                </Typography>
                              </Box>
                            }
                            secondary={
                              <Typography
                                variant="caption"
                                sx={{
                                  fontFamily: 'monospace',
                                  display: 'block',
                                  overflow: 'hidden',
                                  textOverflow: 'ellipsis',
                                  whiteSpace: 'nowrap',
                                  maxWidth: '100%',
                                  mt: 0.5
                                }}
                              >
                                {query.queryText}
                              </Typography>
                            }
                          />
                        </ListItem>
                      ))}
                    </List>
                  ) : (
                    <Alert severity="info">
                      No queries found for this table. Create your first query!
                    </Alert>
                  )}
                </AccordionDetails>
              </Accordion>

              {/* Action Buttons */}
              <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
                <Button
                  variant="contained"
                  startIcon={<QueryBuilder />}
                  onClick={handleCreateQuery}
                >
                  Create New Query
                </Button>
                {tableQueries.length > 0 && (
                  <Button
                    variant="outlined"
                    startIcon={<Visibility />}
                    onClick={handleViewAllQueries}
                  >
                    View All Queries ({tableQueries.length})
                  </Button>
                )}
              </Box>
            </Paper>
          ) : (
            <Paper sx={{ p: 4, textAlign: 'center', height: 400, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
              <TableChart sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
              <Typography variant="h6" color="text.secondary" gutterBottom>
                Select a table to explore
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Choose a table from the dropdown to view its structure and related queries.
              </Typography>
            </Paper>
          )}
        </Grid>
      </Grid>
    </Box>
  );
};

export default TableExplorer;