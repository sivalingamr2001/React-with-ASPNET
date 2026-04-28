import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { QUERY_ENTITIES } from "../data";

interface Props {
  entity: string;
  onEntityChange: (value: string) => void;
}

export default function QueryToolbar({ entity, onEntityChange }: Props) {
  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardContent className="flex flex-col gap-4 pt-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="space-y-2">
          <p className="text-xs uppercase tracking-[0.24em] text-muted-foreground">
            Root Entity
          </p>
          <Select onValueChange={onEntityChange} value={entity}>
            <SelectTrigger className="h-11 w-full rounded-2xl lg:w-72">
              <SelectValue placeholder="Select table" />
            </SelectTrigger>
            <SelectContent>
              {QUERY_ENTITIES.map((item) => (
                <SelectItem key={item} value={item}>
                  {item}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button className="rounded-full px-5" variant="outline">
          Fetch Preview
        </Button>
      </CardContent>
    </Card>
  );
}
