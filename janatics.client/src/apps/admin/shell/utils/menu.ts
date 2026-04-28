import type { AdminMenuItem, AdminPageConfig } from "../types";

export function isBranchActive(item: AdminMenuItem, path: string): boolean {
  if (item.route === path) return true;
  return item.children?.some((child) => isBranchActive(child, path)) ?? false;
}

export function getPageMeta(pages: AdminPageConfig[], path: string) {
  return pages.find((page) => page.route === path) ?? pages[0];
}

export function filterMenu(
  items: AdminMenuItem[],
  query: string,
): AdminMenuItem[] {
  if (!query.trim()) return items;
  const value = query.toLowerCase();
  return items.flatMap((item) => {
    const children: AdminMenuItem[] = item.children
      ? filterMenu(item.children, query)
      : [];
    const match = item.label.toLowerCase().includes(value);
    return match || children.length ? [{ ...item, children }] : [];
  });
}
