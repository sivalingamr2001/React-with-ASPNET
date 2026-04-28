import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { ColumnMeta } from "@/services/registryApi";
import { dataTypes, type ColumnFormValues } from "./types";

interface Props {
  form: ColumnFormValues;
  onChecked: (
    key: keyof ColumnFormValues,
  ) => (checked: boolean | "indeterminate") => void;
  onInput: (
    key: keyof ColumnFormValues,
  ) => (event: React.ChangeEvent<HTMLInputElement>) => void;
  onTypeChange: (dataType: ColumnMeta["dataType"]) => void;
}

export default function ColumnFormFields({
  form,
  onChecked,
  onInput,
  onTypeChange,
}: Props) {
  return (
    <>
      <label className="grid gap-2">
        <Label htmlFor="columnName">Column Name</Label>
        <Input
          id="columnName"
          required
          value={form.columnName}
          onChange={onInput("columnName")}
        />
      </label>
      <label className="grid gap-2">
        <Label>Data Type</Label>
        <Select value={form.dataType} onValueChange={onTypeChange}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {dataTypes.map((dataType) => (
              <SelectItem key={dataType} value={dataType}>
                {dataType}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </label>
      <div className="grid gap-3">
        <label className="flex items-center gap-3 rounded-lg border p-3">
          <Checkbox
            checked={form.isRequired}
            onCheckedChange={onChecked("isRequired")}
          />
          <span className="text-sm">Required</span>
        </label>
        <label className="flex items-center gap-3 rounded-lg border p-3">
          <Checkbox
            checked={form.isPrimaryKey}
            onCheckedChange={onChecked("isPrimaryKey")}
          />
          <span className="text-sm">Primary Key</span>
        </label>
        <label className="flex items-center gap-3 rounded-lg border p-3">
          <Checkbox
            checked={form.isForeignKey}
            onCheckedChange={onChecked("isForeignKey")}
          />
          <span className="text-sm">Foreign Key</span>
        </label>
      </div>
      <label className="grid gap-2">
        <Label htmlFor="fkReference">FK Reference</Label>
        <Input
          id="fkReference"
          value={form.fkReference || ""}
          onChange={onInput("fkReference")}
        />
      </label>
    </>
  );
}
