import { motion } from "framer-motion";
import { ChevronRight } from "lucide-react";
import { useMemo, useState } from "react";
import { NavLink } from "react-router-dom";

import { cn } from "@/lib/utils";

import type { AdminMenuItem } from "../types";
import { getIcon } from "../utils/iconMap";
import { isBranchActive } from "../utils/menu";

interface Props {
  isCollapsed: boolean;
  item: AdminMenuItem;
  pathname: string;
}

export default function AppSidebarItem({ item, pathname, isCollapsed }: Props) {
  const Icon = getIcon(item.icon);
  const isActive = isBranchActive(item, pathname);
  const [isOpen, setIsOpen] = useState(isActive);
  const showChildren = !!item.children?.length && !isCollapsed;
  const linkClass = useMemo(
    () => cn("flex items-center gap-3 rounded-2xl px-3 py-3 text-sm"),
    [],
  );

  const handleToggle = () => setIsOpen((value: boolean) => !value);

  if (!showChildren) {
    return (
      <NavLink
        className={cn(
          linkClass,
          isActive && "bg-primary text-primary-foreground",
        )}
        to={item.route}
      >
        <Icon className="size-4" />
        {!isCollapsed && <span className="truncate">{item.label}</span>}
      </NavLink>
    );
  }

  return (
    <div className="space-y-2">
      <button
        className={cn(
          linkClass,
          "w-full justify-between",
          isActive && "bg-secondary",
        )}
        onClick={handleToggle}
      >
        <span className="flex items-center gap-3">
          <Icon className="size-4" />
          <span>{item.label}</span>
        </span>
        <ChevronRight
          className={cn("size-4 transition", isOpen && "rotate-90")}
        />
      </button>
      <motion.div
        animate={{ height: isOpen ? "auto" : 0, opacity: isOpen ? 1 : 0 }}
        className="overflow-hidden pl-4"
      >
        <div className="space-y-2 border-l border-border/80 pl-4">
          {item.children?.map((child) => (
            <AppSidebarItem
              isCollapsed={false}
              item={child}
              key={child.route}
              pathname={pathname}
            />
          ))}
        </div>
      </motion.div>
    </div>
  );
}
