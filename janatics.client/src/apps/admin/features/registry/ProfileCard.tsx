import { PencilLineIcon, Trash2Icon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import type { DbProfile } from "@/services/registryApi";

interface Props {
  profile: DbProfile;
  isSelected: boolean;
  entityCount?: number;
  onSelect: (profileId: string) => void;
  onEdit: (profile: DbProfile) => void;
  onDelete: (profileId: string) => void;
}

const providerIcons = { Oracle: "⊙", SqlServer: "◆", Sqlite: "◯" };
const statusVariants = {
  active: "default",
  inactive: "secondary",
  error: "destructive",
} as const;

export default function ProfileCard({
  profile,
  isSelected,
  entityCount = 0,
  onSelect,
  onEdit,
  onDelete,
}: Props) {
  const handleSelect = () => onSelect(profile.profileId);
  const handleEdit = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    onEdit(profile);
  };
  const handleDelete = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    onDelete(profile.profileId);
  };

  return (
    <Card
      className={cn(
        "cursor-pointer border transition-colors hover:border-primary/40",
        isSelected && "border-primary bg-primary/5",
      )}
      onClick={handleSelect}
      size="sm"
    >
      <CardContent className="space-y-3">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 space-y-1">
            <div className="flex items-center gap-2">
              <span className="text-base text-muted-foreground">
                {providerIcons[profile.provider]}
              </span>
              <span className="truncate font-semibold">
                {profile.profileName}
              </span>
            </div>
            <p className="text-xs text-muted-foreground">
              {entityCount} entit{entityCount === 1 ? "y" : "ies"}
            </p>
          </div>
          <Badge variant={statusVariants[profile.status]}>
            {profile.status}
          </Badge>
        </div>
        <div className="flex gap-1">
          <Button size="icon-sm" variant="ghost" onClick={handleEdit}>
            <PencilLineIcon />
          </Button>
          <Button size="icon-sm" variant="ghost" onClick={handleDelete}>
            <Trash2Icon />
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
