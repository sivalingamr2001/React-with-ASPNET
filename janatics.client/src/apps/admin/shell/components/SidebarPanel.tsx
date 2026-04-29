import { motion } from "framer-motion";
import { Menu } from "lucide-react";

import type { LayoutConfig } from "../types";
import AppSidebarItem from "./AppSidebarItem";

interface Props {
  config: LayoutConfig;
  isCollapsed: boolean;
  items: LayoutConfig["sidebar"]["menu"];
  pathname: string;
  onToggleCollapse: () => void;
}

export default function SidebarPanel({
  config,
  isCollapsed,
  items,
  pathname,
  onToggleCollapse,
}: Props) {
  return (
    <motion.aside
      animate={{ width: isCollapsed ? 92 : 288 }}
      className="h-screen p-3"
    >
      <div className="flex h-full flex-col rounded-[30px] theme-sidebar p-4 backdrop-blur-xl">
        <button
          className="mb-6 flex items-center justify-between rounded-2xl bg-secondary/70 px-3 py-3 text-left"
          onClick={onToggleCollapse}
        >
          <span className="truncate text-sm font-semibold">
            {isCollapsed ? "JC" : config.app.title}
          </span>
          <Menu className="size-4" />
        </button>
        <motion.nav
          className="space-y-2"
          initial="hidden"
          animate="show"
          variants={{
            hidden: {},
            show: { transition: { staggerChildren: 0.06 } },
          }}
        >
          {items.map((item) => (
            <motion.div
              key={item.route}
              variants={{
                hidden: { opacity: 0, x: -12 },
                show: { opacity: 1, x: 0 },
              }}
            >
              <AppSidebarItem
                isCollapsed={isCollapsed}
                item={item}
                pathname={pathname}
              />
            </motion.div>
          ))}
        </motion.nav>
      </div>
    </motion.aside>
  );
}
