-- Migration: Add TriggerType and InsertConfig columns to kat_dag_trigger table
-- Date: 2024-12-17
-- Description: Adds support for DirectInsert trigger type with JSON configuration

-- Step 1: Add trigger_type column with default value
ALTER TABLE kat_dag_trigger 
ADD COLUMN IF NOT EXISTS trigger_type VARCHAR(50) DEFAULT 'DAGS';

-- Step 2: Add insert_config column for JSON configuration
ALTER TABLE kat_dag_trigger 
ADD COLUMN IF NOT EXISTS insert_config JSONB;

-- Step 3: Update existing records to have explicit TriggerType
UPDATE kat_dag_trigger 
SET trigger_type = 'DAGS' 
WHERE trigger_type IS NULL OR trigger_type = '';

-- Step 4: Add check constraint for TriggerType values
ALTER TABLE kat_dag_trigger 
ADD CONSTRAINT IF NOT EXISTS chk_trigger_type 
CHECK (trigger_type IN ('DAGS', 'DirectInsert'));

-- Step 5: Add comments for documentation
COMMENT ON COLUMN kat_dag_trigger.trigger_type IS 'Trigger execution type: DAGS (call external DAG service) or DirectInsert (create child records directly using InsertConfig)';
COMMENT ON COLUMN kat_dag_trigger.insert_config IS 'JSONB configuration for DirectInsert type. Contains renprops-like structure with |RENGUID| and |Todaydate| placeholders that get replaced during execution. Supports JSON queries and indexing.';

-- Step 6: Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_kat_dag_trigger_type_enabled 
ON kat_dag_trigger(trigger_type, enabled) 
WHERE enabled = true;

-- Create GIN index for JSONB operations on insert_config
CREATE INDEX IF NOT EXISTS idx_kat_dag_trigger_insert_config_gin 
ON kat_dag_trigger USING GIN (insert_config) 
WHERE insert_config IS NOT NULL;

-- Step 7: Verify the changes
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'kat_dag_trigger' 
AND column_name IN ('trigger_type', 'insert_config')
ORDER BY column_name;

-- Example usage after migration:
-- 
-- 1. Create a DAGS trigger (existing functionality):
-- INSERT INTO kat_dag_trigger (table_name, trigger, dag_id, enabled, trigger_type)
-- VALUES ('my_table', 'Insert', 'your-dag-id-here', true, 'DAGS');
--
-- 2. Create a DirectInsert trigger (new functionality):
-- INSERT INTO kat_dag_trigger (table_name, trigger, dag_id, enabled, trigger_type, insert_config)
-- VALUES ('brf_case', 'Insert', '00000000-0000-0000-0000-000000000000', true, 'DirectInsert', 
--         '{"brf_caseparticipant": [{"brf_name": "Auto Participant", "brf_caseid": "|RENGUID|"}]}'::jsonb);
--
-- 3. Query JSONB data (example):
-- SELECT * FROM kat_dag_trigger 
-- WHERE insert_config ? 'brf_caseparticipant'  -- Check if key exists
-- AND insert_config->'brf_caseparticipant'->0->>'brf_name' = 'Auto Participant';  -- Query nested values

COMMIT;