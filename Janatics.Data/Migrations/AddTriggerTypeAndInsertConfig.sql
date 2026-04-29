-- Migration: Add TriggerType and InsertConfig fields to kat_dag_trigger table
-- Date: 2024-12-17

-- Add TriggerType column (default to 'DAGS' for existing records)
ALTER TABLE kat_dag_trigger 
ADD COLUMN IF NOT EXISTS trigger_type VARCHAR(50) DEFAULT 'DAGS';

-- Add InsertConfig column for storing JSON configuration
ALTER TABLE kat_dag_trigger 
ADD COLUMN IF NOT EXISTS insert_config TEXT;

-- Update existing records to have explicit TriggerType
UPDATE kat_dag_trigger 
SET trigger_type = 'DAGS' 
WHERE trigger_type IS NULL OR trigger_type = '';

-- Add check constraint for TriggerType
ALTER TABLE kat_dag_trigger 
ADD CONSTRAINT chk_trigger_type 
CHECK (trigger_type IN ('DAGS', 'DirectInsert'));

-- Add comment for documentation
COMMENT ON COLUMN kat_dag_trigger.trigger_type IS 'Trigger execution type: DAGS (call external DAG) or DirectInsert (create child records directly)';
COMMENT ON COLUMN kat_dag_trigger.insert_config IS 'JSON configuration for DirectInsert type containing renprops-like structure with |RENGUID| placeholders';

-- Example InsertConfig structure:
-- {
--   "brf_caseparticipant": [
--     {
--       "brf_partyroleid": "aa9c1d54-2b0a-4571-a24a-8f4a502d00ec",
--       "brf_name": "System Generated",
--       "brf_address": "Auto Generated",
--       "brf_email": "system@example.com",
--       "brf_mobilenumber": "+91 00000 00000",
--       "brf_caseid": "|RENGUID|"
--     }
--   ]
-- }