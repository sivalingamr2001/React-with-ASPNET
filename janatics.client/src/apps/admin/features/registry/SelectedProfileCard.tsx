import { DatabaseIcon, RadarIcon, RefreshCcwIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardAction,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import type { DbProfile } from "@/services/registryApi";

interface Props {
  entityCount: number;
  loading: boolean;
  profile: DbProfile;
  onDiscoverTables: () => void;
  onRefresh: () => void;
  onTestConnection: () => void;
}

export default function SelectedProfileCard({
  entityCount,
  loading,
  profile,
  onDiscoverTables,
  onRefresh,
  onTestConnection,
}: Props) {
  return (
    <Card>
      <CardHeader className="border-b">
        <CardTitle className="flex items-center gap-2">
          <DatabaseIcon className="size-4 text-muted-foreground" />
          {profile.profileName}
        </CardTitle>
        <CardAction className="flex flex-wrap gap-2">
          <Button
            disabled={loading}
            size="sm"
            variant="outline"
            onClick={onRefresh}
          >
            <RefreshCcwIcon />
            Refresh
          </Button>
          <Button size="sm" variant="outline" onClick={onTestConnection}>
            Test Connection
          </Button>
          <Button size="sm" onClick={onDiscoverTables}>
            <RadarIcon />
            Auto-Discover
          </Button>
        </CardAction>
      </CardHeader>
      <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <div>
          <p className="text-xs uppercase text-muted-foreground">Provider</p>
          <p className="font-medium">{profile.provider}</p>
        </div>
        <div>
          <p className="text-xs uppercase text-muted-foreground">Database</p>
          <p className="font-medium">{profile.database}</p>
        </div>
        <div>
          <p className="text-xs uppercase text-muted-foreground">Status</p>
          <Badge
            variant={
              profile.status === "error"
                ? "destructive"
                : profile.status === "inactive"
                  ? "secondary"
                  : "default"
            }
          >
            {profile.status}
          </Badge>
        </div>
        <div>
          <p className="text-xs uppercase text-muted-foreground">Entities</p>
          <p className="font-medium">{entityCount}</p>
        </div>
      </CardContent>
    </Card>
  );
}
