import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  IconButton,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  Fab,
  Tooltip
} from '@mui/material';
import {
  Edit,
  Delete,
  Add,
  PlayArrow,
  FilterList,
  Visibility
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

import { apiService } from '../services/api';

const QueryManager = () => {
  const navigate = useNavigate();
  const [queries, setQueries] = useState([]);
  const [tables, setTables] = useState([]);
  const [filteredQueries, setFilteredQueries] = useState([]);
  const [selectedTable, setSelectedTable] = useState('');

  const [viewDialogOpen, setViewDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [currentQuery, setCurrentQuery] = useState(null);
  const [executionResult, setExecutionResult] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    filterQueries();
  }, [queries, selectedTable]); // filterQueries is defined inline, so it's safe to omit

  const loadData = async () => {
    try {
      setLoading(true);
      const [queriesData, tablesData] = await Promise.all([
        apiService.getQueries(),
        apiService.getTables()
      ]);
      setQueries(queriesData);
      setTables(tablesData);
    } catch (error) {
      setError('Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const filterQueries = () => {
    if (!selectedTable) {
      setFilteredQueries(queries);
    } else {
      const filtered = queries.filter(query => 
        query.tables && query.tables.includes(selectedTable)
      );
      setFilteredQueries(filtered);
    }
  };

  const handleEdit = (query) => {
    navigate(`/editor/${query.queryNumber}`);
  };

  const handleView = (query) => {
    setCurrentQuery(query);
    setViewDialogOpen(true);
  };

  const handleDelete = (query) => {
    setCurrentQuery(query);
    setDeleteDialogOpen(true);
  };



  const handleConfirmDelete = async () => {
    try {
      setLoading(true);
      await apiService.deleteQuery(currentQuery.queryNumber);
      await loadData();
      setDeleteDialogOpen(false);
      setCurrentQuery(null);
      setError('');
    } catch (error) {
      setError('Failed to delete query');
    } finally {
      setLoading(false);
    }
  };

  const handleExecuteQuery = async (query) => {
    try {
      setLoading(true);
      // For demo purposes, create mock parameters
      const mockParameters = {};
      
      // Extract parameter names from query text
      const paramMatches = query.queryText.match(/\{(\w+)\}/g);
      if (paramMatches) {
        paramMatches.forEach(match => {
          const paramName = match.slice(1, -1); // Remove { }
          mockParameters[paramName] = `sample_${paramName}`;
        });
      }

      const result = await apiService.executeQuery(query.queryNumber, mockParameters);
      setExecutionResult(result);
      setCurrentQuery(query);
      setViewDialogOpen(true);
    } catch (error) {
      setError('Failed to execute query');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">
          Fetch Manager
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel>Filter by Table</InputLabel>
            <Select
              value={selectedTable}
              onChange={(e) => setSelectedTable(e.target.value)}
              startAdornment={<FilterList sx={{ mr: 1 }} />}
            >
              <MenuItem value="">All Tables</MenuItem>
              {tables.map((table) => (
                <MenuItem key={table.name} value={table.name}>
                  {table.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Query #</TableCell>
              <TableCell>Description</TableCell>
              <TableCell>Tables</TableCell>
              <TableCell>Query Preview</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredQueries.map((query) => (
              <TableRow key={query.queryNumber} hover>
                <TableCell>
                  <Typography variant="h6" color="primary">
                    {query.queryNumber}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body1">
                    {query.description}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {query.tables?.map((table) => (
                      <Chip
                        key={table}
                        label={table}
                        size="small"
                        variant="outlined"
                        color="primary"
                      />
                    ))}
                  </Box>
                </TableCell>
                <TableCell>
                  <Typography
                    variant="body2"
                    sx={{
                      fontFamily: 'monospace',
                      maxWidth: 300,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap'
                    }}
                  >
                    {query.queryText}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                    <Tooltip title="Execute Query">
                      <IconButton
                        size="small"
                        color="success"
                        onClick={() => handleExecuteQuery(query)}
                      >
                        <PlayArrow />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="View Details">
                      <IconButton
                        size="small"
                        color="info"
                        onClick={() => handleView(query)}
                      >
                        <Visibility />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit Query">
                      <IconButton
                        size="small"
                        color="primary"
                        onClick={() => handleEdit(query)}
                      >
                        <Edit />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete Query">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDelete(query)}
                      >
                        <Delete />
                      </IconButton>
                    </Tooltip>
                  </Box>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Add New Query Button */}
      <Box sx={{ position: 'fixed', bottom: 16, right: 16 }}>
        <Fab
          color="primary"
          aria-label="add"
          onClick={() => navigate('/editor')}
        >
          <Add />
        </Fab>
      </Box>



      {/* View Dialog */}
      <Dialog open={viewDialogOpen} onClose={() => setViewDialogOpen(false)} maxWidth="lg" fullWidth>
        <DialogTitle>
          Query #{currentQuery?.queryNumber} - {currentQuery?.description}
        </DialogTitle>
        <DialogContent>
          <Typography variant="subtitle2" gutterBottom>
            SQL Query:
          </Typography>
          <Box sx={{ 
            p: 2, 
            bgcolor: 'grey.100', 
            borderRadius: 1, 
            fontFamily: 'monospace', 
            fontSize: '0.875rem',
            mb: 2,
            maxHeight: 200,
            overflow: 'auto'
          }}>
            {currentQuery?.queryText}
          </Box>
          
          {executionResult && (
            <>
              <Typography variant="subtitle2" gutterBottom>
                Execution Result (JSON):
              </Typography>
              <Box sx={{ 
                maxHeight: 400, 
                overflow: 'auto', 
                border: 1, 
                borderColor: 'divider', 
                borderRadius: 1, 
                p: 2,
                backgroundColor: '#1e1e1e',
                color: '#fff',
                fontFamily: 'monospace',
                fontSize: '0.875rem'
              }}>
                <pre>{JSON.stringify(executionResult, null, 2)}</pre>
              </Box>
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setViewDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete Query #{currentQuery?.queryNumber}?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {currentQuery?.description}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleConfirmDelete} color="error" variant="contained" disabled={loading}>
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default QueryManager;