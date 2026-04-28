import {
  DatabaseIcon,
  PencilLineIcon,
  Settings2Icon,
  Trash2Icon,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardAction,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import type { ColumnMeta, EntityMeta } from "@/services/registryApi";
import EntityDetails from "./EntityDetails";

interface Props {
  entity: EntityMeta;
  columns: ColumnMeta[];
  onEdit: (entity: EntityMeta) => void;
  onDelete: (entityId: string) => void;
  onManageColumns: (entity: EntityMeta, columns: ColumnMeta[]) => void;
}

export default function EntityCard({
  entity,
  columns,
  onEdit,
  onDelete,
  onManageColumns,
}: Props) {
  const handleEdit = () => onEdit(entity);
  const handleDelete = () => onDelete(entity.entityId);
  const handleManageColumns = () => onManageColumns(entity, columns);

  return (
    <Card size="sm">
      <CardHeader className="border-b">
        <CardTitle className="flex items-center gap-2">
          <DatabaseIcon className="size-4 text-muted-foreground" />
          <span>{entity.entityName}</span>
          {entity.isReadOnly && <Badge variant="destructive">Read-only</Badge>}
        </CardTitle>
        <CardAction className="flex gap-1">
          <Button size="icon-sm" variant="ghost" onClick={handleEdit}>
            <PencilLineIcon />
          </Button>
          <Button size="icon-sm" variant="ghost" onClick={handleManageColumns}>
            <Settings2Icon />
          </Button>
          <Button size="icon-sm" variant="ghost" onClick={handleDelete}>
            <Trash2Icon />
          </Button>
        </CardAction>
      </CardHeader>
      <CardContent>
        <EntityDetails columns={columns} entity={entity} />
      </CardContent>
    </Card>
  );
}
