import { motion } from "framer-motion";
import { Search } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

import type { LayoutConfig } from "../types";
import { getIcon } from "../utils/iconMap";
import { getPageMeta } from "../utils/menu";
import BreadcrumbTrail from "./BreadcrumbTrail";
import HeaderMenu from "./HeaderMenu";

interface Props {
  config: LayoutConfig;
  isScrolled: boolean;
  query: string;
  onQueryChange: (value: string) => void;
}

export default function Header({
  config,
  isScrolled,
  query,
  onQueryChange,
}: Props) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const meta = getPageMeta(config.pages, pathname);
  const ActionIcon = getIcon(config.header.actions[0].icon);
  const handleAction = () => navigate(config.header.actions[0].route);
  const handleQueryChange = (event: React.ChangeEvent<HTMLInputElement>) =>
    onQueryChange(event.target.value);

  return (
    <motion.header
      animate={{ y: 0, opacity: 1 }}
      className={`sticky top-0 z-40 rounded-[28px] border bg-card/75 p-4 backdrop-blur-xl ${isScrolled ? "shadow-lg shadow-primary/10" : ""}`}
      initial={{ y: -20, opacity: 0 }}
    >
      <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div className="space-y-3">
          <BreadcrumbTrail items={meta.breadcrumb} />
          <div>
            <h2 className="text-xl font-semibold">{meta.title}</h2>
            <p className="text-sm text-muted-foreground">{meta.description}</p>
          </div>
        </div>
        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <div className="relative min-w-[260px]">
            <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="h-11 rounded-full border-white/60 bg-background/70 pl-10"
              onChange={handleQueryChange}
              placeholder={config.header.searchPlaceholder}
              value={query}
            />
          </div>
          <Button className="rounded-full px-4" onClick={handleAction}>
            <ActionIcon className="size-4" />
            <span>{config.header.actions[0].label}</span>
          </Button>
          <HeaderMenu config={config} />
        </div>
      </div>
    </motion.header>
  );
}
