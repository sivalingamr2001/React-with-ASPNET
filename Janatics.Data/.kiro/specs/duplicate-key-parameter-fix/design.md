# Design Document: Duplicate Key Parameter Fix

## Overview

This design addresses the duplicate key parameter issue in the BackgroundTriggerService where the same parameter key is being added multiple times to the parameters dictionary during child record creation. The solution focuses on robust parameter building with duplicate prevention, field name normalization, and comprehensive error handling.

## Architecture

The fix will be implemented within the existing BackgroundTriggerService architecture:

```
BackgroundTriggerService
├── ProcessDirectInsert()
├── CreateChildRecordDirect()
├── BuildParametersFromRecordData() [NEW]
├── NormalizeFieldName() [NEW]
└── LogParameterConflicts() [NEW]
```

## Components and Interfaces

### Enhanced Parameter Building

**BuildParametersFromRecordData Method**
- Input: Dictionary<string, object> recordData
- Output: (Dictionary<string, object> parameters, Dictionary<string, object> uniqueFields)
- Responsibility: Safe parameter building with duplicate prevention

**NormalizeFieldName Method**
- Input: string fieldName
- Output: string normalizedName
- Responsibility: Consistent field name normalization

**LogParameterConflicts Method**
- Input: List<string> conflicts, string tableName
- Output: void
- Responsibility: Detailed conflict logging

## Data Models

### Parameter Building Result
```csharp
public class ParameterBuildResult
{
    public Dictionary<string, object> Parameters { get; set; }
    public Dictionary<string, object> UniqueFields { get; set; }
    public List<string> Conflicts { get; set; }
    public bool HasConflicts => Conflicts.Any();
}
```

### Field Conflict Information
```csharp
public class FieldConflict
{
    public string OriginalKey { get; set; }
    public string NormalizedKey { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After reviewing all identified properties, several can be consolidated to eliminate redundancy:

- Properties 1.1 and 1.5 both test duplicate prevention in dictionaries - can be combined into one comprehensive property
- Properties 2.1, 2.2, 2.3, and 2.4 all test logging behavior - can be combined into a comprehensive logging property
- Properties 3.1, 3.2, 3.3, and 3.4 all test error resilience - can be combined into one resilience property

**Property 1: Parameter dictionary uniqueness**
*For any* record data input, building parameters should result in dictionaries (both parameters and unique fields) with no duplicate keys
**Validates: Requirements 1.1, 1.5**

**Property 2: Last value precedence**
*For any* record data with duplicate field names, the final parameter value should match the last occurrence in the input data
**Validates: Requirements 1.2**

**Property 3: Case-insensitive field normalization**
*For any* field names that differ only in case, they should be normalized to the same dictionary key
**Validates: Requirements 1.3**

**Property 4: Value integrity during normalization**
*For any* field value, the converted value in the parameters dictionary should maintain the correct type and content regardless of key normalization
**Validates: Requirements 1.4**

**Property 5: Comprehensive conflict logging**
*For any* parameter building operation with conflicts, appropriate log messages should be generated containing conflict details, normalization information, final counts, and unique field identification
**Validates: Requirements 2.1, 2.2, 2.3, 2.4**

**Property 6: Error resilience**
*For any* parameter building operation with partial failures, the system should continue processing with valid parameters, proceed with database operations after resolving conflicts, skip problematic fields, and maintain transaction integrity
**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## Error Handling

### Parameter Building Errors
- **Duplicate Key Detection**: Identify and resolve duplicate parameter keys
- **Field Name Conflicts**: Handle case-sensitive and normalized name conflicts
- **Value Conversion Failures**: Skip invalid fields while preserving valid ones
- **Memory Management**: Prevent excessive memory usage during parameter building

### Logging Strategy
- **Conflict Resolution**: Log all duplicate key resolutions with before/after values
- **Field Normalization**: Log original and normalized field names
- **Error Recovery**: Log skipped fields and reasons for exclusion
- **Performance Metrics**: Log parameter building time and conflict counts

### Transaction Safety
- **Partial Failures**: Continue processing even when some parameters fail
- **Data Integrity**: Ensure valid parameters are not corrupted by invalid ones
- **Rollback Prevention**: Avoid transaction failures due to parameter issues
- **Graceful Degradation**: Reduce functionality rather than complete failure

## Testing Strategy

### Unit Testing Approach
- Test parameter building with various input combinations
- Test field name normalization edge cases
- Test value conversion for different data types
- Test error handling for malformed input data

### Property-Based Testing Approach
Using **NUnit** with **FsCheck.NUnit** for property-based testing:
- Configure each property-based test to run a minimum of 100 iterations
- Generate random record data with potential conflicts
- Test parameter building across diverse input scenarios
- Verify logging behavior with mock logger verification

**Property-based testing requirements:**
- Each correctness property will be implemented by a single property-based test
- Tests will be tagged with comments referencing the design document properties
- Tag format: **Feature: duplicate-key-parameter-fix, Property {number}: {property_text}**
- Mock loggers will be used to verify logging behavior
- Random data generators will create realistic field name and value combinations

### Integration Testing
- Test with real database connections and transactions
- Test with actual DirectInsert trigger configurations
- Test memory usage under high-conflict scenarios
- Test performance with large parameter sets

## Implementation Details

### Parameter Building Algorithm
1. **Initialize Collections**: Create empty parameters and unique fields dictionaries
2. **Process Each Field**: Iterate through record data fields
3. **Normalize Field Names**: Convert to consistent case and format
4. **Detect Conflicts**: Check for existing keys in dictionaries
5. **Resolve Duplicates**: Use last value strategy with logging
6. **Convert Values**: Apply appropriate type conversion
7. **Track Unique Fields**: Identify fields with unique constraints
8. **Log Results**: Record conflicts, counts, and resolution details

### Field Name Normalization Rules
- Convert to lowercase for consistency
- Trim whitespace from field names
- Handle special characters consistently
- Preserve original names in conflict logs

### Value Conversion Strategy
- Maintain existing type conversion logic
- Add error handling for conversion failures
- Skip fields that cannot be converted
- Log conversion errors for debugging

### Memory Management
- Limit conflict tracking to prevent memory leaks
- Clear temporary collections after processing
- Use efficient data structures for large parameter sets
- Monitor memory usage during parameter building