import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { type ProfileFormValues } from "./types";

interface Props {
  form: ProfileFormValues;
  isSqlite: boolean;
  preview: string;
  onInput: (
    key: keyof ProfileFormValues,
  ) => (event: React.ChangeEvent<HTMLInputElement>) => void;
  onPort: (event: React.ChangeEvent<HTMLInputElement>) => void;
}

export default function ProfileDatabaseFields({
  form,
  isSqlite,
  preview,
  onInput,
  onPort,
}: Props) {
  if (isSqlite) {
    return (
      <label className="grid gap-2">
        <Label htmlFor="preview">Connection String Preview</Label>
        <Textarea id="preview" readOnly rows={3} value={preview} />
      </label>
    );
  }

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-2">
        <label className="grid gap-2">
          <Label htmlFor="host">Host</Label>
          <Input
            id="host"
            required
            value={form.host || ""}
            onChange={onInput("host")}
          />
        </label>
        <label className="grid gap-2">
          <Label htmlFor="port">Port</Label>
          <Input
            id="port"
            max={65535}
            min={1}
            placeholder={form.provider === "Oracle" ? "1521" : "1433"}
            type="number"
            value={form.port || ""}
            onChange={onPort}
          />
        </label>
      </div>
      <label className="grid gap-2">
        <Label htmlFor="preview">Connection String Preview</Label>
        <Textarea id="preview" readOnly rows={3} value={preview} />
      </label>
    </>
  );
}
