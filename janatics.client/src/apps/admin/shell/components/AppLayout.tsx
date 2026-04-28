import { useEffect, useState } from "react";

import type { LayoutConfig } from "../types";
import Content from "./Content";
import Header from "./Header";
import Sidebar from "./Sidebar";

interface Props {
  config: LayoutConfig;
}

export default function AppLayout({ config }: Props) {
  const [isCollapsed, setIsCollapsed] = useState(config.sidebar.collapsed);
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const [isScrolled, setIsScrolled] = useState(false);
  const [query, setQuery] = useState("");

  useEffect(() => {
    document.documentElement.classList.toggle("dark", config.theme.darkMode);
  }, [config.theme.darkMode]);

  const handleToggleCollapse = () => setIsCollapsed((value) => !value);

  return (
    <div
      className="min-h-screen bg-background"
      style={{ backgroundImage: config.theme.shellGradient }}
    >
      <div className="flex">
        <Sidebar
          config={config}
          isCollapsed={isCollapsed}
          isMobileOpen={isMobileOpen}
          onMobileOpenChange={setIsMobileOpen}
          onToggleCollapse={handleToggleCollapse}
          query={query}
        />
        <div className="flex min-h-screen flex-1 flex-col gap-3">
          <div className="px-3 pt-3 pl-20 lg:pl-3">
            <Header
              config={config}
              isScrolled={isScrolled}
              onQueryChange={setQuery}
              query={query}
            />
          </div>
          <Content onScrollChange={setIsScrolled} />
        </div>
      </div>
    </div>
  );
}
