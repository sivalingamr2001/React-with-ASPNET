-- SQL Server master tables for Fetch and Process services

-- Audit outbox table for background processing of audit jobs
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='audit_outbox' AND xtype='U')
CREATE TABLE audit_outbox (
    id nvarchar(450) PRIMARY KEY,
    payload nvarchar(max) NOT NULL,
    status nvarchar(20) NOT NULL,
    attempts int NOT NULL DEFAULT 0,
    createdon datetime2 NOT NULL DEFAULT GETUTCDATE(),
    processedon datetime2 NULL
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_audit_outbox_status_createdon')
CREATE INDEX ix_audit_outbox_status_createdon ON audit_outbox(status, createdon);

-- Add other master tables here as needed