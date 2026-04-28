import { useEffect, useState } from "react";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
} from "@/components/ui/empty";
import type { DbProfile, EntityMeta } from "@/services/registryApi";
import ColumnPanel from "./ColumnPanel";
import ConfirmDialog from "./ConfirmDialog";
import DiscoveredTablesDialog from "./DiscoveredTablesDialog";
import EntitiesPanel from "./EntitiesPanel";
import EntityForm from "./EntityForm";
import ProfileForm from "./ProfileForm";
import ProfileSidebar from "./ProfileSidebar";
import SelectedProfileCard from "./SelectedProfileCard";
import { useRegistry } from "./hooks/useRegistry";

export default function RegistryWorkspace() {
  const registry = useRegistry();
  const [profileFormVisible, setProfileFormVisible] = useState(false);
  const [entityFormVisible, setEntityFormVisible] = useState(false);
  const [columnPanelVisible, setColumnPanelVisible] = useState(false);
  const [profileSearch, setProfileSearch] = useState("");
  const [entitySearch, setEntitySearch] = useState("");
  const [discoveredTables, setDiscoveredTables] = useState<string[]>([]);
  const [editingProfile, setEditingProfile] = useState<DbProfile>();
  const [editingEntity, setEditingEntity] = useState<EntityMeta>();
  const [deleteProfileId, setDeleteProfileId] = useState("");
  const [deleteEntityId, setDeleteEntityId] = useState("");

  useEffect(() => {
    registry.loadProfiles();
  }, []);

  const selectedProfile = registry.profiles.find(
    (item) => item.profileId === registry.selectedProfileId,
  );
  const filteredProfiles = registry.profiles.filter((item) =>
    item.profileName.toLowerCase().includes(profileSearch.toLowerCase()),
  );
  const filteredEntities = registry.entities.filter((item) =>
    item.entityName.toLowerCase().includes(entitySearch.toLowerCase()),
  );
  const handleDiscoverTables = async () => {
    if (!selectedProfile) return;
    const tables = await registry.handleDiscoverTables(
      selectedProfile.profileId,
    );
    setDiscoveredTables(tables);
  };
  const handleSubmitProfile = async (data: Omit<DbProfile, "profileId">) => {
    if (editingProfile)
      await registry.handleUpdateProfile(editingProfile.profileId, data);
    else await registry.handleAddProfile(data);
    setProfileFormVisible(false);
    setEditingProfile(undefined);
  };
  const handleSubmitEntity = async (data: Omit<EntityMeta, "entityId">) => {
    if (editingEntity)
      await registry.handleUpdateEntity(editingEntity.entityId, data);
    else await registry.handleAddEntity(data);
    setEntityFormVisible(false);
    setEditingEntity(undefined);
  };

  return (
    <div className="grid gap-4 xl:grid-cols-[18rem_minmax(0,1fr)]">
      <ProfileSidebar
        getEntityCount={() => registry.entities.length}
        loading={registry.loading}
        onAddProfile={() => {
          setEditingProfile(undefined);
          setProfileFormVisible(true);
        }}
        onDeleteProfile={setDeleteProfileId}
        onEditProfile={(profile) => {
          setEditingProfile(profile);
          setProfileFormVisible(true);
        }}
        onSearchChange={setProfileSearch}
        onSelectProfile={registry.setSelectedProfileId}
        profiles={filteredProfiles}
        searchQuery={profileSearch}
        selectedProfileId={registry.selectedProfileId}
      />
      <div className="space-y-4">
        {!selectedProfile && (
          <Empty className="border px-6 py-16">
            <EmptyHeader>
              <EmptyTitle>Select a profile to begin</EmptyTitle>
              <EmptyDescription>
                Choose a database profile from the left to manage entities.
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        )}
        {selectedProfile && (
          <SelectedProfileCard
            entityCount={registry.entities.length}
            loading={registry.profileLoading}
            onDiscoverTables={handleDiscoverTables}
            onRefresh={() => registry.loadEntities(selectedProfile.profileId)}
            onTestConnection={() =>
              registry.handleTestConnection(selectedProfile.profileId)
            }
            profile={selectedProfile}
          />
        )}
        {selectedProfile && (
          <EntitiesPanel
            entities={filteredEntities}
            getColumns={(entityId) => registry.columnsByEntity[entityId] || []}
            loading={registry.profileLoading}
            onAddEntity={() => {
              setEditingEntity(undefined);
              setEntityFormVisible(true);
            }}
            onDeleteEntity={setDeleteEntityId}
            onEditEntity={(entity) => {
              setEditingEntity(entity);
              setEntityFormVisible(true);
            }}
            onManageColumns={(entity) => {
              setEditingEntity(entity);
              setColumnPanelVisible(true);
            }}
            onSearchChange={setEntitySearch}
            searchQuery={entitySearch}
          />
        )}
      </div>
      <ProfileForm
        visible={profileFormVisible}
        profile={editingProfile}
        loading={registry.loading}
        onSubmit={handleSubmitProfile}
        onCancel={() => {
          setProfileFormVisible(false);
          setEditingProfile(undefined);
        }}
      />
      <EntityForm
        visible={entityFormVisible}
        entity={editingEntity}
        profileId={selectedProfile?.profileId}
        loading={registry.profileLoading}
        onSubmit={handleSubmitEntity}
        onCancel={() => {
          setEntityFormVisible(false);
          setEditingEntity(undefined);
        }}
      />
      <ColumnPanel
        visible={columnPanelVisible}
        entity={editingEntity}
        columns={
          editingEntity
            ? registry.columnsByEntity[editingEntity.entityId] || []
            : []
        }
        loading={registry.profileLoading}
        onAddColumn={registry.handleAddColumn}
        onDeleteColumn={registry.handleDeleteColumn}
        onClose={() => {
          setColumnPanelVisible(false);
          setEditingEntity(undefined);
        }}
      />
      <ConfirmDialog
        description="Are you sure you want to delete this profile? All associated entities will be deleted."
        isOpen={!!deleteProfileId}
        onConfirm={() => registry.handleDeleteProfile(deleteProfileId)}
        onOpenChange={(open) => !open && setDeleteProfileId("")}
        title="Delete Profile"
      />
      <ConfirmDialog
        description="Are you sure you want to delete this entity and all its columns?"
        isOpen={!!deleteEntityId}
        onConfirm={() => registry.handleDeleteEntity(deleteEntityId)}
        onOpenChange={(open) => !open && setDeleteEntityId("")}
        title="Delete Entity"
      />
      <DiscoveredTablesDialog
        isOpen={discoveredTables.length > 0}
        tables={discoveredTables}
        onOpenChange={(open) => !open && setDiscoveredTables([])}
      />
    </div>
  );
}
