import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import type { ColumnMeta, EntityMeta } from "@/services/registryApi";
import ColumnEmpty from "./ColumnEmpty";
import ColumnSummary from "./ColumnSummary";

interface Props {
  entity: EntityMeta;
  columns: ColumnMeta[];
}

export default function EntityDetails({ entity, columns }: Props) {
  const columnItems = columns.map((column) => (
    <ColumnSummary key={column.columnId} column={column} />
  ));

  return (
    <div className="space-y-2">
      <p>
        <span className="font-medium">Schema:</span> {entity.schemaName}
      </p>
      <p>
        <span className="font-medium">Primary Key:</span> {entity.pkColumn}
      </p>
      {entity.softDeleteColumn && (
        <p>
          <span className="font-medium">Soft Delete:</span>{" "}
          {entity.softDeleteColumn} = {entity.softDeleteValue}
        </p>
      )}
      {entity.allowedRoles && (
        <p>
          <span className="font-medium">Allowed Roles:</span>{" "}
          {entity.allowedRoles}
        </p>
      )}
      {columns.length > 0 ? (
        <Accordion type="single" collapsible>
          <AccordionItem value={entity.entityId}>
            <AccordionTrigger>Columns ({columns.length})</AccordionTrigger>
            <AccordionContent className="space-y-2">
              {columnItems}
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      ) : (
        <ColumnEmpty />
      )}
    </div>
  );
}
