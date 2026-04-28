import {
  getColumns,
  type ColumnMeta,
  type EntityMeta,
} from "@/services/registryApi";

export async function loadColumnsByEntity(entities: EntityMeta[]) {
  const pairs = await Promise.all(
    entities.map(async (entity) => {
      try {
        const columns = await getColumns(entity.entityId);
        return [entity.entityId, columns] as const;
      } catch {
        return [entity.entityId, [] as ColumnMeta[]] as const;
      }
    }),
  );

  return Object.fromEntries(pairs);
}
