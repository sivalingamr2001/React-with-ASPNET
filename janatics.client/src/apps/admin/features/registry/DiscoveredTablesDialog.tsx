import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

interface Props {
  isOpen: boolean;
  tables: string[];
  onOpenChange: (open: boolean) => void;
}

export default function DiscoveredTablesDialog({
  isOpen,
  tables,
  onOpenChange,
}: Props) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Discovered Tables</DialogTitle>
          <DialogDescription>
            Tables returned by the selected profile connection.
          </DialogDescription>
        </DialogHeader>
        <div className="max-h-96 space-y-2 overflow-y-auto">
          {tables.map((table) => (
            <div key={table} className="rounded-lg border px-3 py-2 text-sm">
              {table}
            </div>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  );
}
