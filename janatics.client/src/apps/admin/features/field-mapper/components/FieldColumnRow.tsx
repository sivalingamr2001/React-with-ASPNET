import { ArrowRight, Asterisk } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

import type { SchemaColumn } from "../types";

interface Props {
  column: SchemaColumn;
  value: string;
  onChange: (name: string, value: string) => void;
}

export default function FieldColumnRow({ column, value, onChange }: Props) {
  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) =>
    onChange(column.name, event.target.value);
  const handleClear = () => onChange(column.name, "");
  const type =
    column.type === "date"
      ? "date"
      : column.type === "number" || column.type === "decimal"
        ? "number"
        : "text";

  return (
    <div className="grid gap-3 rounded-[24px] border border-white/60 bg-background/70 p-4 md:grid-cols-[200px_24px_1fr_36px] md:items-center">
      <div className="space-y-1">
        <div className="flex items-center gap-1.5 font-medium">
          {column.name}
          {column.required && <Asterisk className="size-3 text-destructive" />}
        </div>
        <Badge className="rounded-full" variant="secondary">
          {column.type}
        </Badge>
      </div>
      <ArrowRight className="size-4 text-primary" />
      <Input
        className="h-11 rounded-2xl"
        onChange={handleChange}
        placeholder={
          column.required
            ? `Required ${column.name}`
            : `Optional ${column.name}`
        }
        type={type}
        value={value}
      />
      <Button
        className="rounded-full"
        disabled={!value}
        onClick={handleClear}
        size="icon"
        variant="ghost"
      >
        ×
      </Button>
    </div>
  );
}
