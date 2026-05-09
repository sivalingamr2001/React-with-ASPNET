import { useState, useEffect } from 'react';
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
  IconButton,
  Card,
  CardContent,
  CardActions,
  Divider,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Tabs,
  Tab,
  Switch,
  FormControlLabel,
  Accordion,
  AccordionSummary,
  AccordionDetails
} from '@mui/material';
import {
  Add,
  Delete,
  Save,
  PlayArrow,
  ContentCopy,
  ExpandMore,
  FilterList
} from '@mui/icons-material';
import Editor from '@monaco-editor/react';

const OPERATORS = [
  { value: 'eq', label: 'Equals (=)' },
  { value: 'neq', label: 'Not Equals (≠)' },
  { value: 'gt', label: 'Greater Than (>)' },
  { value: 'lt', label: 'Less Than (<)' },
  { value: 'gte', label: 'Greater or Equal (≥)' },
  { value: 'lte', label: 'Less or Equal (≤)' },
  { value: 'contains', label: 'Contains' },
  { value: 'startswith', label: 'Starts With' },
  { value: 'endswith', label: 'Ends With' },
  { value: 'like', label: 'Like' },
  { value: 'in', label: 'In (list)' },
  { value: 'notin', label: 'Not In (list)' },
  { value: 'null', label: 'Is Null' },
  { value: 'notnull', label: 'Is Not Null' }
];

const JOIN_TYPES = [
  { value: 'inner', label: 'INNER JOIN' },
  { value: 'left', label: 'LEFT JOIN' },
  { value: 'right', label: 'RIGHT JOIN' }
];

const FetchJsonQueryBuilder = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [entity, setEntity] = useState('');
  const [alias, setAlias] = useState('');
  const [columns, setColumns] = useState([]);
  const [joins, setJoins] = useState([]);
  const [filter, setFilter] = useState({ operator: 'and', conditions: [], groups: [] });
  const [orderBy, setOrderBy] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [generatedJson, setGeneratedJson] = useState('');
  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const [queryNumber, setQueryNumber] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    generateFetchJson();
  }, [entity, alias, columns, joins, filter, orderBy, page, pageSize]);

  const generateFetchJson = () => {
    if (!entity) {
      setGeneratedJson('');
      return;
    }

    const fetchJsonQuery = {
      entity,
      ...(alias && { alias }),
      columns: columns.filter(c => c.trim()),
      ...(joins.length > 0 && { joins: joins.filter(j => j.entity && j.alias) }),
      ...(hasConditionsOrGroups(filter) && { filter }),
      ...(orderBy.length > 0 && { orderBy: orderBy.filter(o => o.field) }),
      page,
      pageSize
    };

    setGeneratedJson(JSON.stringify(fetchJsonQuery, null, 2));
  };

  const hasConditionsOrGroups = (filterObj) => {
    return (filterObj.conditions && filterObj.conditions.length > 0) ||
           (filterObj.groups && filterObj.groups.length > 0);
  };

  // Column Management
  const addColumn = () => {
    setColumns([...columns, '']);
  };

  const updateColumn = (index, value) => {
    const newColumns = [...columns];
    newColumns[index] = value;
    setColumns(newColumns);
  };

  const removeColumn = (index) => {
    setColumns(columns.filter((_, i) => i !== index));
  };

  // Join Management
  const addJoin = () => {
    setJoins([...joins, {
      entity: '',
      alias: '',
      type: 'inner',
      isIntersect: false,
      from: { entityAlias: alias || entity, field: '' },
      toField: '',
      columns: []
    }]);
  };

  const updateJoin = (index, field, value) => {
    const newJoins = [...joins];
    if (field.includes('.')) {
      const [parent, child] = field.split('.');
      newJoins[index][parent][child] = value;
    } else {
      newJoins[index][field] = value;
    }
    setJoins(newJoins);
  };

  const removeJoin = (index) => {
    setJoins(joins.filter((_, i) => i !== index));
  };

  const addJoinColumn = (joinIndex) => {
    const newJoins = [...joins];
    newJoins[joinIndex].columns.push('');
    setJoins(newJoins);
  };

  const updateJoinColumn = (joinIndex, columnIndex, value) => {
    const newJoins = [...joins];
    newJoins[joinIndex].columns[columnIndex] = value;
    setJoins(newJoins);
  };

  const removeJoinColumn = (joinIndex, columnIndex) => {
    const newJoins = [...joins];
    newJoins[joinIndex].columns = newJoins[joinIndex].columns.filter((_, i) => i !== columnIndex);
    setJoins(newJoins);
  };

  // Filter Management (Conditions)
  const addCondition = (filterObj = filter) => {
    const newCondition = {
      field: '',
      operator: 'eq',
      value: null
    };
    filterObj.conditions = [...(filterObj.conditions || []), newCondition];
    setFilter({ ...filter });
  };

  const updateCondition = (conditionIndex, field, value, filterObj = filter) => {
    filterObj.conditions[conditionIndex][field] = value;
    setFilter({ ...filter });
  };

  const removeCondition = (conditionIndex, filterObj = filter) => {
    filterObj.conditions = filterObj.conditions.filter((_, i) => i !== conditionIndex);
    setFilter({ ...filter });
  };

  // Filter Management (Groups)
  const addFilterGroup = (parentFilter = filter) => {
    const newGroup = {
      operator: 'and',
      conditions: [],
      groups: []
    };
    parentFilter.groups = [...(parentFilter.groups || []), newGroup];
    setFilter({ ...filter });
  };

  const updateFilterGroupOperator = (groupIndex, operator, parentFilter = filter) => {
    parentFilter.groups[groupIndex].operator = operator;
    setFilter({ ...filter });
  };

  const removeFilterGroup = (groupIndex, parentFilter = filter) => {
    parentFilter.groups = parentFilter.groups.filter((_, i) => i !== groupIndex);
    setFilter({ ...filter });
  };

  // OrderBy Management
  const addOrderBy = () => {
    setOrderBy([...orderBy, { field: '', direction: 'asc' }]);
  };

  const updateOrderBy = (index, field, value) => {
    const newOrderBy = [...orderBy];
    newOrderBy[index][field] = value;
    setOrderBy(newOrderBy);
  };

  const removeOrderBy = (index) => {
    setOrderBy(orderBy.filter((_, i) => i !== index));
  };

  // Actions
  const handleCopyJson = () => {
    navigator.clipboard.writeText(generatedJson);
    setSuccess('JSON copied to clipboard!');
    setTimeout(() => setSuccess(''), 3000);
  };

  const handleSave = async () => {
    try {
      // Here you would call your API to save the query
      const queryData = {
        queryNumber: parseInt(queryNumber),
        fetchJson: generatedJson,
        isFetchJson: true,
        description
      };
      
      console.log('Saving query:', queryData);
      setSuccess('Query saved successfully!');
      setSaveDialogOpen(false);
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError('Failed to save query: ' + err.message);
    }
  };

  const handleLoadExample = () => {
    setEntity('leaverequest');
    setAlias('a');
    setColumns(['leaverefno']);
    setJoins([
      {
        entity: 'employees',
        alias: 'b',
        type: 'inner',
        isIntersect: false,
        from: { entityAlias: 'a', field: 'employeeid' },
        toField: 'employeeid',
        columns: ['employeename']
      },
      {
        entity: 'leavetypes',
        alias: 'c',
        type: 'inner',
        isIntersect: false,
        from: { entityAlias: 'a', field: 'leavetypeid' },
        toField: 'leavetypeid',
        columns: ['leavetypename']
      }
    ]);
    setFilter({
      operator: 'and',
      conditions: [
        { field: 'a.status', operator: 'eq', value: 'approved' }
      ],
      groups: []
    });
    setOrderBy([{ field: 'a.leaverefno', direction: 'desc' }]);
  };

  // Render Filter Conditions (Recursive for nested groups)
  const renderFilterConditions = (filterObj, parentPath = '') => {
    return (
      <Box>
        {/* Operator Selection */}
        <Box sx={{ mb: 2, display: 'flex', alignItems: 'center', gap: 2 }}>
          <Typography variant="subtitle2">Logical Operator:</Typography>
          <FormControl size="small" sx={{ minWidth: 120 }}>
            <Select
              value={filterObj.operator}
              onChange={(e) => {
                filterObj.operator = e.target.value;
                setFilter({ ...filter });
              }}
            >
              <MenuItem value="and">AND</MenuItem>
              <MenuItem value="or">OR</MenuItem>
            </Select>
          </FormControl>
        </Box>

        {/* Conditions */}
        {filterObj.conditions && filterObj.conditions.map((condition, index) => (
          <Card key={`condition-${parentPath}-${index}`} sx={{ mb: 2, bgcolor: 'grey.50' }}>
            <CardContent>
              <Grid container spacing={2} alignItems="center">
                <Grid item xs={12} sm={3}>
                  <TextField
                    size="small"
                    label="Field"
                    value={condition.field}
                    onChange={(e) => updateCondition(index, 'field', e.target.value, filterObj)}
                    fullWidth
                    placeholder="e.g., a.status"
                  />
                </Grid>
                <Grid item xs={12} sm={3}>
                  <FormControl fullWidth size="small">
                    <InputLabel>Operator</InputLabel>
                    <Select
                      value={condition.operator}
                      onChange={(e) => updateCondition(index, 'operator', e.target.value, filterObj)}
                    >
                      {OPERATORS.map(op => (
                        <MenuItem key={op.value} value={op.value}>{op.label}</MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={5}>
                  {!['null', 'notnull'].includes(condition.operator) && (
                    <TextField
                      size="small"
                      label="Value"
                      value={condition.value || ''}
                      onChange={(e) => updateCondition(index, 'value', e.target.value, filterObj)}
                      fullWidth
                      placeholder="value or {paramName}"
                      helperText="Use {paramName} for parameters"
                    />
                  )}
                </Grid>
                <Grid item xs={12} sm={1}>
                  <IconButton
                    size="small"
                    color="error"
                    onClick={() => removeCondition(index, filterObj)}
                  >
                    <Delete />
                  </IconButton>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        ))}

        {/* Nested Groups */}
        {filterObj.groups && filterObj.groups.map((group, groupIndex) => (
          <Accordion key={`group-${parentPath}-${groupIndex}`} sx={{ mb: 2 }}>
            <AccordionSummary expandIcon={<ExpandMore />}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, width: '100%' }}>
                <FilterList />
                <Typography>Nested Group ({group.operator.toUpperCase()})</Typography>
                <Box sx={{ flexGrow: 1 }} />
                <IconButton
                  size="small"
                  color="error"
                  onClick={(e) => {
                    e.stopPropagation();
                    removeFilterGroup(groupIndex, filterObj);
                  }}
                >
                  <Delete />
                </IconButton>
              </Box>
            </AccordionSummary>
            <AccordionDetails>
              {renderFilterConditions(group, `${parentPath}-${groupIndex}`)}
            </AccordionDetails>
          </Accordion>
        ))}

        {/* Add Buttons */}
        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<Add />}
            onClick={() => addCondition(filterObj)}
          >
            Add Condition
          </Button>
          <Button
            size="small"
            variant="outlined"
            startIcon={<FilterList />}
            onClick={() => addFilterGroup(filterObj)}
          >
            Add Nested Group (AND/OR)
          </Button>
        </Box>
      </Box>
    );
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        FetchJSON Query Builder
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess('')}>{success}</Alert>}

      <Button variant="outlined" onClick={handleLoadExample} sx={{ mb: 2 }}>
        Load Example Query
      </Button>

      <Grid container spacing={3}>
        {/* Left Panel - Query Builder */}
        <Grid item xs={12} md={7}>
          <Paper sx={{ p: 2 }}>
            <Tabs value={activeTab} onChange={(e, v) => setActiveTab(v)} sx={{ mb: 2 }}>
              <Tab label="Entity & Columns" />
              <Tab label="Joins" />
              <Tab label="Filters (AND/OR)" />
              <Tab label="Order & Pagination" />
            </Tabs>

            {/* Tab 0: Entity & Columns */}
            {activeTab === 0 && (
              <Box>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      label="Entity (Table Name)"
                      value={entity}
                      onChange={(e) => setEntity(e.target.value)}
                      fullWidth
                      required
                      placeholder="e.g., leaverequest"
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      label="Alias"
                      value={alias}
                      onChange={(e) => setAlias(e.target.value)}
                      fullWidth
                      placeholder="e.g., a"
                    />
                  </Grid>
                </Grid>

                <Divider sx={{ my: 3 }} />

                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Typography variant="h6">Columns</Typography>
                  <Button startIcon={<Add />} onClick={addColumn}>Add Column</Button>
                </Box>

                {columns.map((column, index) => (
                  <Box key={index} sx={{ display: 'flex', gap: 2, mb: 2 }}>
                    <TextField
                      label={`Column ${index + 1}`}
                      value={column}
                      onChange={(e) => updateColumn(index, e.target.value)}
                      fullWidth
                      placeholder="e.g., leaverefno or a.leaverefno"
                    />
                    <IconButton color="error" onClick={() => removeColumn(index)}>
                      <Delete />
                    </IconButton>
                  </Box>
                ))}
              </Box>
            )}

            {/* Tab 1: Joins */}
            {activeTab === 1 && (
              <Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Typography variant="h6">Joins</Typography>
                  <Button startIcon={<Add />} onClick={addJoin}>Add Join</Button>
                </Box>

                {joins.map((join, joinIndex) => (
                  <Card key={joinIndex} sx={{ mb: 2 }}>
                    <CardContent>
                      <Grid container spacing={2}>
                        <Grid item xs={12} sm={6}>
                          <TextField
                            label="Join Entity"
                            value={join.entity}
                            onChange={(e) => updateJoin(joinIndex, 'entity', e.target.value)}
                            fullWidth
                            required
                          />
                        </Grid>
                        <Grid item xs={12} sm={6}>
                          <TextField
                            label="Alias"
                            value={join.alias}
                            onChange={(e) => updateJoin(joinIndex, 'alias', e.target.value)}
                            fullWidth
                            required
                          />
                        </Grid>
                        <Grid item xs={12} sm={4}>
                          <FormControl fullWidth>
                            <InputLabel>Join Type</InputLabel>
                            <Select
                              value={join.type}
                              onChange={(e) => updateJoin(joinIndex, 'type', e.target.value)}
                            >
                              {JOIN_TYPES.map(jt => (
                                <MenuItem key={jt.value} value={jt.value}>{jt.label}</MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                        </Grid>
                        <Grid item xs={12} sm={4}>
                          <TextField
                            label="From Entity Alias"
                            value={join.from.entityAlias}
                            onChange={(e) => updateJoin(joinIndex, 'from.entityAlias', e.target.value)}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={12} sm={4}>
                          <TextField
                            label="From Field"
                            value={join.from.field}
                            onChange={(e) => updateJoin(joinIndex, 'from.field', e.target.value)}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={12} sm={6}>
                          <TextField
                            label="To Field"
                            value={join.toField}
                            onChange={(e) => updateJoin(joinIndex, 'toField', e.target.value)}
                            fullWidth
                          />
                        </Grid>
                        <Grid item xs={12} sm={6}>
                          <FormControlLabel
                            control={
                              <Switch
                                checked={join.isIntersect}
                                onChange={(e) => updateJoin(joinIndex, 'isIntersect', e.target.checked)}
                              />
                            }
                            label="Is Intersect (Many-to-Many)"
                          />
                        </Grid>

                        <Grid item xs={12}>
                          <Divider sx={{ my: 1 }} />
                          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                            <Typography variant="subtitle2">Join Columns</Typography>
                            <Button size="small" onClick={() => addJoinColumn(joinIndex)}>Add Column</Button>
                          </Box>
                          {join.columns.map((col, colIndex) => (
                            <Box key={colIndex} sx={{ display: 'flex', gap: 1, mb: 1 }}>
                              <TextField
                                size="small"
                                value={col}
                                onChange={(e) => updateJoinColumn(joinIndex, colIndex, e.target.value)}
                                fullWidth
                                placeholder="column name"
                              />
                              <IconButton size="small" color="error" onClick={() => removeJoinColumn(joinIndex, colIndex)}>
                                <Delete />
                              </IconButton>
                            </Box>
                          ))}
                        </Grid>
                      </Grid>
                    </CardContent>
                    <CardActions>
                      <Button size="small" color="error" onClick={() => removeJoin(joinIndex)}>
                        Remove Join
                      </Button>
                    </CardActions>
                  </Card>
                ))}
              </Box>
            )}

            {/* Tab 2: Filters */}
            {activeTab === 2 && (
              <Box>
                <Typography variant="h6" gutterBottom>
                  Filters (Conditions & Groups)
                </Typography>
                <Alert severity="info" sx={{ mb: 2 }}>
                  Build complex filters with AND/OR logic. You can nest groups for advanced conditions.
                </Alert>
                {renderFilterConditions(filter)}
              </Box>
            )}

            {/* Tab 3: Order & Pagination */}
            {activeTab === 3 && (
              <Box>
                <Typography variant="h6" gutterBottom>Order By</Typography>
                <Button startIcon={<Add />} onClick={addOrderBy} sx={{ mb: 2 }}>
                  Add Order By
                </Button>

                {orderBy.map((order, index) => (
                  <Box key={index} sx={{ display: 'flex', gap: 2, mb: 2 }}>
                    <TextField
                      label="Field"
                      value={order.field}
                      onChange={(e) => updateOrderBy(index, 'field', e.target.value)}
                      fullWidth
                      placeholder="e.g., a.created_date"
                    />
                    <FormControl sx={{ minWidth: 120 }}>
                      <InputLabel>Direction</InputLabel>
                      <Select
                        value={order.direction}
                        onChange={(e) => updateOrderBy(index, 'direction', e.target.value)}
                      >
                        <MenuItem value="asc">ASC</MenuItem>
                        <MenuItem value="desc">DESC</MenuItem>
                      </Select>
                    </FormControl>
                    <IconButton color="error" onClick={() => removeOrderBy(index)}>
                      <Delete />
                    </IconButton>
                  </Box>
                ))}

                <Divider sx={{ my: 3 }} />

                <Typography variant="h6" gutterBottom>Pagination</Typography>
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <TextField
                      label="Page"
                      type="number"
                      value={page}
                      onChange={(e) => setPage(parseInt(e.target.value) || 1)}
                      fullWidth
                      inputProps={{ min: 1 }}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <TextField
                      label="Page Size"
                      type="number"
                      value={pageSize}
                      onChange={(e) => setPageSize(parseInt(e.target.value) || 50)}
                      fullWidth
                      inputProps={{ min: 1, max: 200 }}
                    />
                  </Grid>
                </Grid>
              </Box>
            )}
          </Paper>
        </Grid>

        {/* Right Panel - Generated JSON */}
        <Grid item xs={12} md={5}>
          <Paper sx={{ p: 2, position: 'sticky', top: 20 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6">Generated FetchJSON</Typography>
              <Box>
                <IconButton onClick={handleCopyJson} disabled={!generatedJson}>
                  <ContentCopy />
                </IconButton>
                <Button
                  variant="contained"
                  startIcon={<Save />}
                  onClick={() => setSaveDialogOpen(true)}
                  disabled={!generatedJson}
                  sx={{ ml: 1 }}
                >
                  Save
                </Button>
              </Box>
            </Box>

            <Box sx={{ height: 600, border: 1, borderColor: 'divider', borderRadius: 1 }}>
              <Editor
                height="600px"
                defaultLanguage="json"
                value={generatedJson}
                onChange={setGeneratedJson}
                options={{
                  minimap: { enabled: false },
                  scrollBeyondLastLine: false,
                  fontSize: 13,
                  readOnly: false
                }}
              />
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Save Dialog */}
      <Dialog open={saveDialogOpen} onClose={() => setSaveDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Save FetchJSON Query</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Query Number"
            type="number"
            fullWidth
            variant="outlined"
            value={queryNumber}
            onChange={(e) => setQueryNumber(e.target.value)}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Description"
            fullWidth
            variant="outlined"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            sx={{ mb: 2 }}
          />
          <Typography variant="subtitle2" gutterBottom>
            SQL to be generated:
          </Typography>
          <Alert severity="info">
            INSERT INTO query_definitions (query_number, fetch_json, is_fetch_json, description) 
            VALUES ({queryNumber}, '{generatedJson}'::jsonb, TRUE, '{description}');
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSaveDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSave} variant="contained" disabled={!queryNumber || !description}>
            Save Query
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default FetchJsonQueryBuilder;
