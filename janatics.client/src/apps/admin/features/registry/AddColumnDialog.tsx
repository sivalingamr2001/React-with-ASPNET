import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { ColumnMeta } from "@/services/registryApi";
import ColumnFormFields from "./ColumnFormFields";
import type { ColumnFormValues } from "./types";

interface Props {
  open: boolean;
  loading: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (column: Omit<ColumnMeta, "columnId">) => Promise<void>;
  entityId?: string;
}

const defaultValues: ColumnFormValues = {
  columnName: "",
  dataType: "string",
  isRequired: false,
  isPrimaryKey: false,
  isForeignKey: false,
  fkReference: "",
};

export default function AddColumnDialog({
  open,
  loading,
  onOpenChange,
  onSubmit,
  entityId,
}: Props) {
  const [form, setForm] = useState<ColumnFormValues>(defaultValues);

  useEffect(() => {
    if (open) setForm(defaultValues);
  }, [open]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await onSubmit({ ...form, entityId: entityId || "" });
    onOpenChange(false);
  };
  const handleChecked =
    (key: keyof ColumnFormValues) => (checked: boolean | "indeterminate") =>
      setForm((current) => ({ ...current, [key]: checked === true }));
  const handleInput =
    (key: keyof ColumnFormValues) =>
    (event: React.ChangeEvent<HTMLInputElement>) =>
      setForm((current) => ({ ...current, [key]: event.target.value }));
  const handleTypeChange = (dataType: ColumnMeta["dataType"]) =>
    setForm((current) => ({ ...current, dataType }));

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add Column</DialogTitle>
          <DialogDescription>
            Create a new field mapping for this entity.
          </DialogDescription>
        </DialogHeader>
        <form className="grid gap-4" onSubmit={handleSubmit}>
          <ColumnFormFields
            form={form}
            onChecked={handleChecked}
            onInput={handleInput}
            onTypeChange={handleTypeChange}
          />
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              Cancel
            </Button>
            <Button disabled={loading} type="submit">
              {loading ? "Saving..." : "Add Column"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
