import { Trash2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";

import { FIELD_SCHEMA } from "../data";
import FieldColumnRow from "./FieldColumnRow";

interface Props {
  child: {
    entity: string;
    fkColumn: string;
    id: number;
    values: Record<string, string>;
  };
  onFKChange: (id: number, value: string) => void;
  onRemove: (id: number) => void;
  onValueChange: (id: number, column: string, value: string) => void;
}

export default function ChildEntityCard({
  child,
  onFKChange,
  onRemove,
  onValueChange,
}: Props) {
  const schema = FIELD_SCHEMA[child.entity];
  const handleFKChange = (event: React.ChangeEvent<HTMLInputElement>) =>
    onFKChange(child.id, event.target.value);
  const handleRecordChange = (event: React.ChangeEvent<HTMLInputElement>) =>
    onValueChange(child.id, "__recordId", event.target.value);
  const handleRemove = () => onRemove(child.id);

  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>{child.entity}</CardTitle>
        <Button
          className="rounded-full"
          onClick={handleRemove}
          size="icon"
          variant="ghost"
        >
          <Trash2 className="size-4" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="grid gap-3 md:grid-cols-2">
          <Input
            className="h-11 rounded-2xl"
            onChange={handleFKChange}
            placeholder="Foreign key column"
            value={child.fkColumn}
          />
          <Input
            className="h-11 rounded-2xl"
            onChange={handleRecordChange}
            placeholder="Child record ID"
            value={child.values.__recordId || ""}
          />
        </div>
        <div className="space-y-3">
          {schema.columns.map((column) => (
            <FieldColumnRow
              column={column}
              key={column.name}
              onChange={onValueChange.bind(null, child.id)}
              value={child.values[column.name] || ""}
            />
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
