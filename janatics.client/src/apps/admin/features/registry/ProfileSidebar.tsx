import { PlusIcon, SearchIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
} from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import type { DbProfile } from "@/services/registryApi";
import ProfileCard from "./ProfileCard";

interface Props {
  loading: boolean;
  profiles: DbProfile[];
  selectedProfileId: string;
  searchQuery: string;
  getEntityCount: (profileId: string) => number;
  onAddProfile: () => void;
  onDeleteProfile: (profileId: string) => void;
  onEditProfile: (profile: DbProfile) => void;
  onSearchChange: (value: string) => void;
  onSelectProfile: (profileId: string) => void;
}

export default function ProfileSidebar({
  loading,
  profiles,
  selectedProfileId,
  searchQuery,
  getEntityCount,
  onAddProfile,
  onDeleteProfile,
  onEditProfile,
  onSearchChange,
  onSelectProfile,
}: Props) {
  return (
    <aside className="space-y-3 rounded-xl border bg-card p-4">
      <Button className="w-full" onClick={onAddProfile}>
        <PlusIcon />
        New Profile
      </Button>
      <div className="relative">
        <SearchIcon className="absolute top-2.5 left-3 size-4 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Search profiles..."
          value={searchQuery}
          onChange={(event) => onSearchChange(event.target.value)}
        />
      </div>
      <div className="max-h-[calc(100vh-18rem)] space-y-2 overflow-y-auto">
        {loading && <Spinner className="mx-auto mt-6 size-5" />}
        {!loading && profiles.length === 0 && (
          <Empty className="border px-4 py-10">
            <EmptyHeader>
              <EmptyTitle>No profiles</EmptyTitle>
              <EmptyDescription>
                Create a database profile to begin.
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        )}
        {profiles.map((profile) => (
          <ProfileCard
            key={profile.profileId}
            entityCount={getEntityCount(profile.profileId)}
            isSelected={profile.profileId === selectedProfileId}
            onDelete={onDeleteProfile}
            onEdit={onEditProfile}
            onSelect={onSelectProfile}
            profile={profile}
          />
        ))}
      </div>
    </aside>
  );
}
