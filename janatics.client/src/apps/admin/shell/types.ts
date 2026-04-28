export interface AdminAction {
  label: string;
  icon: string;
  route: string;
}

export interface AdminMenuItem extends AdminAction {
  children?: AdminMenuItem[];
}

export interface AdminPageStat {
  label: string;
  value: string;
  detail: string;
  icon: string;
}

export interface AdminPageHighlight extends AdminAction {
  description: string;
  title: string;
}

export interface AdminPageConfig {
  id: string;
  route: string;
  title: string;
  description: string;
  icon: string;
  breadcrumb: string[];
  stats?: AdminPageStat[];
  highlights?: AdminPageHighlight[];
}

export interface LayoutConfig {
  app: { title: string; logo: string; homeRoute: string };
  theme: { primaryColor: string; darkMode: boolean; shellGradient: string };
  header: {
    searchPlaceholder: string;
    notifications: Array<{
      id: string;
      label: string;
      detail: string;
      icon: string;
    }>;
    profile: { name: string; role: string; avatar: string };
    actions: AdminAction[];
  };
  sidebar: { collapsed: boolean; menu: AdminMenuItem[] };
  pages: AdminPageConfig[];
}
