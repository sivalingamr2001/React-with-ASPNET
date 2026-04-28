import { useMemo, useState } from "react";

import { QUERY_REGISTRY } from "../data";
import { buildQueryPayload } from "../utils/buildPayload";
import type { QueryFilter } from "../types";

const uid = () => Math.random().toString(36).slice(2, 8);

export function useQueryBuilder() {
  const [entity, setEntity] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [includeCount, setIncludeCount] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [searchCols, setSearchCols] = useState<string[]>([]);
  const [filters, setFilters] = useState<QueryFilter[]>([]);
  const [sortCol, setSortCol] = useState("");
  const [sortDir, setSortDir] = useState("asc");
  const [selectedCols, setSelectedCols] = useState<string[]>([]);
  const [nodes, setNodes] = useState<string[]>([]);
  const schema = entity ? QUERY_REGISTRY[entity] : null;
  const columns = schema?.columns ?? [];
  const payload = entity
    ? buildQueryPayload({
        columns,
        entity,
        filters,
        includeCount,
        nodes,
        page,
        pageSize,
        searchCols,
        searchTerm,
        selectedCols,
        sortCol,
        sortDir,
      })
    : null;
  const preview = useMemo(() => JSON.stringify(payload, null, 2), [payload]);
  const addFilter = () =>
    setFilters((items) => [
      ...items,
      { column: "", id: uid(), op: "eq", value: "", value2: "" },
    ]);
  return {
    addFilter,
    columns,
    entity,
    filters,
    includeCount,
    nodes,
    page,
    pageSize,
    payload,
    preview,
    schema,
    searchCols,
    searchTerm,
    selectedCols,
    setEntity,
    setFilters,
    setIncludeCount,
    setNodes,
    setPage,
    setPageSize,
    setSearchCols,
    setSearchTerm,
    setSelectedCols,
    setSortCol,
    setSortDir,
    sortCol,
    sortDir,
  };
}
