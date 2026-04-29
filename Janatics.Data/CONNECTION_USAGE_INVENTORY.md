# Connection Usage Inventory - KATCRUDServices.Core

## Complete Inventory of Connection Usage Locations

### 1. Data Provider Layer

#### PostgreSqlProvider.cs
- **GetConnectionAsync()** (Line 21)
  - **Pattern**: `new NpgsqlConnection(_connectionString)`
  - **Disposal**: Caller responsible
  - **Usage**: Every database operation entry point
  - **Issue**: No connection reuse, no pooling optimization

- **ExecuteQueryAsync()** (Line 97)
  - **Pattern**: `transaction?.Connection ?? await GetConnectionAsync()`
  - **Disposal**: Conditional - only if no transaction
  - **Usage**: Query operations
  - **Issue**: Inconsistent disposal pattern

- **BulkInsertAsync()** (Line 261)
  - **Pattern**: Uses transaction connection with casting
  - **Disposal**: Not responsible (uses existing connection)
  - **Usage**: Bulk insert operations
  - **Issue**: Direct casting to NpgsqlConnection

### 2. Service Layer

#### TransactionService.cs

##### Main Transaction Processing
- **ProcessTransactionAsync()** (Line 95)
  - **Pattern**: `await _transactionDataProvider.GetConnectionAsync()`
  - **Disposal**: Manual in finally block (`connection?.Close(); connection?.Dispose()`)
  - **Scope**: Entire transaction lifecycle
  - **Issue**: Not using `await using`, manual disposal

##### Child Record Processing
- **CreateChildRecordAsync()** (Indirect)
  - **Pattern**: Uses transaction connection from parent
  - **Disposal**: Parent transaction handles
  - **Scope**: Individual child record creation
  - **Issue**: May trigger additional connections through data provider

##### Model Binding Operations
- **CreateMainRecordModelBindingAsync()** (Indirect)
  - **Pattern**: Uses transaction connection
  - **Disposal**: Parent transaction handles
  - **Scope**: Main record creation in model binding mode
  - **Issue**: Relies on parent connection management

#### TransactionService.Bulk.cs

##### Bulk Transaction Processing
- **ProcessBulkEntityTransactionsAsync()** (Line 129)
  - **Pattern**: `await _transactionDataProvider.GetConnectionAsync()`
  - **Disposal**: Manual in finally block
  - **Scope**: Entire bulk operation for one entity type
  - **Issue**: Separate connection per entity type

##### Bulk Insert Processing
- **ProcessBulkInsertsAsync()** (Uses transaction connection)
  - **Pattern**: Uses provided transaction connection
  - **Disposal**: Not responsible
  - **Scope**: Bulk insert operation
  - **Issue**: None (proper pattern)

#### ValidationService.cs
- **ValidateAsync()** (Line 101)
  - **Pattern**: `await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Not explicitly shown (potential leak)
  - **Scope**: Validation query execution
  - **Issue**: Separate connection for validation, unclear disposal

#### AutoNumberService.cs

##### Sequence Operations
- **GenerateAutoNumberAsync()** (Line 56)
  - **Pattern**: `await dataProvider.GetConnectionAsync()`
  - **Disposal**: Not explicitly shown
  - **Scope**: Single sequence value generation
  - **Issue**: New connection per auto-number generation

- **GetCurrentSequenceValueAsync()** (Line 108)
  - **Pattern**: `await dataProvider.GetConnectionAsync()`
  - **Disposal**: Not explicitly shown
  - **Scope**: Current sequence value retrieval
  - **Issue**: New connection per sequence query

- **ResetSequenceAsync()** (Line 159)
  - **Pattern**: `await dataProvider.GetConnectionAsync()`
  - **Disposal**: Not explicitly shown
  - **Scope**: Sequence reset operation
  - **Issue**: New connection per reset operation

#### BackgroundTriggerService.cs
- **ProcessTriggersAsync()** (Line 389)
  - **Pattern**: `using var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Using statement (proper)
  - **Scope**: Background trigger processing
  - **Issue**: Separate connection for background operations

### 3. Repository Layer

#### DagTriggerRepository.cs

##### CRUD Operations
- **CreateTriggerAsync()** (Line 117)
  - **Pattern**: `using var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Using statement
  - **Scope**: Single trigger creation
  - **Issue**: Cannot participate in larger transactions

- **UpdateTriggerAsync()** (Line 207)
  - **Pattern**: `using var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Using statement
  - **Scope**: Single trigger update
  - **Issue**: Cannot participate in larger transactions

- **GetTriggerLogsAsync()** (Line 301)
  - **Pattern**: `using var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Using statement
  - **Scope**: Trigger log retrieval
  - **Issue**: Cannot share connection with related operations

#### SelectOptionRepository.cs

##### CRUD Operations
- **GetSelectOptionsAsync()** (Line 101)
  - **Pattern**: `var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Not explicitly shown (potential leak)
  - **Scope**: Select option retrieval
  - **Issue**: Unclear disposal pattern

- **CreateSelectionGroupAsync()** (Line 185)
  - **Pattern**: `var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Transaction disposal handles connection
  - **Scope**: Selection group creation
  - **Issue**: Cannot share with parent operations

- **UpdateSelectionGroupAsync()** (Line 300)
  - **Pattern**: `var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Transaction disposal handles connection
  - **Scope**: Selection group update
  - **Issue**: Cannot share with parent operations

- **DeleteSelectionGroupAsync()** (Line 332)
  - **Pattern**: `var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Transaction disposal handles connection
  - **Scope**: Selection group deletion
  - **Issue**: Cannot share with parent operations

- **CreateSelectOptionAsync()** (Line 602)
  - **Pattern**: `var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Transaction disposal handles connection
  - **Scope**: Select option creation
  - **Issue**: Cannot share with parent operations

- **UpdateSelectOptionAsync()** (Line 634)
  - **Pattern**: `var connection = await _dataProvider.GetConnectionAsync()`
  - **Disposal**: Transaction disposal handles connection
  - **Scope**: Select option update
  - **Issue**: Cannot share with parent operations

## Connection Lifecycle Analysis

### Connection Creation Points
1. **PostgreSqlProvider.GetConnectionAsync()** - Primary creation point
2. **Every repository method** - Individual connections
3. **Every service operation** - Separate connections
4. **Validation operations** - Additional connections
5. **Auto-number generation** - Per-sequence connections

### Connection Disposal Patterns

#### Proper Disposal (Using Statements)
- DagTriggerRepository methods
- BackgroundTriggerService
- Some PostgreSqlProvider methods

#### Manual Disposal (Finally Blocks)
- TransactionService.ProcessTransactionAsync()
- TransactionService.ProcessBulkEntityTransactionsAsync()

#### Unclear/Missing Disposal
- ValidationService operations
- AutoNumberService operations
- Some SelectOptionRepository methods

### Connection Sharing Opportunities

#### Current Sharing (Limited)
- Transaction operations use same connection for main record and transaction scope
- Bulk operations share connection within entity group

#### Missing Sharing Opportunities
- Parent-child record operations
- Repository methods within transactions
- Validation + main operation
- Auto-number generation + record creation
- Related repository operations

## Connection Pool Impact Assessment

### High-Impact Operations
1. **Complex transactions with child records**: 3-8 connections
2. **Bulk operations with multiple entities**: 2-10 connections per entity
3. **Operations with validation**: +1 connection per validation
4. **Operations with auto-numbers**: +1 connection per sequence

### Medium-Impact Operations
1. **Simple CRUD operations**: 1-2 connections
2. **Repository operations**: 1 connection each
3. **Background trigger processing**: 1 connection per batch

### Low-Impact Operations
1. **Read-only queries**: 1 connection
2. **Single record operations**: 1 connection

## Critical Connection Leak Risks

### High Risk
1. **ValidationService** - No visible disposal
2. **AutoNumberService** - No visible disposal
3. **SelectOptionRepository** - Inconsistent disposal patterns

### Medium Risk
1. **Exception scenarios** - Manual disposal may fail
2. **Nested operations** - Complex disposal chains
3. **Background operations** - Long-running connections

### Low Risk
1. **Using statement patterns** - Automatic disposal
2. **Simple operations** - Clear lifecycle

## Recommendations Summary

### Immediate Actions Required
1. **Audit all connection disposal** - Ensure proper cleanup
2. **Implement connection sharing** - Reduce connection multiplication
3. **Add connection monitoring** - Track usage and leaks
4. **Standardize disposal patterns** - Use `await using` consistently

### Architecture Changes Needed
1. **Operation scope pattern** - Share connections across related operations
2. **Repository parameter support** - Accept external connections
3. **Connection factory pattern** - Centralized connection management
4. **Health monitoring** - Pool metrics and leak detection

This inventory provides the complete picture of connection usage across KATCRUDServices.Core and forms the basis for implementing the connection optimization patterns from KATReaderService.Core.