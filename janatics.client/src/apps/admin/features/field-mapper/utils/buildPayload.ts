import type { ChildRecord } from "../types";

export function buildFieldPayload(
  entity: string,
  transactionId: string,
  values: Record<string, string>,
  children: ChildRecord[],
) {
  const extendedProperties = Object.fromEntries(
    Object.entries(values).filter(([, value]) => value),
  );
  const relProps = children.map((child) => ({
    entityName: child.entity,
    foreignKeyColumn: child.fkColumn,
    recordId: child.values.__recordId || null,
    properties: Object.fromEntries(
      Object.entries(child.values).filter(
        ([key, value]) => key !== "__recordId" && !!value,
      ),
    ),
  }));
  return {
    transactionEntityName: entity,
    transactionId: transactionId || null,
    extendedProperties,
    relProps,
  };
}
