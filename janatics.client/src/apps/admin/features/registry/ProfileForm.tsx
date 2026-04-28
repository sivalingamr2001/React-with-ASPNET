import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { DbProfile } from "@/services/registryApi";
import { useProfileForm } from "./hooks/useProfileForm";
import ProfileFormFields from "./ProfileFormFields";

interface Props {
  visible: boolean;
  profile?: DbProfile;
  loading?: boolean;
  onSubmit: (data: Omit<DbProfile, "profileId">) => Promise<void>;
  onCancel: () => void;
}

export default function ProfileForm({
  visible,
  profile,
  loading = false,
  onSubmit,
  onCancel,
}: Props) {
  const {
    form,
    isSqlite,
    preview,
    handleInput,
    handlePort,
    handleProvider,
    handleStatus,
  } = useProfileForm({ profile, visible });
  const handleOpenChange = (open: boolean) => !open && onCancel();
  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await onSubmit(form);
  };

  return (
    <Dialog open={visible} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {profile ? "Edit Database Profile" : "Add Database Profile"}
          </DialogTitle>
          <DialogDescription>
            Configure the database source used by the registry.
          </DialogDescription>
        </DialogHeader>
        <form className="grid gap-4" onSubmit={handleSubmit}>
          <ProfileFormFields
            form={form}
            isSqlite={isSqlite}
            onInput={handleInput}
            onPort={handlePort}
            onProvider={handleProvider}
            onStatus={handleStatus}
            preview={preview}
          />
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onCancel}>
              Cancel
            </Button>
            <Button disabled={loading} type="submit">
              {loading ? "Saving..." : "Save Profile"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
