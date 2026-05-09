PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS applicationtable (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tablename TEXT NOT NULL UNIQUE,
    displayname TEXT NULL,
    enableaudit INTEGER NOT NULL DEFAULT 0,
    isactive INTEGER NOT NULL DEFAULT 1,
    createdon TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    createdby TEXT NULL,
    modifiedon TEXT NULL,
    modifiedby TEXT NULL
);

CREATE TABLE IF NOT EXISTS fieldmapper (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entityname TEXT NOT NULL,
    fieldname TEXT NOT NULL,
    columnname TEXT NOT NULL,
    datatype TEXT NOT NULL,
    displayname TEXT NULL,
    properties TEXT NULL,
    defaultvalue TEXT NULL,
    allowupdate INTEGER NOT NULL DEFAULT 1,
    isactive INTEGER NOT NULL DEFAULT 1,
    createdon TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    createdby TEXT NULL,
    modifiedon TEXT NULL,
    modifiedby TEXT NULL
);

CREATE INDEX IF NOT EXISTS ix_fieldmapper_entityname_active
    ON fieldmapper (entityname, isactive);

CREATE TABLE IF NOT EXISTS auditlog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    transactiontable TEXT NOT NULL,
    transactionid TEXT NOT NULL,
    modifiedby TEXT NULL,
    operation TEXT NOT NULL,
    changelog TEXT NOT NULL,
    createdon TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);

CREATE INDEX IF NOT EXISTS ix_auditlog_table_transaction
    ON auditlog (transactiontable, transactionid);

CREATE TABLE IF NOT EXISTS fetchquerydefinition (
    querynumber INTEGER PRIMARY KEY AUTOINCREMENT,
    description TEXT NOT NULL,
    querytext TEXT NULL,
    fetchjson TEXT NULL,
    isfetchjson INTEGER NOT NULL DEFAULT 0,
    parameterdefinition TEXT NULL,
    isactive INTEGER NOT NULL DEFAULT 1,
    createdby TEXT NULL,
    createdon TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    modifiedby TEXT NULL,
    modifiedon TEXT NULL,
    versionno INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS fetchquerytablemap (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    querynumber INTEGER NOT NULL REFERENCES fetchquerydefinition(querynumber),
    tablename TEXT NOT NULL,
    createdon TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);

CREATE INDEX IF NOT EXISTS ix_fetchquerytablemap_querynumber
    ON fetchquerytablemap (querynumber);

CREATE INDEX IF NOT EXISTS ix_fetchquerytablemap_tablename
    ON fetchquerytablemap (tablename);

CREATE TABLE IF NOT EXISTS fetchqueryauditlog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    querynumber INTEGER NOT NULL REFERENCES fetchquerydefinition(querynumber),
    actionname TEXT NOT NULL,
    executedquery TEXT NULL,
    parametersjson TEXT NULL,
    resultcount INTEGER NULL,
    status TEXT NOT NULL,
    errormessage TEXT NULL,
    createdby TEXT NULL,
    createdon TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);

CREATE INDEX IF NOT EXISTS ix_fetchqueryauditlog_querynumber
    ON fetchqueryauditlog (querynumber, createdon DESC);
