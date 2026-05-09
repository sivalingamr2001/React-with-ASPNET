-- Oracle master tables for Fetch and Process services

-- Audit outbox table for background processing of audit jobs
CREATE TABLE audit_outbox (
    id VARCHAR2(255) PRIMARY KEY,
    payload CLOB NOT NULL,
    status VARCHAR2(20) NOT NULL,
    attempts NUMBER(10) NOT NULL DEFAULT 0,
    createdon TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    processedon TIMESTAMP WITH TIME ZONE NULL
);
CREATE INDEX ix_audit_outbox_status_createdon ON audit_outbox(status, createdon);

-- Add other master tables here as needed