import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type { ColumnMeta } from "@/services/registryApi";
import ColumnDeleteButton from "./ColumnDeleteButton";

interface Props {
  columns: ColumnMeta[];
  onDeleteColumn: (columnId: string) => Promise<void>;
}

export default function ColumnTable({ columns, onDeleteColumn }: Props) {
  const rows = columns.map((column) => (
    <TableRow key={column.columnId}>
      <TableCell className="font-medium">{column.columnName}</TableCell>
      <TableCell>
        <Badge variant="secondary">{column.dataType}</Badge>
      </TableCell>
      <TableCell>{column.isPrimaryKey ? "Yes" : "-"}</TableCell>
      <TableCell>{column.isForeignKey ? "Yes" : "-"}</TableCell>
      <TableCell>{column.isRequired ? "Yes" : "-"}</TableCell>
      <TableCell className="text-right">
        <ColumnDeleteButton
          columnId={column.columnId}
          onDelete={onDeleteColumn}
        />
      </TableCell>
    </TableRow>
  ));

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Column Name</TableHead>
          <TableHead>Type</TableHead>
          <TableHead>PK</TableHead>
          <TableHead>FK</TableHead>
          <TableHead>Required</TableHead>
          <TableHead className="text-right">Action</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>{rows}</TableBody>
    </Table>
  );
}
