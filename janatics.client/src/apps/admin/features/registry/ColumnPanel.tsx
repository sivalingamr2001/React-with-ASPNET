import { useState } from "react";
import { PlusIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import type { ColumnMeta, EntityMeta } from "@/services/registryApi";
import AddColumnDialog from "./AddColumnDialog";
import ColumnTable from "./ColumnTable";

interface Props {
  visible: boolean;
  entity?: EntityMeta;
  columns: ColumnMeta[];
  loading?: boolean;
  onAddColumn: (column: Omit<ColumnMeta, "columnId">) => Promise<void>;
  onDeleteColumn: (columnId: string) => Promise<void>;
  onClose: () => void;
}

export default function ColumnPanel({
  visible,
  entity,
  columns,
  loading = false,
  onAddColumn,
  onDeleteColumn,
  onClose,
}: Props) {
  const [isAddOpen, setIsAddOpen] = useState(false);
  const handleOpenChange = (open: boolean) => !open && onClose();
  const handleAddOpen = () => setIsAddOpen(true);

  return (
    <>
      <Sheet open={visible} onOpenChange={handleOpenChange}>
        <SheetContent className="w-full gap-0 sm:max-w-3xl">
          <SheetHeader>
            <SheetTitle>Columns: {entity?.entityName}</SheetTitle>
            <SheetDescription>
              Manage field mappings for the selected entity.
            </SheetDescription>
          </SheetHeader>
          <div className="space-y-4 p-4 pt-0">
            <Button className="w-full" onClick={handleAddOpen}>
              <PlusIcon />
              Add Column
            </Button>
            {columns.length > 0 ? (
              <ColumnTable columns={columns} onDeleteColumn={onDeleteColumn} />
            ) : (
              <Empty className="border px-4 py-10">
                <EmptyHeader>
                  <EmptyMedia variant="icon">
                    <PlusIcon />
                  </EmptyMedia>
                  <EmptyTitle>No columns yet</EmptyTitle>
                  <EmptyDescription>
                    Add a column to start building this entity definition.
                  </EmptyDescription>
                </EmptyHeader>
              </Empty>
            )}
            {loading && (
              <p className="text-sm text-muted-foreground">
                Refreshing column metadata...
              </p>
            )}
          </div>
        </SheetContent>
      </Sheet>
      <AddColumnDialog
        entityId={entity?.entityId}
        loading={loading}
        onOpenChange={setIsAddOpen}
        onSubmit={onAddColumn}
        open={isAddOpen}
      />
    </>
  );
}
