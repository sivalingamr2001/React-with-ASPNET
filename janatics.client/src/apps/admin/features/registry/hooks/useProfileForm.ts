import { useEffect, useMemo, useState } from "react";
import type { DbProfile } from "@/services/registryApi";
import { defaultProfileFormValues, type ProfileFormValues } from "../types";
import { getConnectionString } from "../utils/connectionString";

interface Props {
  profile?: DbProfile;
  visible: boolean;
}

export function useProfileForm({ profile, visible }: Props) {
  const [form, setForm] = useState<ProfileFormValues>(defaultProfileFormValues);
  const preview = useMemo(() => getConnectionString(form), [form]);
  const isSqlite = form.provider === "Sqlite";

  useEffect(() => {
    if (visible) {
      setForm(
        profile
          ? { ...defaultProfileFormValues, ...profile }
          : defaultProfileFormValues,
      );
    }
  }, [profile, visible]);

  const handleInput =
    (key: keyof ProfileFormValues) =>
    (event: React.ChangeEvent<HTMLInputElement>) =>
      setForm((current) => ({ ...current, [key]: event.target.value }));
  const handlePort = (event: React.ChangeEvent<HTMLInputElement>) =>
    setForm((current) => ({
      ...current,
      port: Number(event.target.value) || undefined,
    }));
  const handleProvider = (provider: DbProfile["provider"]) =>
    setForm((current) => ({
      ...current,
      provider,
      host: provider === "Sqlite" ? "" : current.host,
      port:
        provider === "Oracle"
          ? 1521
          : provider === "SqlServer"
            ? 1433
            : undefined,
    }));
  const handleStatus = (status: DbProfile["status"]) =>
    setForm((current) => ({ ...current, status }));

  return {
    form,
    isSqlite,
    preview,
    handleInput,
    handlePort,
    handleProvider,
    handleStatus,
  };
}
