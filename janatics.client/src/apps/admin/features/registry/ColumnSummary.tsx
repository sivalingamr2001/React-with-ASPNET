import { Badge } from "@/components/ui/badge";
import type { ColumnMeta } from "@/services/registryApi";

interface Props {
  column: ColumnMeta;
}

const typeVariants: Record<
  ColumnMeta["dataType"],
  "default" | "secondary" | "outline"
> = {
  string: "secondary",
  number: "default",
  decimal: "default",
  date: "outline",
  boolean: "outline",
  guid: "secondary",
};

export default function ColumnSummary({ column }: Props) {
  return (
    <div className="space-y-2 rounded-lg border p-3">
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-medium">{column.columnName}</span>
        <Badge variant={typeVariants[column.dataType]}>{column.dataType}</Badge>
        {column.isPrimaryKey && <Badge variant="destructive">PK</Badge>}
        {column.isForeignKey && <Badge variant="outline">FK</Badge>}
        {column.isRequired && <Badge variant="secondary">Required</Badge>}
      </div>
      {column.fkReference && (
        <p className="text-sm text-muted-foreground">
          References {column.fkReference}
        </p>
      )}
    </div>
  );
}
