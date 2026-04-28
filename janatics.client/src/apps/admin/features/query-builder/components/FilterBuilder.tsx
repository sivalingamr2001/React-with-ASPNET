import { Plus } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

import FilterRow from "./FilterRow";
import type { QueryFilter } from "../types";

interface Props {
  columns: Array<{
    name: string;
    type: "string" | "number" | "decimal" | "date" | "boolean";
  }>;
  filters: QueryFilter[];
  onAdd: () => void;
  onChange: (value: QueryFilter) => void;
}

export default function FilterBuilder({
  columns,
  filters,
  onAdd,
  onChange,
}: Props) {
  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Filters</CardTitle>
        <Button className="rounded-full" onClick={onAdd} size="sm">
          <Plus className="size-4" />
          Add
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        {filters.map((filter) => (
          <FilterRow
            columns={columns}
            filter={filter}
            key={filter.id}
            onChange={onChange}
          />
        ))}
      </CardContent>
    </Card>
  );
}
