import { Checkbox } from "@/components/ui/checkbox";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface Props {
  columns: Array<{ name: string; type: string }>;
  selectedCols: string[];
  onToggle: (name: string) => void;
}

export default function ColumnSelector({
  columns,
  selectedCols,
  onToggle,
}: Props) {
  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardHeader>
        <CardTitle>Columns</CardTitle>
      </CardHeader>
      <CardContent className="grid gap-3 md:grid-cols-2">
        {columns.map((column) => (
          <label
            className="flex items-center gap-3 rounded-2xl border border-white/60 bg-background/70 p-3"
            key={column.name}
          >
            <Checkbox
              checked={selectedCols.includes(column.name)}
              onCheckedChange={onToggle.bind(null, column.name)}
            />
            <span className="flex-1 text-sm">{column.name}</span>
            <span className="rounded-full bg-secondary px-2 py-1 text-xs">
              {column.type}
            </span>
          </label>
        ))}
      </CardContent>
    </Card>
  );
}
