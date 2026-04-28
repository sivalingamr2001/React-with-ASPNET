import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { DbProfile } from "@/services/registryApi";
import { statuses, type ProfileFormValues } from "./types";

interface Props {
  status: ProfileFormValues["status"];
  onStatus: (status: DbProfile["status"]) => void;
}

export default function ProfileStatusField({ status, onStatus }: Props) {
  return (
    <label className="grid gap-2">
      <Label>Status</Label>
      <Select value={status} onValueChange={onStatus}>
        <SelectTrigger className="w-full">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {statuses.map((item) => (
            <SelectItem key={item} value={item}>
              {item}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </label>
  );
}
