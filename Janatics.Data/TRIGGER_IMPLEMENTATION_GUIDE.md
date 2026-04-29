# DAG Trigger Implementation Guide

## Overview

This implementation adds support for creating additional system records when a main record is created, using two different trigger types:

1. **DAGS** - Calls external DAG service (existing functionality)
2. **DirectInsert** - Creates child records directly using InsertConfig (new functionality)

## New Fields in kat_dag_trigger Table

### TriggerType
- **Values**: "DAGS" or "DirectInsert"
- **Default**: "DAGS"
- **Purpose**: Determines how the trigger is processed

### InsertConfig
- **Type**: JSONB
- **Purpose**: Contains configuration for creating child records
- **Format**: Similar to renprops structure with |RENGUID| placeholders
- **Benefits**: JSON validation, indexing, and query capabilities

## Usage Examples

### 1. DirectInsert Trigger Configuration

```sql
INSERT INTO kat_dag_triggers (
    id, 
    table_name, 
    trigger, 
    dag_id, 
    enabled, 
    trigger_type, 
    insert_config
) VALUES (
    gen_random_uuid(),
    'brf_case',
    'Insert',
    '00000000-0000-0000-0000-000000000000', -- Not used for DirectInsert
    true,
    'DirectInsert',
    '{
        "brf_caseparticipant": [
            {
                "brf_partyroleid": "aa9c1d54-2b0a-4571-a24a-8f4a502d00ec",
                "brf_name": "System Generated Participant",
                "brf_address": "Auto Generated Address",
                "brf_email": "system@example.com",
                "brf_mobilenumber": "+91 00000 00000",
                "brf_caseid": "|RENGUID|"
            }
        ],
        "brf_case_notes": [
            {
                "note_text": "Case created automatically",
                "created_date": "|Todaydate|",
                "case_id": "|RENGUID|"
            }
        ]
    }'::jsonb
);
```

### 2. DAGS Trigger Configuration (Existing)

```sql
INSERT INTO kat_dag_triggers (
    id, 
    table_name, 
    trigger, 
    dag_id, 
    enabled, 
    trigger_type
) VALUES (
    gen_random_uuid(),
    'brf_case',
    'Insert',
    'your-actual-dag-id-here',
    true,
    'DAGS'
);
```

## How It Works

### Transaction Flow

1. **Main Record Creation**: Record is created in the main table
2. **Background Processing**: After successful transaction commit, triggers are processed in background
3. **Trigger Execution**: Based on TriggerType:
   - **DAGS**: Calls external DAG service
   - **DirectInsert**: Creates child records using InsertConfig

### Placeholder Replacement & Type Conversion

The system automatically replaces placeholders and converts data types:

**Placeholders:**
- `|RENGUID|` → Actual record ID from the main table
- `|Todaydate|` → Current date/time

**Automatic Type Conversion:**
- **UUID Fields**: Fields containing "id", "guid", ending with "_id", or starting with "id_" are automatically converted to UUID type
- **Integers**: Numeric strings are converted to integers
- **Decimals**: Decimal strings are converted to decimal type
- **Booleans**: "true"/"false" strings are converted to boolean
- **Dates**: Valid date strings are converted to DateTime
- **Strings**: Default fallback for other values

**UUID Field Examples:**
- `case_id`, `user_id`, `party_role_id` → Converted to UUID
- `recordid`, `id_field`, `field_id` → Converted to UUID
- `name`, `description`, `status` → Remain as strings

### Background Processing

- Uses `BackgroundTriggerService` instead of Hangfire
- Processes jobs every 1 second
- Supports delayed execution
- Handles both trigger types

## Configuration Steps

### 1. Configure DAG Trigger Settings

Add the following to your `appsettings.json`:

```json
{
  "DagTrigger": {
    "ApiBaseUrl": "https://your-ktaiflow-instance.com/",
    "BearerToken": "your-bearer-token-here"
  }
}
```

**Configuration Options:**
- `DagTrigger:ApiBaseUrl` - KTAiFlow API base URL
- `DagTrigger:BearerToken` - Authentication token for API calls
- `DagTrigger:ApiKey` - Alternative to BearerToken
- `KTAiFlow:*` - Alternative section name for same settings

**Priority Order:**
1. `DagTrigger:*` settings
2. `KTAiFlow:*` settings  
3. Default values

### 2. Run Database Migration

```sql
-- Execute the migration script
\i Migrations/AddTriggerTypeAndInsertConfig.sql
```

### 3. Create Trigger Configuration

Use the examples above to create trigger configurations for your tables.

### 4. Test the Implementation

Create a record in a table that has triggers configured and verify:
- Main record is created successfully
- Background jobs are enqueued
- Child records are created (for DirectInsert triggers)
- DAG services are called (for DAGS triggers)

## Monitoring

### Check Background Job Status

```csharp
var (queuedJobs, isProcessing) = BackgroundTriggerService.GetStatus();
Console.WriteLine($"Queued Jobs: {queuedJobs}, Processing: {isProcessing}");
```

### View Trigger Logs

```sql
SELECT * FROM kat_dag_trigger_logs 
WHERE table_name = 'your_table_name' 
ORDER BY created_at DESC 
LIMIT 10;
```

## JSONB Query Capabilities

With JSONB storage, you can perform advanced queries on trigger configurations:

```sql
-- Find triggers that create specific table records
SELECT * FROM kat_dag_trigger 
WHERE insert_config ? 'brf_caseparticipant';

-- Query nested JSON values
SELECT table_name, insert_config->'brf_caseparticipant'->0->>'brf_name' as participant_name
FROM kat_dag_trigger 
WHERE trigger_type = 'DirectInsert';

-- Find triggers with specific field values
SELECT * FROM kat_dag_trigger 
WHERE insert_config @> '{"brf_caseparticipant": [{"brf_name": "System Generated Participant"}]}';

-- Update specific JSON fields
UPDATE kat_dag_trigger 
SET insert_config = jsonb_set(insert_config, '{brf_caseparticipant,0,brf_email}', '"newemail@example.com"')
WHERE id = 'your-trigger-id';
```

## Benefits

1. **No Hangfire Dependency**: Uses simple background processing
2. **Flexible Configuration**: JSONB-based configuration for child records
3. **JSON Validation**: Automatic validation of JSON structure
4. **Advanced Queries**: Full PostgreSQL JSONB query capabilities
5. **Indexing Support**: GIN indexes for fast JSON queries
6. **Automatic Placeholder Replacement**: Handles RENGUID and date placeholders
7. **Background Processing**: Doesn't impact main transaction performance
8. **Dual Mode Support**: Both DAG calls and direct inserts

## Example Use Case

When a case is created in `brf_case` table:
1. Main case record is inserted
2. Background trigger creates:
   - Default case participants
   - Initial case notes
   - System audit records
   - Notification records

All child records automatically reference the main case via the replaced `|RENGUID|` placeholder.