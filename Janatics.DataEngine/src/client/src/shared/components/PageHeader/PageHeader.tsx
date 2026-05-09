import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: ReactNode;
  className?: string;
}

export const PageHeader = ({
  title,
  description,
  actions,
  className,
}: PageHeaderProps) => (
  <div className={cn("flex items-start justify-between gap-4 pb-6", className)}>
    <div className="space-y-1">
      <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
      {description && (
        <p className="text-sm text-muted-foreground">{description}</p>
      )}
    </div>
    {actions && <div className="flex items-center gap-2">{actions}</div>}
  </div>
);