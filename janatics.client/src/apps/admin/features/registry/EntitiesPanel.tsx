import { PlusIcon, SearchIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardAction,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
} from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import type { ColumnMeta, EntityMeta } from "@/services/registryApi";
import EntityCard from "./EntityCard";

interface Props {
  entities: EntityMeta[];
  loading: boolean;
  searchQuery: string;
  getColumns: (entityId: string) => ColumnMeta[];
  onAddEntity: () => void;
  onDeleteEntity: (entityId: string) => void;
  onEditEntity: (entity: EntityMeta) => void;
  onManageColumns: (entity: EntityMeta) => void;
  onSearchChange: (value: string) => void;
}

export default function EntitiesPanel({
  entities,
  loading,
  searchQuery,
  getColumns,
  onAddEntity,
  onDeleteEntity,
  onEditEntity,
  onManageColumns,
  onSearchChange,
}: Props) {
  return (
    <Card>
      <CardHeader className="border-b">
        <CardTitle>Entities</CardTitle>
        <CardAction className="flex flex-wrap gap-2">
          <div className="relative min-w-56">
            <SearchIcon className="absolute top-2.5 left-3 size-4 text-muted-foreground" />
            <Input
              className="pl-9"
              placeholder="Search entities..."
              value={searchQuery}
              onChange={(event) => onSearchChange(event.target.value)}
            />
          </div>
          <Button onClick={onAddEntity}>
            <PlusIcon />
            New Entity
          </Button>
        </CardAction>
      </CardHeader>
      <CardContent className="space-y-3">
        {loading && <Spinner className="mx-auto my-8 size-5" />}
        {!loading && entities.length === 0 && (
          <Empty className="border px-4 py-10">
            <EmptyHeader>
              <EmptyTitle>No entities configured</EmptyTitle>
              <EmptyDescription>
                Add an entity for the selected profile.
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        )}
        {entities.map((entity) => (
          <EntityCard
            key={entity.entityId}
            columns={getColumns(entity.entityId)}
            entity={entity}
            onDelete={onDeleteEntity}
            onEdit={onEditEntity}
            onManageColumns={(current) => onManageColumns(current)}
          />
        ))}
      </CardContent>
    </Card>
  );
}
