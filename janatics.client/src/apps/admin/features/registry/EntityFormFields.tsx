import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { EntityFormValues } from "./types";

interface Props {
  form: EntityFormValues;
  onInput: (
    key: keyof EntityFormValues,
  ) => (event: React.ChangeEvent<HTMLInputElement>) => void;
  onReadOnly: (checked: boolean | "indeterminate") => void;
}

export default function EntityFormFields({ form, onInput, onReadOnly }: Props) {
  return (
    <>
      <label className="grid gap-2">
        <Label htmlFor="entityName">Entity Name</Label>
        <Input
          id="entityName"
          required
          value={form.entityName}
          onChange={onInput("entityName")}
        />
      </label>
      <div className="grid gap-4 sm:grid-cols-2">
        <label className="grid gap-2">
          <Label htmlFor="schemaName">Schema Name</Label>
          <Input
            id="schemaName"
            required
            value={form.schemaName}
            onChange={onInput("schemaName")}
          />
        </label>
        <label className="grid gap-2">
          <Label htmlFor="pkColumn">Primary Key Column</Label>
          <Input
            id="pkColumn"
            required
            value={form.pkColumn}
            onChange={onInput("pkColumn")}
          />
        </label>
      </div>
      <label className="grid gap-2">
        <Label htmlFor="allowedRoles">Allowed Roles</Label>
        <Input
          id="allowedRoles"
          value={form.allowedRoles || ""}
          onChange={onInput("allowedRoles")}
        />
      </label>
      <div className="grid gap-4 sm:grid-cols-2">
        <label className="grid gap-2">
          <Label htmlFor="softDeleteColumn">Soft Delete Column</Label>
          <Input
            id="softDeleteColumn"
            value={form.softDeleteColumn || ""}
            onChange={onInput("softDeleteColumn")}
          />
        </label>
        <label className="grid gap-2">
          <Label htmlFor="softDeleteValue">Soft Delete Value</Label>
          <Input
            id="softDeleteValue"
            value={form.softDeleteValue || ""}
            onChange={onInput("softDeleteValue")}
          />
        </label>
      </div>
      <label className="flex items-center gap-3 rounded-lg border p-3">
        <Checkbox checked={form.isReadOnly} onCheckedChange={onReadOnly} />
        <span className="text-sm">
          Block all write operations for this entity.
        </span>
      </label>
    </>
  );
}
