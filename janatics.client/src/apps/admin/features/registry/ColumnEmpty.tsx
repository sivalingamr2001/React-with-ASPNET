import { DatabaseIcon } from "lucide-react";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty";

export default function ColumnEmpty() {
  return (
    <Empty className="border px-4 py-10">
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <DatabaseIcon />
        </EmptyMedia>
        <EmptyTitle>No columns configured</EmptyTitle>
        <EmptyDescription>
          Add the first column to describe this entity.
        </EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}
