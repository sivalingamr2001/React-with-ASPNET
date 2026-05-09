-- MySQL master tables for Fetch and Process services

-- Audit outbox table for background processing of audit jobs
CREATE TABLE IF NOT EXISTS audit_outbox (
    id VARCHAR(255) PRIMARY KEY,
    payload JSON NOT NULL,
    status VARCHAR(20) NOT NULL,
    attempts INT NOT NULL DEFAULT 0,
    createdon DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processedon DATETIME NULL
);
CREATE INDEX IF NOT EXISTS ix_audit_outbox_status_createdon ON audit_outbox(status, createdon);

-- Add other master tables here as needed