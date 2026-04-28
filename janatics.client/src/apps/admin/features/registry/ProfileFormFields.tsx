import type { DbProfile } from "@/services/registryApi";
import ProfileConnectionFields from "./ProfileConnectionFields";
import ProfileStatusField from "./ProfileStatusField";
import type { ProfileFormValues } from "./types";

interface Props {
  form: ProfileFormValues;
  isSqlite: boolean;
  preview: string;
  onInput: (
    key: keyof ProfileFormValues,
  ) => (event: React.ChangeEvent<HTMLInputElement>) => void;
  onPort: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onProvider: (provider: DbProfile["provider"]) => void;
  onStatus: (status: DbProfile["status"]) => void;
}

export default function ProfileFormFields({
  form,
  isSqlite,
  preview,
  onInput,
  onPort,
  onProvider,
  onStatus,
}: Props) {
  return (
    <>
      <ProfileConnectionFields
        form={form}
        isSqlite={isSqlite}
        onInput={onInput}
        onPort={onPort}
        onProvider={onProvider}
        preview={preview}
      />
      <ProfileStatusField onStatus={onStatus} status={form.status} />
    </>
  );
}
