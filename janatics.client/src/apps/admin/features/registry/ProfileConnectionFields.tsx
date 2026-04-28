import type { DbProfile } from "@/services/registryApi";
import ProfileDatabaseFields from "./ProfileDatabaseFields";
import ProfileIdentityFields from "./ProfileIdentityFields";
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
}

export default function ProfileConnectionFields({
  form,
  isSqlite,
  preview,
  onInput,
  onPort,
  onProvider,
}: Props) {
  return (
    <>
      <ProfileIdentityFields
        form={form}
        onInput={onInput}
        onProvider={onProvider}
      />
      <ProfileDatabaseFields
        form={form}
        isSqlite={isSqlite}
        onInput={onInput}
        onPort={onPort}
        preview={preview}
      />
    </>
  );
}
