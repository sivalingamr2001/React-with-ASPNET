# Janatics DataEngine — React Client Structure
> Aligned with System Design Document v1.0 · Stack: React 18 + TypeScript + Vite

---

## Current vs. Recommended: Gap Summary

| Area | Current | Recommended | Status |
|---|---|---|---|
| Domain components | ❌ Missing (`ui/` only) | `registry/`, `querybuilder/`, `fieldmapper/` | 🔴 Add |
| Services layer | ❌ Missing entirely | `registryApi`, `fetchApi`, `forgeApi` | 🔴 Add |
| Types | ⚠️ Single `types.ts` | Split by domain | 🟡 Refactor |
| Admin pages | ⚠️ Partial | `RegistryManager`, `QueryBuilder`, `FieldMapper` | 🟡 Complete |
| Auth/layout/context | ✅ Present | Keep as-is | 🟢 Good |
| `apps/admin` + `apps/user` split | ✅ Better than doc | Keep — improves on the spec | 🟢 Keep |

---

## Recommended Structure

```
janatics.client/
│
├── index.html
├── vite.config.ts
├── tsconfig.json
├── package.json
├── .env.development
│
├── public/
│
└── src/
    │
    ├── main.tsx                        # App entry point
    ├── App.tsx                         # Root router
    ├── index.css
    ├── vite-env.d.ts
    │
    ├── apps/
    │   │
    │   ├── admin/                      # 🔴 DataEngine Admin Suite (per §10)
    │   │   │
    │   │   ├── Index.tsx               # Admin shell / layout wrapper
    │   │   │
    │   │   ├── pages/
    │   │   │   ├── RegistryManager.tsx # DB Profiles + Entity list + Column panel
    │   │   │   ├── QueryBuilder.tsx    # /fetch visual query composer
    │   │   │   └── FieldMapper.tsx     # /forge payload builder  ← ADD THIS
    │   │   │
    │   │   └── components/
    │   │       │
    │   │       ├── registry/           # ← ADD entire folder
    │   │       │   ├── ProfileCard.tsx
    │   │       │   ├── ProfileForm.tsx
    │   │       │   ├── EntityCard.tsx
    │   │       │   ├── EntityForm.tsx
    │   │       │   └── ColumnPanel.tsx
    │   │       │
    │   │       ├── querybuilder/       # ← ADD entire folder
    │   │       │   ├── FilterRow.tsx
    │   │       │   ├── ColumnSelector.tsx
    │   │       │   ├── SortConfig.tsx
    │   │       │   ├── PaginationConfig.tsx
    │   │       │   └── ResultsTable.tsx
    │   │       │
    │   │       └── fieldmapper/        # ← ADD entire folder
    │   │           ├── ColumnRow.tsx
    │   │           └── ChildSection.tsx
    │   │
    │   └── user/                       # ✅ User-facing portal (keep as-is)
    │       │
    │       ├── index.tsx
    │       │
    │       └── pages/
    │           ├── HomePage.tsx
    │           ├── Login.tsx
    │           └── Portal.tsx
    │
    ├── services/                       # 🔴 ADD — API communication layer (per §10.1)
    │   ├── registryApi.ts              # CRUD for DbProfiles + Entities + Columns
    │   ├── fetchApi.ts                 # POST /fetch
    │   └── forgeApi.ts                 # POST /forge
    │
    ├── types/                          # 🟡 REFACTOR from single types.ts
    │   ├── FetchRequest.ts             # FetchRequest, FilterClause, SortClause, etc.
    │   ├── ForgeRequest.ts             # ForgeRequest, ChildRecord, etc.
    │   ├── Registry.ts                 # DbProfile, EntityDefinition, ColumnDefinition
    │   └── index.ts                    # Re-exports all types
    │
    ├── components/
    │   └── ui/                         # ✅ Keep — shadcn/ui primitives
    │       ├── button.tsx
    │       ├── table.tsx
    │       ├── dialog.tsx
    │       └── ...                     # (all existing shadcn components)
    │
    ├── context/                        # ✅ Keep as-is
    │   ├── auth-provider.tsx
    │   └── theme-provider.tsx
    │
    ├── layout/                         # ✅ Keep as-is
    │   ├── AppHeader.tsx
    │   ├── AppLayout.tsx
    │   └── AppSidebar.tsx
    │
    ├── hooks/                          # ✅ Keep, expand as needed
    │   └── use-mobile.ts
    │
    ├── lib/                            # ✅ Keep
    │   └── utils.ts
    │
    └── assets/
        └── jana.png
```

---

## Key Changes Explained

### 🔴 Add: `services/` layer
The document defines three distinct API contracts (§10.1). These must be separate files — not inline fetch calls — so they can be mocked, tested, and swapped independently.

```
services/registryApi.ts   →  CRUD on MetaDB (DbProfiles, EntityRegistry, ColumnRegistry)
services/fetchApi.ts      →  POST /fetch  (read queries)
services/forgeApi.ts      →  POST /forge  (write operations)
```

### 🔴 Add: Domain component folders under `apps/admin/components/`
The document prescribes three component namespaces. Flat `ui/` components (shadcn) are primitives — domain components compose them into DataEngine-specific UI.

```
registry/       →  forms + cards for DB Profiles and Entity management
querybuilder/   →  filter rows, column pickers, sort/pagination controls
fieldmapper/    →  column rows and child-record sections for /forge payloads
```

### 🟡 Refactor: `types.ts` → `types/` folder
Split the single flat file by domain. This prevents circular imports as the codebase grows and mirrors the backend's contract boundaries (`FetchRequest`, `ForgeRequest`, `Registry`).

### 🟢 Keep: `apps/admin` + `apps/user` split
The document shows a flat `src/pages/` structure, but your two-app separation is strictly better. It enforces access control at the routing level and keeps admin bundle separate from the user portal.

---

## Type Skeleton Reference

### `types/FetchRequest.ts`
```typescript
export interface FilterClause {
  field: string;
  operator: 'eq' | 'neq' | 'gt' | 'lt' | 'gte' | 'lte' | 'like' | 'in';
  value: string | number | boolean | null;
}

export interface SortClause {
  field: string;
  direction: 'asc' | 'desc';
}

export interface FetchRequest {
  entity: string;
  columns?: string[];
  filters?: FilterClause[];
  sort?: SortClause[];
  page?: number;
  pageSize?: number;
}

export interface FetchResponse<T = Record<string, unknown>> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

### `types/ForgeRequest.ts`
```typescript
export interface ColumnValue {
  column: string;
  value: string | number | boolean | null;
}

export interface ChildRecord {
  entity: string;
  values: ColumnValue[];
}

export interface ForgeRequest {
  entity: string;
  operation: 'insert' | 'update' | 'delete';
  values: ColumnValue[];
  children?: ChildRecord[];
}
```

### `types/Registry.ts`
```typescript
export type DbProvider = 'Oracle' | 'SqlServer' | 'Sqlite';

export interface DbProfile {
  profileId: string;
  profileName: string;
  provider: DbProvider;
  host: string;
  port: number;
  database: string;
  status: 'active' | 'inactive';
}

export interface ColumnDefinition {
  columnName: string;
  dataType: string;
  isPrimaryKey: boolean;
  isNullable: boolean;
  isForgeable: boolean;
}

export interface EntityDefinition {
  entityId: string;
  entityName: string;
  tableName: string;
  schemaName: string;
  profileId: string;
  columns: ColumnDefinition[];
}
```

---

## Priority Order for Implementation

1. **`types/`** — Split and define all contracts first. Everything else depends on these.
2. **`services/`** — Wire up the three API files against your .NET backend.
3. **`apps/admin/pages/RegistryManager.tsx`** — Core governance UI.
4. **`apps/admin/components/registry/`** — Cards and forms used by RegistryManager.
5. **`apps/admin/pages/FieldMapper.tsx`** — Currently missing entirely.
6. **`apps/admin/components/querybuilder/`** and **`fieldmapper/`** — Sub-components.

---

*Structure aligned with Janatics DataEngine System & Development Design Document v1.0 — Section 10: Frontend Admin Suite*