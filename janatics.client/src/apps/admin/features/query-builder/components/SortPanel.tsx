import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface Props {
  columns: Array<{ name: string }>;
  sortCol: string;
  sortDir: string;
  onColumnChange: (value: string) => void;
  onDirectionChange: (value: string) => void;
}

export default function SortPanel(props: Props) {
  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardHeader>
        <CardTitle>Sort</CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4 md:grid-cols-[1fr_220px]">
        <Select onValueChange={props.onColumnChange} value={props.sortCol}>
          <SelectTrigger className="h-11 rounded-2xl">
            <SelectValue placeholder="Sort column" />
          </SelectTrigger>
          <SelectContent>
            {props.columns.map((column) => (
              <SelectItem key={column.name} value={column.name}>
                {column.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="flex gap-2">
          <Button
            className="flex-1 rounded-2xl"
            onClick={props.onDirectionChange.bind(null, "asc")}
            variant={props.sortDir === "asc" ? "default" : "outline"}
          >
            ASC
          </Button>
          <Button
            className="flex-1 rounded-2xl"
            onClick={props.onDirectionChange.bind(null, "desc")}
            variant={props.sortDir === "desc" ? "default" : "outline"}
          >
            DESC
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
