import { Menu } from "lucide-react";
import { useDeferredValue } from "react";
import { useLocation } from "react-router-dom";

import { Button } from "@/components/ui/button";
import { Sheet, SheetContent } from "@/components/ui/sheet";

import type { LayoutConfig } from "../types";
import { filterMenu } from "../utils/menu";
import SidebarPanel from "./SidebarPanel";

interface Props {
  config: LayoutConfig;
  isCollapsed: boolean;
  isMobileOpen: boolean;
  query: string;
  onMobileOpenChange: (open: boolean) => void;
  onToggleCollapse: () => void;
}

export default function Sidebar(props: Props) {
  const { pathname } = useLocation();
  const deferredQuery = useDeferredValue(props.query);
  const items = filterMenu(props.config.sidebar.menu, deferredQuery);
  const content = (
    <SidebarPanel
      config={props.config}
      isCollapsed={props.isCollapsed}
      items={items}
      onToggleCollapse={props.onToggleCollapse}
      pathname={pathname}
    />
  );
  return (
    <>
      <div className="hidden lg:block">{content}</div>
      <div className="lg:hidden">
        <Button
          className="fixed top-4 left-4 z-50 rounded-full shadow-lg"
          onClick={props.onMobileOpenChange.bind(null, true)}
          size="icon"
        >
          <Menu className="size-4" />
        </Button>
        <Sheet
          onOpenChange={props.onMobileOpenChange}
          open={props.isMobileOpen}
        >
          <SheetContent
            className="w-[92vw] max-w-xs border-none bg-transparent p-3 shadow-none"
            side="left"
            showCloseButton={false}
          >
            {content}
          </SheetContent>
        </Sheet>
      </div>
    </>
  );
}
