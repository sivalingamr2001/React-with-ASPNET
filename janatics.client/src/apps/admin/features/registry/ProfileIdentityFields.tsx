import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { DbProfile } from "@/services/registryApi";
import { providers, type ProfileFormValues } from "./types";

interface Props {
  form: ProfileFormValues;
  onInput: (
    key: keyof ProfileFormValues,
  ) => (event: React.ChangeEvent<HTMLInputElement>) => void;
  onProvider: (provider: DbProfile["provider"]) => void;
}

export default function ProfileIdentityFields({
  form,
  onInput,
  onProvider,
}: Props) {
  return (
    <>
      <label className="grid gap-2">
        <Label htmlFor="profileName">Profile Name</Label>
        <Input
          id="profileName"
          required
          value={form.profileName}
          onChange={onInput("profileName")}
        />
      </label>
      <label className="grid gap-2">
        <Label>Database Provider</Label>
        <Select value={form.provider} onValueChange={onProvider}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {providers.map((provider) => (
              <SelectItem key={provider} value={provider}>
                {provider}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </label>
      <label className="grid gap-2">
        <Label htmlFor="database">Database Name</Label>
        <Input
          id="database"
          required
          value={form.database}
          onChange={onInput("database")}
        />
      </label>
      <label className="grid gap-2">
        <Label htmlFor="username">Username</Label>
        <Input
          id="username"
          value={form.username || ""}
          onChange={onInput("username")}
        />
      </label>
    </>
  );
}
