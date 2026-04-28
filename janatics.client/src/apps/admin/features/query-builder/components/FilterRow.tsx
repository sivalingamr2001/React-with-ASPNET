import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { OPS_BY_TYPE } from "../data";
import type { QueryFilter } from "../types";

interface Props {
  columns: Array<{
    name: string;
    type: "string" | "number" | "decimal" | "date" | "boolean";
  }>;
  filter: QueryFilter;
  onChange: (value: QueryFilter) => void;
}

export default function FilterRow({ filter, columns, onChange }: Props) {
  const column = columns.find((item) => item.name === filter.column);
  const ops = column ? OPS_BY_TYPE[column.type] : OPS_BY_TYPE.string;
  const handleColumn = (value: string) =>
    onChange({ ...filter, column: value, op: "eq", value: "", value2: "" });
  const handleOp = (value: string) =>
    onChange({ ...filter, op: value, value: "", value2: "" });
  const handleValue = (event: React.ChangeEvent<HTMLInputElement>) =>
    onChange({ ...filter, value: event.target.value });

  return (
    <div className="grid gap-3 rounded-[24px] border border-white/60 bg-background/70 p-4 md:grid-cols-3">
      <Select onValueChange={handleColumn} value={filter.column}>
        <SelectTrigger className="h-11 rounded-2xl">
          <SelectValue placeholder="Column" />
        </SelectTrigger>
        <SelectContent>
          {columns.map((item) => (
            <SelectItem key={item.name} value={item.name}>
              {item.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Select onValueChange={handleOp} value={filter.op}>
        <SelectTrigger className="h-11 rounded-2xl">
          <SelectValue placeholder="Operator" />
        </SelectTrigger>
        <SelectContent>
          {ops.map((item) => (
            <SelectItem key={item} value={item}>
              {item}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Input
        className="h-11 rounded-2xl"
        onChange={handleValue}
        placeholder="Value"
        value={filter.value}
      />
    </div>
  );
}
