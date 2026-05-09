import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  IconButton,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemText,
  ListItemButton,
  Checkbox,
  Grid,
  Card,
  CardContent,
  Divider,
  Alert,
  Tooltip,
  Stack
} from '@mui/material';
import { TreeItem, TreeView } from '@mui/lab';
import {
  Add,
  Delete,
  ExpandMore,
  ChevronRight,
  Save,
  ContentCopy,
  FilterList,
  TableChart,
  AccountTree
} from '@mui/icons-material';

// Types
interface TableInfo {
  name: string;
  columns: ColumnInfo[];
}

interface ColumnInfo {
  name: string;
  type: string;
  isPrimaryKey: boolean;
}

interface FetchJsonJoin {
  entity: string;
  alias: string;
  type: 'inner' | 'left' | 'right';
  isIntersect: boolean;
  from: {
    entityAlias: string;
    field: string;
  };
  toField: string;
  columns: string[];
  children?: FetchJsonJoin[];
}

interface FetchJsonCondition {
  field: string;
  operator: string;
  value: any;
}

interface FetchJsonFilter {
  operator: 'and' | 'or';
  conditions: FetchJsonCondition[];
  groups: FetchJsonFilter[];
}

interface FetchJsonQuery {
  entity: string;
  alias?: string;
  columns: string[];
  joins?: FetchJsonJoin[];
  filter?: FetchJsonFilter;
  orderBy?: Array<{ field: string; direction: 'asc' | 'desc' }>;
  page: number;
  pageSize: number;
}

const OPERATORS = [
  { value: 'eq', label: '=' },
  { value: 'neq', label: '≠' },
  { value: 'gt', label: '>' },
  { value: 'lt', label: '<' },
  { value: 'gte', label: '≥' },
  { value: 'lte', label: '≤' },
  { value: 'contains', label: 'Contains' },
  { value: 'startswith', label: 'Starts With' },
  { value: 'endswith', label: 'Ends With' },
  { value: 'in', label: 'In' },
  { value: 'null', label: 'Is Null' },
  { value: 'notnull', label: 'Is Not Null' }
];

const FetchJsonBuilder: React.FC = () => {
  // State
  const [tables, setTables] = useState<TableInfo[]>([]);
  const [query, setQuery] = useState<FetchJsonQuery>({
    entity: '',
    columns: [],
    page: 1,
    pageSize: 50
  });
  const [generatedJson, setGeneratedJson] = useState('');
  
  // Modal states
  const [columnModalOpen, setColumnModalOpen] = useState(false);
  const [joinModalOpen, setJoinModalOpen] = useState(false);
  const [filterModalOpen, setFilterModalOpen] = useState(false);
  const [currentTable, setCurrentTable] = useState<TableInfo | null>(null);
  const [selectedColumns, setSelectedColumns] = useState<string[]>([]);
  const [currentJoinParent, setCurrentJoinParent] = useState<string>('');
  
  // Alerts
  const [success, setSuccess] = useState('');
  const [error, setError] = useState('');

  // Load tables from API
  useEffect(() => {
    loadTables();
  }, []);

  // Generate JSON whenever query changes
  useEffect(() => {
    generateJson();
  }, [query]);

  const loadTables = async () => {
    try {
      // Mock API call - replace with actual API
      const mockTables: TableInfo[] = [
        {
          name: 'leaverequest',
          columns: [
            { name: 'id', type: 'int', isPrimaryKey: true },
            { name: 'leaverefno', type: 'string', isPrimaryKey: false },
            { name: 'employeeid', type: 'int', isPrimaryKey: false },
            { name: 'leavetypeid', type: 'int', isPrimaryKey: false },
            { name: 'startdate', type: 'date', isPrimaryKey: false },
            { name: 'enddate', type: 'date', isPrimaryKey: false },
            { name: 'status', type: 'string', isPrimaryKey: false }
          ]
        },
        {
          name: 'employees',
          columns: [
            { name: 'employeeid', type: 'int', isPrimaryKey: true },
            { name: 'employeename', type: 'string', isPrimaryKey: false },
            { name: 'email', type: 'string', isPrimaryKey: false },
            { name: 'departmentid', type: 'int', isPrimaryKey: false }
          ]
        },
        {
          name: 'leavetypes',
          columns: [
            { name: 'leavetypeid', type: 'int', isPrimaryKey: true },
            { name: 'leavetypename', type: 'string', isPrimaryKey: false },
            { name: 'maxdays', type: 'int', isPrimaryKey: false }
          ]
        },
        {
          name: 'departments',
          columns: [
            { name: 'departmentid', type: 'int', isPrimaryKey: true },
            { name: 'departmentname', type: 'string', isPrimaryKey: false }
          ]
        }
      ];
      setTables(mockTables);
    } catch (err) {
      setError('Failed to load tables');
    }
  };

  const generateJson = () => {
    const cleanQuery = { ...query };
    if (!cleanQuery.entity) {
      setGeneratedJson('');
      return;
    }
    
    // Remove empty arrays
    if (cleanQuery.joins?.length === 0) delete cleanQuery.joins;
    if (cleanQuery.orderBy?.length === 0) delete cleanQuery.orderBy;
    if (cleanQuery.filter && !hasFilters(cleanQuery.filter)) delete cleanQuery.filter;
    
    setGeneratedJson(JSON.stringify(cleanQuery, null, 2));
  };

  const hasFilters = (filter: FetchJsonFilter): boolean => {
    return (filter.conditions?.length > 0) || (filter.groups?.length > 0);
  };

  // Entity Selection
  const handleEntitySelect = (tableName: string) => {
    const table = tables.find(t => t.name === tableName);
    if (table) {
      setQuery({
        ...query,
        entity: tableName,
        alias: tableName.charAt(0),
        columns: []
      });
    }
  };

  // Column Selection Modal
  const openColumnModal = (tableName: string, isMainEntity: boolean = true) => {
    const table = tables.find(t => t.name === tableName);
    if (table) {
      setCurrentTable(table);
      setSelectedColumns(isMainEntity ? query.columns : []);
      setColumnModalOpen(true);
    }
  };

  const handleColumnSave = () => {
    setQuery({ ...query, columns: selectedColumns });
    setColumnModalOpen(false);
  };

  // Join Management
  const openJoinModal = (parentAlias: string = '') => {
    setCurrentJoinParent(parentAlias || query.alias || query.entity);
    setJoinModalOpen(true);
  };

  const addJoin = (join: FetchJsonJoin) => {
    const newJoins = [...(query.joins || []), join];
    setQuery({ ...query, joins: newJoins });
    setJoinModalOpen(false);
  };

  const removeJoin = (index: number) => {
    const newJoins = query.joins?.filter((_, i) => i !== index);
    setQuery({ ...query, joins: newJoins });
  };

  // Filter Management
  const openFilterModal = () => {
    if (!query.filter) {
      setQuery({
        ...query,
        filter: { operator: 'and', conditions: [], groups: [] }
      });
    }
    setFilterModalOpen(true);
  };

  const addCondition = () => {
    if (query.filter) {
      query.filter.conditions.push({ field: '', operator: 'eq', value: '' });
      setQuery({ ...query });
    }
  };

  const updateCondition = (index: number, field: keyof FetchJsonCondition, value: any) => {
    if (query.filter) {
      query.filter.conditions[index][field] = value;
      setQuery({ ...query });
    }
  };

  const removeCondition = (index: number) => {
    if (query.filter) {
      query.filter.conditions = query.filter.conditions.filter((_, i) => i !== index);
      setQuery({ ...query });
    }
  };

  const addFilterGroup = () => {
    if (query.filter) {
      query.filter.groups.push({ operator: 'and', conditions: [], groups: [] });
      setQuery({ ...query });
    }
  };

  // Order By
  const addOrderBy = () => {
    const newOrderBy = [...(query.orderBy || []), { field: '', direction: 'asc' as const }];
    setQuery({ ...query, orderBy: newOrderBy });
  };

  const updateOrderBy = (index: number, field: 'field' | 'direction', value: any) => {
    if (query.orderBy) {
      query.orderBy[index][field] = value;
      setQuery({ ...query });
    }
  };

  const removeOrderBy = (index: number) => {
    const newOrderBy = query.orderBy?.filter((_, i) => i !== index);
    setQuery({ ...query, orderBy: newOrderBy });
  };

  // Render Join Tree
  const renderJoinTree = (joins: FetchJsonJoin[], parentId: string = 'root') => {
    return joins.map((join, index) => {
      const nodeId = `${parentId}-${index}`;
      return (
        <TreeItem
          key={nodeId}
          nodeId={nodeId}
          label={
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, py: 1 }}>
              <AccountTree fontSize="small" />
              <Typography variant="body2">
                <strong>{join.alias}</strong> ({join.entity})
              </Typography>
              <Chip label={join.type.toUpperCase()} size="small" />
              <Typography variant="caption" color="text.secondary">
                ON {join.from.entityAlias}.{join.from.field} = {join.alias}.{join.toField}
              </Typography>
              <Box sx={{ flexGrow: 1 }} />
              <IconButton size="small" onClick={() => removeJoin(index)}>
                <Delete fontSize="small" />
              </IconButton>
            </Box>
          }
        >
          {join.children && join.children.length > 0 && renderJoinTree(join.children, nodeId)}
        </TreeItem>
      );
    });
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(generatedJson);
    setSuccess('JSON copied to clipboard!');
    setTimeout(() => setSuccess(''), 3000);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        FetchJSON Query Builder
      </Typography>

      {success && <Alert severity="success" sx={{ mb: 2 }}>{success}</Alert>}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Grid container spacing={3}>
        {/* Left Panel - Query Builder */}
        <Grid item xs={12} md={7}>
          {/* Entity Selection */}
          <Paper sx={{ p: 2, mb: 2 }}>
            <Typography variant="h6" gutterBottom>
              1. Select Entity (Table)
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={8}>
                <FormControl fullWidth>
                  <InputLabel>Entity</InputLabel>
                  <Select
                    value={query.entity}
                    onChange={(e) => handleEntitySelect(e.target.value)}
                  >
                    {tables.map(table => (
                      <MenuItem key={table.name} value={table.name}>
                        {table.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={4}>
                <TextField
                  label="Alias"
                  value={query.alias || ''}
                  onChange={(e) => setQuery({ ...query, alias: e.target.value })}
                  fullWidth
                />
              </Grid>
            </Grid>
          </Paper>

          {/* Columns */}
          {query.entity && (
            <Paper sx={{ p: 2, mb: 2 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">2. Select Columns</Typography>
                <Button
                  startIcon={<TableChart />}
                  onClick={() => openColumnModal(query.entity, true)}
                  variant="outlined"
                >
                  Select Columns
                </Button>
              </Box>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {query.columns.map((col, index) => (
                  <Chip
                    key={index}
                    label={col}
                    onDelete={() => {
                      const newCols = query.columns.filter((_, i) => i !== index);
                      setQuery({ ...query, columns: newCols });
                    }}
                  />
                ))}
                {query.columns.length === 0 && (
                  <Typography variant="body2" color="text.secondary">
                    No columns selected
                  </Typography>
                )}
              </Box>
            </Paper>
          )}

          {/* Joins */}
          {query.entity && (
            <Paper sx={{ p: 2, mb: 2 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">3. Joins (Tree View)</Typography>
                <Button
                  startIcon={<Add />}
                  onClick={() => openJoinModal()}
                  variant="outlined"
                >
                  Add Join
                </Button>
              </Box>
              {query.joins && query.joins.length > 0 ? (
                <TreeView
                  defaultCollapseIcon={<ExpandMore />}
                  defaultExpandIcon={<ChevronRight />}
                  sx={{ flexGrow: 1, overflowY: 'auto' }}
                >
                  {renderJoinTree(query.joins)}
                </TreeView>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No joins added
                </Typography>
              )}
            </Paper>
          )}

          {/* Filters */}
          {query.entity && (
            <Paper sx={{ p: 2, mb: 2 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">4. Filters (AND/OR)</Typography>
                <Button
                  startIcon={<FilterList />}
                  onClick={openFilterModal}
                  variant="outlined"
                >
                  Configure Filters
                </Button>
              </Box>
              {query.filter && hasFilters(query.filter) ? (
                <Alert severity="info">
                  {query.filter.conditions.length} condition(s), {query.filter.groups.length} group(s)
                </Alert>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No filters configured
                </Typography>
              )}
            </Paper>
          )}

          {/* Order By & Pagination */}
          {query.entity && (
            <Paper sx={{ p: 2, mb: 2 }}>
              <Typography variant="h6" gutterBottom>5. Order By & Pagination</Typography>
              
              <Box sx={{ mb: 2 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                  <Typography variant="subtitle2">Order By</Typography>
                  <Button size="small" startIcon={<Add />} onClick={addOrderBy}>
                    Add
                  </Button>
                </Box>
                {query.orderBy?.map((order, index) => (
                  <Box key={index} sx={{ display: 'flex', gap: 1, mb: 1 }}>
                    <TextField
                      size="small"
                      label="Field"
                      value={order.field}
                      onChange={(e) => updateOrderBy(index, 'field', e.target.value)}
                      sx={{ flex: 1 }}
                    />
                    <FormControl size="small" sx={{ minWidth: 100 }}>
                      <Select
                        value={order.direction}
                        onChange={(e) => updateOrderBy(index, 'direction', e.target.value)}
                      >
                        <MenuItem value="asc">ASC</MenuItem>
                        <MenuItem value="desc">DESC</MenuItem>
                      </Select>
                    </FormControl>
                    <IconButton size="small" onClick={() => removeOrderBy(index)}>
                      <Delete />
                    </IconButton>
                  </Box>
                ))}
              </Box>

              <Divider sx={{ my: 2 }} />

              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <TextField
                    label="Page"
                    type="number"
                    value={query.page}
                    onChange={(e) => setQuery({ ...query, page: parseInt(e.target.value) || 1 })}
                    fullWidth
                    size="small"
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Page Size"
                    type="number"
                    value={query.pageSize}
                    onChange={(e) => setQuery({ ...query, pageSize: parseInt(e.target.value) || 50 })}
                    fullWidth
                    size="small"
                  />
                </Grid>
              </Grid>
            </Paper>
          )}
        </Grid>

        {/* Right Panel - Generated JSON */}
        <Grid item xs={12} md={5}>
          <Paper sx={{ p: 2, position: 'sticky', top: 20 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6">Generated FetchJSON</Typography>
              <Stack direction="row" spacing={1}>
                <Tooltip title="Copy JSON">
                  <IconButton onClick={handleCopy} disabled={!generatedJson}>
                    <ContentCopy />
                  </IconButton>
                </Tooltip>
                <Button
                  startIcon={<Save />}
                  variant="contained"
                  disabled={!generatedJson}
                >
                  Save
                </Button>
              </Stack>
            </Box>
            <Box
              sx={{
                bgcolor: '#1e1e1e',
                color: '#d4d4d4',
                p: 2,
                borderRadius: 1,
                fontFamily: 'monospace',
                fontSize: '0.875rem',
                maxHeight: '70vh',
                overflow: 'auto',
                whiteSpace: 'pre-wrap'
              }}
            >
              {generatedJson || '// Configure your query to see JSON output'}
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Column Selection Modal */}
      <Dialog open={columnModalOpen} onClose={() => setColumnModalOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          Select Columns from {currentTable?.name}
        </DialogTitle>
        <DialogContent>
          <List>
            {currentTable?.columns.map((column) => (
              <ListItem key={column.name} disablePadding>
                <ListItemButton
                  onClick={() => {
                    const col = `${query.alias || query.entity}.${column.name}`;
                    if (selectedColumns.includes(col)) {
                      setSelectedColumns(selectedColumns.filter(c => c !== col));
                    } else {
                      setSelectedColumns([...selectedColumns, col]);
                    }
                  }}
                >
                  <Checkbox checked={selectedColumns.includes(`${query.alias || query.entity}.${column.name}`)} />
                  <ListItemText
                    primary={column.name}
                    secondary={`${column.type}${column.isPrimaryKey ? ' (PK)' : ''}`}
                  />
                </ListItemButton>
              </ListItem>
            ))}
          </List>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setColumnModalOpen(false)}>Cancel</Button>
          <Button onClick={handleColumnSave} variant="contained">
            Save ({selectedColumns.length} selected)
          </Button>
        </DialogActions>
      </Dialog>

      {/* Join Modal */}
      <JoinModal
        open={joinModalOpen}
        onClose={() => setJoinModalOpen(false)}
        onSave={addJoin}
        tables={tables}
        parentAlias={currentJoinParent}
      />

      {/* Filter Modal */}
      <FilterModal
        open={filterModalOpen}
        onClose={() => setFilterModalOpen(false)}
        filter={query.filter}
        onUpdate={(newFilter) => setQuery({ ...query, filter: newFilter })}
      />
    </Box>
  );
};

// Join Modal Component
interface JoinModalProps {
  open: boolean;
  onClose: () => void;
  onSave: (join: FetchJsonJoin) => void;
  tables: TableInfo[];
  parentAlias: string;
}

const JoinModal: React.FC<JoinModalProps> = ({ open, onClose, onSave, tables, parentAlias }) => {
  const [join, setJoin] = useState<FetchJsonJoin>({
    entity: '',
    alias: '',
    type: 'inner',
    isIntersect: false,
    from: { entityAlias: parentAlias, field: '' },
    toField: '',
    columns: []
  });

  const handleSave = () => {
    if (join.entity && join.alias && join.from.field && join.toField) {
      onSave(join);
      setJoin({
        entity: '',
        alias: '',
        type: 'inner',
        isIntersect: false,
        from: { entityAlias: parentAlias, field: '' },
        toField: '',
        columns: []
      });
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Add Join</DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={6}>
            <FormControl fullWidth>
              <InputLabel>Join Entity</InputLabel>
              <Select
                value={join.entity}
                onChange={(e) => setJoin({ ...join, entity: e.target.value })}
              >
                {tables.map(table => (
                  <MenuItem key={table.name} value={table.name}>{table.name}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={6}>
            <TextField
              label="Alias"
              value={join.alias}
              onChange={(e) => setJoin({ ...join, alias: e.target.value })}
              fullWidth
            />
          </Grid>
          <Grid item xs={6}>
            <FormControl fullWidth>
              <InputLabel>Join Type</InputLabel>
              <Select
                value={join.type}
                onChange={(e) => setJoin({ ...join, type: e.target.value as any })}
              >
                <MenuItem value="inner">INNER</MenuItem>
                <MenuItem value="left">LEFT</MenuItem>
                <MenuItem value="right">RIGHT</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={6}>
            <TextField
              label="From Entity Alias"
              value={join.from.entityAlias}
              onChange={(e) => setJoin({ ...join, from: { ...join.from, entityAlias: e.target.value } })}
              fullWidth
            />
          </Grid>
          <Grid item xs={6}>
            <TextField
              label="From Field"
              value={join.from.field}
              onChange={(e) => setJoin({ ...join, from: { ...join.from, field: e.target.value } })}
              fullWidth
            />
          </Grid>
          <Grid item xs={6}>
            <TextField
              label="To Field"
              value={join.toField}
              onChange={(e) => setJoin({ ...join, toField: e.target.value })}
              fullWidth
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained">Add Join</Button>
      </DialogActions>
    </Dialog>
  );
};

// Filter Modal Component
interface FilterModalProps {
  open: boolean;
  onClose: () => void;
  filter?: FetchJsonFilter;
  onUpdate: (filter: FetchJsonFilter) => void;
}

const FilterModal: React.FC<FilterModalProps> = ({ open, onClose, filter, onUpdate }) => {
  const [localFilter, setLocalFilter] = useState<FetchJsonFilter>(
    filter || { operator: 'and', conditions: [], groups: [] }
  );

  const addCondition = () => {
    localFilter.conditions.push({ field: '', operator: 'eq', value: '' });
    setLocalFilter({ ...localFilter });
  };

  const updateCondition = (index: number, field: keyof FetchJsonCondition, value: any) => {
    localFilter.conditions[index][field] = value;
    setLocalFilter({ ...localFilter });
  };

  const removeCondition = (index: number) => {
    localFilter.conditions = localFilter.conditions.filter((_, i) => i !== index);
    setLocalFilter({ ...localFilter });
  };

  const handleSave = () => {
    onUpdate(localFilter);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Configure Filters</DialogTitle>
      <DialogContent>
        <Box sx={{ mt: 2 }}>
          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel>Logical Operator</InputLabel>
            <Select
              value={localFilter.operator}
              onChange={(e) => setLocalFilter({ ...localFilter, operator: e.target.value as any })}
            >
              <MenuItem value="and">AND</MenuItem>
              <MenuItem value="or">OR</MenuItem>
            </Select>
          </FormControl>

          <Button startIcon={<Add />} onClick={addCondition} sx={{ mb: 2 }}>
            Add Condition
          </Button>

          {localFilter.conditions.map((condition, index) => (
            <Card key={index} sx={{ mb: 2 }}>
              <CardContent>
                <Grid container spacing={2}>
                  <Grid item xs={4}>
                    <TextField
                      label="Field"
                      value={condition.field}
                      onChange={(e) => updateCondition(index, 'field', e.target.value)}
                      fullWidth
                      size="small"
                    />
                  </Grid>
                  <Grid item xs={3}>
                    <FormControl fullWidth size="small">
                      <InputLabel>Operator</InputLabel>
                      <Select
                        value={condition.operator}
                        onChange={(e) => updateCondition(index, 'operator', e.target.value)}
                      >
                        {OPERATORS.map(op => (
                          <MenuItem key={op.value} value={op.value}>{op.label}</MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Grid>
                  <Grid item xs={4}>
                    {!['null', 'notnull'].includes(condition.operator) && (
                      <TextField
                        label="Value"
                        value={condition.value}
                        onChange={(e) => updateCondition(index, 'value', e.target.value)}
                        fullWidth
                        size="small"
                      />
                    )}
                  </Grid>
                  <Grid item xs={1}>
                    <IconButton onClick={() => removeCondition(index)}>
                      <Delete />
                    </IconButton>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          ))}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained">Save Filters</Button>
      </DialogActions>
    </Dialog>
  );
};

export default FetchJsonBuilder;
