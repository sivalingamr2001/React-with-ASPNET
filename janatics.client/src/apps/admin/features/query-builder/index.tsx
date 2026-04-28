import PageFrame from "../../shell/components/PageFrame";
import type { AdminPageConfig } from "../../shell/types";
import { useQueryBuilder } from "./hooks/useQueryBuilder";
import ColumnSelector from "./components/ColumnSelector";
import FilterBuilder from "./components/FilterBuilder";
import PaginationPanel from "./components/PaginationPanel";
import QueryToolbar from "./components/QueryToolbar";
import ResultsPreview from "./components/ResultsPreview";
import SearchPanel from "./components/SearchPanel";
import SortPanel from "./components/SortPanel";

interface Props {
  page: AdminPageConfig;
}

export default function QueryBuilderPage({ page }: Props) {
  const state = useQueryBuilder();
  const handleEntityChange = (value: string) => {
    state.setEntity(value);
    state.setSelectedCols([]);
    state.setFilters([]);
    state.setSearchCols([]);
    state.setSortCol("");
    state.setNodes([]);
  };
  const handleColumnToggle = (name: string) =>
    state.setSelectedCols((items) =>
      items.includes(name)
        ? items.filter((item) => item !== name)
        : [...items, name],
    );
  const handleSearchToggle = (name: string) =>
    state.setSearchCols((items) =>
      items.includes(name)
        ? items.filter((item) => item !== name)
        : [...items, name],
    );
  const handleFilterChange = (value: {
    column: string;
    id: string;
    op: string;
    value: string;
    value2: string;
  }) =>
    state.setFilters((items) =>
      items.map((item) => (item.id === value.id ? value : item)),
    );

  return (
    <PageFrame description={page.description} title={page.title}>
      <QueryToolbar entity={state.entity} onEntityChange={handleEntityChange} />
      {state.schema && (
        <div className="grid gap-4 xl:grid-cols-2">
          <ColumnSelector
            columns={state.columns}
            onToggle={handleColumnToggle}
            selectedCols={state.selectedCols}
          />
          <SearchPanel
            columns={state.columns}
            onSearchColToggle={handleSearchToggle}
            onSearchTermChange={state.setSearchTerm}
            searchCols={state.searchCols}
            searchTerm={state.searchTerm}
          />
          <FilterBuilder
            columns={state.columns}
            filters={state.filters}
            onAdd={state.addFilter}
            onChange={handleFilterChange}
          />
          <SortPanel
            columns={state.columns}
            onColumnChange={state.setSortCol}
            onDirectionChange={state.setSortDir}
            sortCol={state.sortCol}
            sortDir={state.sortDir}
          />
          <PaginationPanel
            includeCount={state.includeCount}
            onIncludeCountChange={state.setIncludeCount}
            onPageChange={state.setPage}
            onPageSizeChange={state.setPageSize}
            page={state.page}
            pageSize={state.pageSize}
          />
          <ResultsPreview preview={state.preview} />
        </div>
      )}
    </PageFrame>
  );
}
