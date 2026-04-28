import { RegistryWorkspace } from "../features/registry";
import PageFrame from "../shell/components/PageFrame";
import type { AdminPageConfig } from "../shell/types";

interface Props {
  page: AdminPageConfig;
}

export default function RegistryManager({ page }: Props) {
  return (
    <PageFrame description={page.description} title={page.title}>
      <RegistryWorkspace />
    </PageFrame>
  );
}
