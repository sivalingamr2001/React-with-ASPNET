import { Plus } from "lucide-react";

import { Button } from "@/components/ui/button";

import ChildEntityCard from "./ChildEntityCard";

interface Props {
  availableChildren: string[];
  children: Array<{
    entity: string;
    fkColumn: string;
    id: number;
    values: Record<string, string>;
  }>;
  onAddChild: (entity: string) => void;
  onChildFKChange: (id: number, value: string) => void;
  onChildRemove: (id: number) => void;
  onChildValueChange: (id: number, column: string, value: string) => void;
}

export default function MapperChildren(props: Props) {
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        {props.availableChildren.map((entity) => (
          <Button
            className="rounded-full"
            key={entity}
            onClick={props.onAddChild.bind(null, entity)}
            variant="outline"
          >
            <Plus className="size-4" />
            {entity}
          </Button>
        ))}
      </div>
      <div className="space-y-4">
        {props.children.map((child) => (
          <ChildEntityCard
            child={child}
            key={child.id}
            onFKChange={props.onChildFKChange}
            onRemove={props.onChildRemove}
            onValueChange={props.onChildValueChange}
          />
        ))}
      </div>
    </div>
  );
}
