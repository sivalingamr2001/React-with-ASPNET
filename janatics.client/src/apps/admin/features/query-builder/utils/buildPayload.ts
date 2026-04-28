import type { QueryFilter } from "../types";

interface Args {
  columns: Array<{ name: string }>;
  entity: string;
  filters: QueryFilter[];
  includeCount: boolean;
  nodes: string[];
  page: number;
  pageSize: number;
  searchCols: string[];
  searchTerm: string;
  selectedCols: string[];
  sortCol: string;
  sortDir: string;
}

export function buildQueryPayload(args: Args) {
  return {
    rootEntity: args.entity,
    page: args.page,
    pageSize: args.pageSize,
    includeCount: args.includeCount,
    ...(args.searchTerm && {
      search: {
        term: args.searchTerm,
        columns: args.searchCols.length
          ? args.searchCols
          : args.columns.map((column) => column.name),
      },
    }),
    ...(args.filters.filter((filter) => filter.column).length && {
      filters: args.filters
        .filter((filter) => filter.column)
        .map((filter) => ({
          column: filter.column,
          op: filter.op,
          ...(filter.value && { value: filter.value }),
          ...(filter.value2 && { value2: filter.value2 }),
        })),
    }),
    ...(args.sortCol && {
      sort: { column: args.sortCol, direction: args.sortDir },
    }),
    ...(args.selectedCols.length && { select: args.selectedCols }),
    ...(args.nodes.length && { nodes: args.nodes }),
  };
}
