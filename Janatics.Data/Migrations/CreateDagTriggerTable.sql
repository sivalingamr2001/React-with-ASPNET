-- Migration: Create DAG Trigger Configuration Table
-- Description: Creates table for managing DAG triggers on table CRUD operations

-- Create kat_dag_triggers table
CREATE TABLE IF NOT EXISTS kat_dag_triggers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    table_name VARCHAR(200) NOT NULL,
    trigger VARCHAR(50) NOT NULL,  -- "Insert", "Update", "Scheduled"
    dag_id UUID NOT NULL,  -- Reference to DAG configuration ID in KTAiFlow
    enabled BOOLEAN NOT NULL DEFAULT true,
    cron_expression VARCHAR(100),  -- For scheduled triggers
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_kat_dag_triggers_table_name 
    ON kat_dag_triggers(table_name);

CREATE INDEX IF NOT EXISTS idx_kat_dag_triggers_trigger 
    ON kat_dag_triggers(trigger);

CREATE INDEX IF NOT EXISTS idx_kat_dag_triggers_enabled 
    ON kat_dag_triggers(enabled);

CREATE INDEX IF NOT EXISTS idx_kat_dag_triggers_table_trigger 
    ON kat_dag_triggers(table_name, trigger);

-- Add comment to table
COMMENT ON TABLE kat_dag_triggers IS 'Configuration for DAG triggers on table CRUD operations (Insert, Update, Scheduled)';
COMMENT ON COLUMN kat_dag_triggers.table_name IS 'Name of the table to monitor';
COMMENT ON COLUMN kat_dag_triggers.trigger IS 'Type of trigger: Insert, Update, or Scheduled';
COMMENT ON COLUMN kat_dag_triggers.dag_id IS 'Reference to DAG configuration ID in KTAiFlow (GUID)';
COMMENT ON COLUMN kat_dag_triggers.cron_expression IS 'Cron expression for scheduled triggers (e.g., "0 2 * * *" for daily at 2 AM)';

-- Create kat_dag_trigger_logs table for audit and debugging
CREATE TABLE IF NOT EXISTS kat_dag_trigger_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    trigger_id UUID NOT NULL,
    dag_id UUID NOT NULL,
    table_name VARCHAR(200) NOT NULL,
    trigger_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(500),
    context_data JSONB,
    response_data JSONB,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    error_message TEXT,
    http_status_code INTEGER,
    execution_time_ms INTEGER,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    executed_at TIMESTAMP,
    CONSTRAINT fk_trigger_logs_trigger_id FOREIGN KEY (trigger_id) REFERENCES kat_dag_triggers(id) ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_kat_dag_trigger_logs_trigger_id 
    ON kat_dag_trigger_logs(trigger_id);

CREATE INDEX IF NOT EXISTS idx_kat_dag_trigger_logs_dag_id 
    ON kat_dag_trigger_logs(dag_id);

CREATE INDEX IF NOT EXISTS idx_kat_dag_trigger_logs_status 
    ON kat_dag_trigger_logs(status);

CREATE INDEX IF NOT EXISTS idx_kat_dag_trigger_logs_created_at 
    ON kat_dag_trigger_logs(created_at);

CREATE INDEX IF NOT EXISTS idx_kat_dag_trigger_logs_table_name 
    ON kat_dag_trigger_logs(table_name);

-- Add comments to log table
COMMENT ON TABLE kat_dag_trigger_logs IS 'Audit log for DAG trigger executions with context and response data';
COMMENT ON COLUMN kat_dag_trigger_logs.trigger_id IS 'Reference to the trigger configuration';
COMMENT ON COLUMN kat_dag_trigger_logs.dag_id IS 'DAG configuration ID that was triggered';
COMMENT ON COLUMN kat_dag_trigger_logs.table_name IS 'Name of the table that triggered the DAG';
COMMENT ON COLUMN kat_dag_trigger_logs.trigger_type IS 'Type of trigger: Insert, Update, or Scheduled';
COMMENT ON COLUMN kat_dag_trigger_logs.entity_id IS 'ID of the entity that triggered the DAG';
COMMENT ON COLUMN kat_dag_trigger_logs.context_data IS 'JSON data passed to the DAG (input context)';
COMMENT ON COLUMN kat_dag_trigger_logs.response_data IS 'JSON response from the DAG execution';
COMMENT ON COLUMN kat_dag_trigger_logs.status IS 'Execution status: Pending, Success, Failed';
COMMENT ON COLUMN kat_dag_trigger_logs.error_message IS 'Error message if execution failed';
COMMENT ON COLUMN kat_dag_trigger_logs.http_status_code IS 'HTTP status code from API response';
COMMENT ON COLUMN kat_dag_trigger_logs.execution_time_ms IS 'Time taken to execute the DAG in milliseconds';
