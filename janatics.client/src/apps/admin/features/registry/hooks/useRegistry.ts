import { useEffect, useState } from "react";
import { toast } from "sonner";
import {
  getEntities,
  getProfiles,
  type ColumnMeta,
  type DbProfile,
  type EntityMeta,
} from "@/services/registryApi";
import {
  createEntityActions,
  createProfileActions,
  handleDiscoverTables,
  handleTestConnection,
} from "../utils/registryMutations";
import { loadColumnsByEntity } from "../utils/loadColumns";

export function useRegistry() {
  const [profiles, setProfiles] = useState<DbProfile[]>([]);
  const [selectedProfileId, setSelectedProfileId] = useState("");
  const [entities, setEntities] = useState<EntityMeta[]>([]);
  const [columnsByEntity, setColumnsByEntity] = useState<
    Record<string, ColumnMeta[]>
  >({});
  const [loading, setLoading] = useState(false);
  const [profileLoading, setProfileLoading] = useState(false);

  async function loadProfiles() {
    try {
      setLoading(true);
      const data = await getProfiles();
      setProfiles(data);
      if (!selectedProfileId && data[0])
        setSelectedProfileId(data[0].profileId);
    } catch {
      toast.error("Failed to load profiles");
    } finally {
      setLoading(false);
    }
  }

  async function loadEntities(profileId: string) {
    try {
      setProfileLoading(true);
      const data = await getEntities(profileId);
      setEntities(data);
      setColumnsByEntity(await loadColumnsByEntity(data));
    } catch {
      setEntities([]);
      toast.error("Failed to load entities");
    } finally {
      setProfileLoading(false);
    }
  }

  useEffect(() => {
    if (selectedProfileId) loadEntities(selectedProfileId);
  }, [selectedProfileId]);

  return {
    profiles,
    selectedProfileId,
    entities,
    columnsByEntity,
    loading,
    profileLoading,
    setSelectedProfileId,
    loadProfiles,
    loadEntities,
    ...createProfileActions(
      setLoading,
      loadProfiles,
      selectedProfileId,
      setSelectedProfileId,
    ),
    ...createEntityActions(setProfileLoading, selectedProfileId, loadEntities),
    handleTestConnection,
    handleDiscoverTables,
  };
}
