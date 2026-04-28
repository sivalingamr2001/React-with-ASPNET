import { Bell } from "lucide-react";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

import type { LayoutConfig } from "../types";
import { getIcon } from "../utils/iconMap";

export default function HeaderMenu({ config }: { config: LayoutConfig }) {
  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button className="rounded-full" size="icon" variant="outline">
            <Bell className="size-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {config.header.notifications.map((item) => (
            <NotificationItem item={item} key={item.id} />
          ))}
          <DropdownMenuSeparator />
          <DropdownMenuItem>Notification center</DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      <ProfileMenu config={config} />
    </>
  );
}

function NotificationItem({
  item,
}: {
  item: LayoutConfig["header"]["notifications"][number];
}) {
  const Icon = getIcon(item.icon);
  return (
    <DropdownMenuItem className="flex items-start gap-3">
      <Icon className="mt-0.5 size-4 text-primary" />
      <span>
        <span className="block">{item.label}</span>
        <span className="block text-xs text-muted-foreground">
          {item.detail}
        </span>
      </span>
    </DropdownMenuItem>
  );
}

function ProfileMenu({ config }: { config: LayoutConfig }) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button className="flex items-center gap-3 rounded-full border border-white/60 bg-background/80 px-2 py-1.5">
          <Avatar className="size-9">
            <AvatarFallback>
              {config.header.profile.name.slice(0, 2)}
            </AvatarFallback>
          </Avatar>
          <span className="hidden text-left md:block">
            <span className="block text-sm font-medium">
              {config.header.profile.name}
            </span>
            <span className="block text-xs text-muted-foreground">
              {config.header.profile.role}
            </span>
          </span>
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem>{config.header.profile.name}</DropdownMenuItem>
        <DropdownMenuItem>{config.header.profile.role}</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
