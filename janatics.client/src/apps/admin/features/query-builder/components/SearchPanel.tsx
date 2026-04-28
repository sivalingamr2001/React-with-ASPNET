import { Checkbox } from "@/components/ui/checkbox";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";

interface Props {
  columns: Array<{ name: string; type: string }>;
  searchCols: string[];
  searchTerm: string;
  onSearchColToggle: (name: string) => void;
  onSearchTermChange: (value: string) => void;
}

export default function SearchPanel(props: Props) {
  const handleSearchTerm = (event: React.ChangeEvent<HTMLInputElement>) =>
    props.onSearchTermChange(event.target.value);

  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardHeader>
        <CardTitle>Search</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <Input
          className="h-11 rounded-2xl"
          onChange={handleSearchTerm}
          placeholder="Search term"
          value={props.searchTerm}
        />
        <div className="grid gap-3 md:grid-cols-2">
          {props.columns
            .filter((column) => ["string", "number"].includes(column.type))
            .map((column) => (
              <label
                className="flex items-center gap-3 rounded-2xl border border-white/60 bg-background/70 p-3"
                key={column.name}
              >
                <Checkbox
                  checked={props.searchCols.includes(column.name)}
                  onCheckedChange={props.onSearchColToggle.bind(
                    null,
                    column.name,
                  )}
                />
                <span className="text-sm">{column.name}</span>
              </label>
            ))}
        </div>
      </CardContent>
    </Card>
  );
}
