import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { EntityMeta } from "@/services/registryApi";
import EntityFormFields from "./EntityFormFields";
import type { EntityFormValues } from "./types";

interface Props {
  visible: boolean;
  entity?: EntityMeta;
  profileId?: string;
  loading?: boolean;
  onSubmit: (data: Omit<EntityMeta, "entityId">) => Promise<void>;
  onCancel: () => void;
}

const defaultValues: EntityFormValues = {
  profileId: "",
  entityName: "",
  schemaName: "dbo",
  pkColumn: "Id",
  isReadOnly: false,
  allowedRoles: "",
  softDeleteColumn: "",
  softDeleteValue: "",
};

export default function EntityForm({
  visible,
  entity,
  profileId,
  loading = false,
  onSubmit,
  onCancel,
}: Props) {
  const [form, setForm] = useState<EntityFormValues>(defaultValues);

  useEffect(() => {
    if (visible)
      setForm(
        entity
          ? { ...defaultValues, ...entity }
          : { ...defaultValues, profileId: profileId || "" },
      );
  }, [entity, profileId, visible]);

  const handleOpenChange = (open: boolean) => !open && onCancel();
  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await onSubmit({
      ...form,
      profileId: entity?.profileId || profileId || "",
    });
  };
  const handleInput =
    (key: keyof EntityFormValues) =>
    (event: React.ChangeEvent<HTMLInputElement>) =>
      setForm((current) => ({ ...current, [key]: event.target.value }));
  const handleReadOnly = (checked: boolean | "indeterminate") =>
    setForm((current) => ({ ...current, isReadOnly: checked === true }));

  return (
    <Dialog open={visible} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{entity ? "Edit Entity" : "Add Entity"}</DialogTitle>
          <DialogDescription>
            Define the metadata exposed for this registry entity.
          </DialogDescription>
        </DialogHeader>
        <form className="grid gap-4" onSubmit={handleSubmit}>
          <EntityFormFields
            form={form}
            onInput={handleInput}
            onReadOnly={handleReadOnly}
          />
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onCancel}>
              Cancel
            </Button>
            <Button disabled={loading} type="submit">
              {loading ? "Saving..." : "Save Entity"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
