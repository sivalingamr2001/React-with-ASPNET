-- PostgreSQL master tables for Fetch and Process services

-- Audit outbox table for background processing of audit jobs
CREATE TABLE IF NOT EXISTS public.audit_outbox (
    id uuid PRIMARY KEY,
    payload jsonb NOT NULL,
    status varchar(20) NOT NULL,
    attempts integer NOT NULL DEFAULT 0,
    createdon timestamptz NOT NULL DEFAULT NOW(),
    processedon timestamptz NULL
);
CREATE INDEX IF NOT EXISTS ix_audit_outbox_status_createdon ON public.audit_outbox(status, createdon);

-- Add other master tables here as needed