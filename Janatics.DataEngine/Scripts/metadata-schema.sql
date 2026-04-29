CREATE TABLE IF NOT EXISTS DbProfiles (
    ProfileId TEXT NOT NULL PRIMARY KEY,
    ProfileName TEXT NOT NULL UNIQUE,
    Provider TEXT NOT NULL,
    ConnectionString TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'active',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS EntityRegistry (
    EntityId TEXT NOT NULL PRIMARY KEY,
    ProfileId TEXT NOT NULL,
    EntityName TEXT NOT NULL UNIQUE,
    TableName TEXT NOT NULL,
    SchemaName TEXT NULL,
    PkColumn TEXT NOT NULL,
    SoftDeleteColumn TEXT NULL,
    IsReadOnly INTEGER NOT NULL DEFAULT 0,
    AllowedRoles TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (ProfileId) REFERENCES DbProfiles(ProfileId)
);

CREATE TABLE IF NOT EXISTS ColumnRegistry (
    ColumnId TEXT NOT NULL PRIMARY KEY,
    EntityId TEXT NOT NULL,
    ColumnName TEXT NOT NULL,
    DataType TEXT NOT NULL,
    IsRequired INTEGER NOT NULL DEFAULT 0,
    IsPrimaryKey INTEGER NOT NULL DEFAULT 0,
    IsSearchable INTEGER NOT NULL DEFAULT 0,
    IsReadOnly INTEGER NOT NULL DEFAULT 0,
    IsNullable INTEGER NOT NULL DEFAULT 1,
    OrdinalPosition INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (EntityId) REFERENCES EntityRegistry(EntityId)
);
