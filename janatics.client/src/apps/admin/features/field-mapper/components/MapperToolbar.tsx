import { Sparkles } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { FIELD_ENTITIES } from "../data";

interface Props {
  isUpdate: boolean;
  selectedEntity: string;
  transactionId: string;
  onEntityChange: (value: string) => void;
  onModeChange: (value: boolean) => void;
  onTransactionChange: (value: string) => void;
}

export default function MapperToolbar(props: Props) {
  const handleEntityChange = (value: string) => props.onEntityChange(value);
  const handleTransactionChange = (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => props.onTransactionChange(event.target.value);

  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardContent className="grid gap-4 pt-4 lg:grid-cols-3">
        <div className="space-y-2">
          <p className="text-xs uppercase tracking-[0.24em] text-muted-foreground">
            Entity
          </p>
          <Select
            onValueChange={handleEntityChange}
            value={props.selectedEntity}
          >
            <SelectTrigger className="h-11 rounded-2xl">
              <SelectValue placeholder="Select entity" />
            </SelectTrigger>
            <SelectContent>
              {FIELD_ENTITIES.map((entity) => (
                <SelectItem key={entity} value={entity}>
                  {entity}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <p className="text-xs uppercase tracking-[0.24em] text-muted-foreground">
            Mode
          </p>
          <div className="flex gap-2">
            <Button
              className="flex-1 rounded-2xl"
              onClick={props.onModeChange.bind(null, false)}
              variant={props.isUpdate ? "outline" : "default"}
            >
              Insert
            </Button>
            <Button
              className="flex-1 rounded-2xl"
              onClick={props.onModeChange.bind(null, true)}
              variant={props.isUpdate ? "default" : "outline"}
            >
              Update
            </Button>
          </div>
        </div>
        <div className="space-y-2">
          <p className="text-xs uppercase tracking-[0.24em] text-muted-foreground">
            Transaction ID
          </p>
          <div className="relative">
            <Sparkles className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="h-11 rounded-2xl pl-9"
              disabled={!props.isUpdate}
              onChange={handleTransactionChange}
              placeholder="Existing record ID"
              value={props.transactionId}
            />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
