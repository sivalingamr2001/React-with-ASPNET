import type { Dispatch, SetStateAction } from "react";
import { toast } from "sonner";
import {
  createColumn,
  createEntity,
  createProfile,
  deleteColumn,
  deleteEntity,
  deleteProfile,
  discoverTables,
  testConnection,
  updateEntity,
  updateProfile,
  type ColumnMeta,
  type DbProfile,
  type EntityMeta,
} from "@/services/registryApi";

type Setter = Dispatch<SetStateAction<boolean>>;

export function createProfileActions(
  setLoading: Setter,
  reload: () => Promise<void>,
  selectedId: string,
  setSelectedId: (value: string) => void,
) {
  return {
    handleAddProfile: (data: Omit<DbProfile, "profileId">) =>
      runMutation(
        setLoading,
        () => createProfile(data),
        reload,
        "Profile created",
        "Failed to create profile",
      ),
    handleUpdateProfile: (profileId: string, data: Partial<DbProfile>) =>
      runMutation(
        setLoading,
        () => updateProfile(profileId, data),
        reload,
        "Profile updated",
        "Failed to update profile",
      ),
    handleDeleteProfile: (profileId: string) =>
      runMutation(
        setLoading,
        () => deleteProfile(profileId),
        async () => {
          if (selectedId === profileId) setSelectedId("");
          await reload();
        },
        "Profile deleted",
        "Failed to delete profile",
        false,
      ),
  };
}

export function createEntityActions(
  setLoading: Setter,
  selectedId: string,
  reload: (profileId: string) => Promise<void>,
) {
  return {
    handleAddEntity: (data: Omit<EntityMeta, "entityId">) =>
      runMutation(
        setLoading,
        () => createEntity(data),
        () => reload(selectedId),
        "Entity created",
        "Failed to create entity",
      ),
    handleUpdateEntity: (entityId: string, data: Partial<EntityMeta>) =>
      runMutation(
        setLoading,
        () => updateEntity(entityId, data),
        () => reload(selectedId),
        "Entity updated",
        "Failed to update entity",
      ),
    handleDeleteEntity: (entityId: string) =>
      runMutation(
        setLoading,
        () => deleteEntity(entityId),
        () => reload(selectedId),
        "Entity deleted",
        "Failed to delete entity",
        false,
      ),
    handleAddColumn: (data: Omit<ColumnMeta, "columnId">) =>
      runMutation(
        setLoading,
        () => createColumn(data),
        () => reload(selectedId),
        "Column created",
        "Failed to create column",
      ),
    handleDeleteColumn: (columnId: string) =>
      runMutation(
        setLoading,
        () => deleteColumn(columnId),
        () => reload(selectedId),
        "Column deleted",
        "Failed to delete column",
        false,
      ),
  };
}

export async function handleTestConnection(profileId: string) {
  try {
    const result = await testConnection(profileId);
    if (result.status === "success")
      toast.success(`Connection successful (${result.latencyMs}ms)`);
    else toast.error(result.message || "Connection failed");
  } catch {
    toast.error("Failed to test connection");
  }
}

export async function handleDiscoverTables(profileId: string) {
  try {
    const tables = await discoverTables(profileId);
    toast.info(`Found ${tables.length} tables`);
    return tables;
  } catch {
    toast.error("Failed to discover tables");
    return [];
  }
}

async function runMutation(
  setLoading: Setter,
  action: () => Promise<unknown>,
  reload: (() => Promise<void>) | undefined,
  success: string,
  error: string,
  rethrow = true,
) {
  try {
    setLoading(true);
    await action();
    if (reload) await reload();
    toast.success(success);
  } catch (err) {
    toast.error(error);
    if (rethrow) throw err;
  } finally {
    setLoading(false);
  }
}
