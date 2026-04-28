import { motion } from "framer-motion";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";

import MapperChildren from "./MapperChildren";
import FieldColumnRow from "./FieldColumnRow";

interface Props {
  availableChildren: string[];
  children: Array<{
    entity: string;
    fkColumn: string;
    id: number;
    values: Record<string, string>;
  }>;
  columns: Array<{
    isPK: boolean;
    name: string;
    required: boolean;
    type: "string" | "number" | "decimal" | "date";
  }>;
  mappedCount: number;
  missingRequired: Array<{ name: string }>;
  onAddChild: (entity: string) => void;
  onChildFKChange: (id: number, value: string) => void;
  onChildRemove: (id: number) => void;
  onChildValueChange: (id: number, column: string, value: string) => void;
  onFieldChange: (name: string, value: string) => void;
  values: Record<string, string>;
}

export default function MapperCanvas(props: Props) {
  return (
    <motion.div
      className="space-y-4"
      initial={{ opacity: 0, y: 18 }}
      animate={{ opacity: 1, y: 0 }}
    >
      <Card className="rounded-[28px] border-white/60 bg-card/85">
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Main Entity Mapping</span>
            <span className="text-sm font-normal text-muted-foreground">
              {props.mappedCount}/{props.columns.length} mapped
            </span>
          </CardTitle>
          <Progress
            className="h-2 rounded-full"
            value={
              (props.mappedCount / Math.max(props.columns.length, 1)) * 100
            }
          />
        </CardHeader>
        <CardContent className="space-y-3">
          {props.columns.map((column) => (
            <FieldColumnRow
              column={column}
              key={column.name}
              onChange={props.onFieldChange}
              value={props.values[column.name] || ""}
            />
          ))}
          {!!props.missingRequired.length && (
            <p className="text-sm text-destructive">
              {props.missingRequired.map((item) => item.name).join(", ")}{" "}
              required
            </p>
          )}
        </CardContent>
      </Card>
      <MapperChildren
        availableChildren={props.availableChildren}
        children={props.children}
        onAddChild={props.onAddChild}
        onChildFKChange={props.onChildFKChange}
        onChildRemove={props.onChildRemove}
        onChildValueChange={props.onChildValueChange}
      />
    </motion.div>
  );
}
