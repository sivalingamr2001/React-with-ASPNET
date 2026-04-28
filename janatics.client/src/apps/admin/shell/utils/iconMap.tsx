import type { LucideIcon } from "lucide-react";
import {
  ArrowRight,
  Bell,
  Database,
  Filter,
  LayoutDashboard,
  Plus,
  Sparkles,
  Workflow,
} from "lucide-react";

const icons: Record<string, LucideIcon> = {
  ArrowRight,
  Bell,
  Database,
  Filter,
  LayoutDashboard,
  Plus,
  Sparkles,
  Workflow,
};

export function getIcon(name: string) {
  return icons[name] ?? Sparkles;
}
