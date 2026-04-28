import { motion } from "framer-motion";
import { Link } from "react-router-dom";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

import PageFrame from "../../shell/components/PageFrame";
import type { AdminPageConfig } from "../../shell/types";
import { getIcon } from "../../shell/utils/iconMap";

interface Props {
  page: AdminPageConfig;
}

export default function DashboardPage({ page }: Props) {
  return (
    <PageFrame description={page.description} title={page.title}>
      <div className="grid gap-4 lg:grid-cols-3">
        {page.stats?.map((item) => (
          <StatCard item={item} key={item.label} />
        ))}
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        {page.highlights?.map((item) => (
          <HighlightCard item={item} key={item.title} />
        ))}
      </div>
    </PageFrame>
  );
}

function StatCard({
  item,
}: {
  item: NonNullable<AdminPageConfig["stats"]>[number];
}) {
  const Icon = getIcon(item.icon);
  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      whileHover={{ y: -4 }}
      whileTap={{ scale: 0.98 }}
    >
      <Card className="rounded-[28px] border-white/60 bg-card/85">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardDescription>{item.label}</CardDescription>
            <Icon className="size-5 text-primary" />
          </div>
          <CardTitle className="text-3xl">{item.value}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">{item.detail}</p>
        </CardContent>
      </Card>
    </motion.div>
  );
}

function HighlightCard({
  item,
}: {
  item: NonNullable<AdminPageConfig["highlights"]>[number];
}) {
  const Icon = getIcon(item.icon);
  return (
    <motion.div initial={{ opacity: 0, y: 30 }} whileHover={{ y: -4 }}>
      <Card className="rounded-[30px] border-white/60 bg-gradient-to-br from-card to-primary/5">
        <CardHeader>
          <CardTitle>{item.title}</CardTitle>
          <CardDescription>{item.description}</CardDescription>
        </CardHeader>
        <CardContent>
          <Link
            className="inline-flex items-center gap-2 text-sm font-medium text-primary"
            to={item.route}
          >
            {item.label}
            <Icon className="size-4" />
          </Link>
        </CardContent>
      </Card>
    </motion.div>
  );
}
