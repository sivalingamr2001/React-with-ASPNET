'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ChevronLeft,
  ChevronRight,
  Home,
  BarChart3,
  Settings,
  Users,
  Zap,
  FileText,
  ChevronDown,
  Layers,
} from 'lucide-react';

interface NavItem {
  icon: any;
  label: string;
  href?: string;
  shortcut?: string;
  children?: NavItem[];
}

const navItems: NavItem[] = [
  { icon: Home, label: 'Dashboard', href: '#', shortcut: 'Cmd+1' },
  { icon: BarChart3, label: 'Analytics', href: '#', shortcut: 'Cmd+2' },
  {
    icon: Layers,
    label: 'Projects',
    href: '#',
    shortcut: 'Cmd+3',
    children: [
      { icon: FileText, label: 'Active Projects', href: '#' },
      { icon: FileText, label: 'Archived', href: '#' },
      { icon: FileText, label: 'Templates', href: '#' },
    ],
  },
  { icon: Users, label: 'Team', href: '#', shortcut: 'Cmd+4' },
  { icon: Zap, label: 'Integrations', href: '#', shortcut: 'Cmd+5' },
  { icon: Settings, label: 'Settings', href: '#', shortcut: 'Cmd+,' },
];

export function Sidebar() {
  const [collapsed, setCollapsed] = useState(false);
  const [expandedItems, setExpandedItems] = useState<string[]>([]);
  const [activeItem, setActiveItem] = useState('Dashboard');

  const toggleExpand = (label: string) => {
    setExpandedItems((prev) =>
      prev.includes(label) ? prev.filter((item) => item !== label) : [...prev, label]
    );
  };

  const sidebarVariants = {
    expanded: { width: 240 },
    collapsed: { width: 80 },
  };

  const labelVariants = {
    expanded: { opacity: 1, x: 0 },
    collapsed: { opacity: 0, x: -10 },
  };

  return (
    <motion.aside
      initial={false}
      animate={collapsed ? 'collapsed' : 'expanded'}
      variants={sidebarVariants}
      transition={{ duration: 0.3, ease: 'easeInOut' }}
      className="fixed left-0 top-16 bottom-0 border-r border-zinc-800 bg-zinc-950/60 backdrop-blur-md flex flex-col pt-6 pb-6 z-40"
    >
      {/* Workspace Switcher */}
      <motion.div className="px-4 mb-8">
        <motion.div
          className="p-3 rounded-lg bg-zinc-900/50 border border-zinc-800 hover:border-zinc-700 cursor-pointer transition-colors hover:bg-zinc-800/50"
          whileHover={{ scale: 1.02 }}
          whileTap={{ scale: 0.98 }}
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 flex-1">
              <div className="w-8 h-8 rounded-md bg-gradient-to-br from-purple-500 to-pink-500" />
              <motion.div
                variants={labelVariants}
                transition={{ duration: 0.3 }}
                className="flex-1"
              >
                <div className="text-sm font-semibold text-foreground leading-none">
                  Design Co.
                </div>
                <div className="text-xs text-zinc-600 mt-1">Premium</div>
              </motion.div>
            </div>
            <motion.div
              variants={labelVariants}
              transition={{ duration: 0.3 }}
            >
              <ChevronDown className="w-4 h-4 text-zinc-600" />
            </motion.div>
          </div>
        </motion.div>
      </motion.div>

      {/* Navigation Items */}
      <nav className="flex-1 px-2 overflow-y-auto space-y-1">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = activeItem === item.label;
          const isExpanded = expandedItems.includes(item.label);
          const hasChildren = item.children && item.children.length > 0;

          return (
            <div key={item.label}>
              <motion.button
                onClick={() => {
                  setActiveItem(item.label);
                  if (hasChildren) {
                    toggleExpand(item.label);
                  }
                }}
                className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all relative group ${
                  isActive
                    ? 'nav-item-active glow-accent'
                    : 'nav-item hover:bg-zinc-800/30'
                }`}
                whileHover={{ x: 2 }}
                whileTap={{ scale: 0.98 }}
              >
                <Icon className="w-5 h-5 flex-shrink-0" />
                <motion.span
                  variants={labelVariants}
                  transition={{ duration: 0.3 }}
                  className="text-sm font-medium flex-1"
                >
                  {item.label}
                </motion.span>

                {/* Keyboard shortcut hint */}
                <motion.kbd
                  variants={labelVariants}
                  transition={{ duration: 0.3 }}
                  className="hidden text-xs text-zinc-600 opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  {item.shortcut?.split('+').slice(-1)[0]}
                </motion.kbd>

                {hasChildren && (
                  <motion.div
                    animate={{ rotate: isExpanded ? 180 : 0 }}
                    transition={{ duration: 0.2 }}
                  >
                    <ChevronRight className="w-4 h-4 text-zinc-600" />
                  </motion.div>
                )}

                {/* Active indicator glow */}
                {isActive && (
                  <motion.div
                    layoutId="activeIndicator"
                    className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-8 bg-blue-500 rounded-r-lg"
                    transition={{ type: 'spring', damping: 20, stiffness: 300 }}
                  />
                )}
              </motion.button>

              {/* Nested Items */}
              <AnimatePresence initial={false}>
                {hasChildren && isExpanded && (
                  <motion.div
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: 'auto' }}
                    exit={{ opacity: 0, height: 0 }}
                    transition={{ duration: 0.2 }}
                    className="overflow-hidden"
                  >
                    {item.children?.map((child) => {
                      const ChildIcon = child.icon;
                      return (
                        <motion.button
                          key={child.label}
                          initial={{ opacity: 0, x: -10 }}
                          animate={{ opacity: 1, x: 0 }}
                          exit={{ opacity: 0, x: -10 }}
                          transition={{ duration: 0.15 }}
                          className="w-full flex items-center gap-3 px-3 py-2 ml-3 rounded-lg text-sm text-zinc-400 hover:text-foreground hover:bg-zinc-800/30 transition-colors"
                          whileHover={{ x: 2 }}
                          whileTap={{ scale: 0.98 }}
                        >
                          <ChildIcon className="w-4 h-4 flex-shrink-0" />
                          <motion.span
                            variants={labelVariants}
                            transition={{ duration: 0.3 }}
                          >
                            {child.label}
                          </motion.span>
                        </motion.button>
                      );
                    })}
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          );
        })}
      </nav>

      {/* Collapse Toggle */}
      <motion.div className="px-2 border-t border-zinc-800 pt-4">
        <motion.button
          onClick={() => setCollapsed(!collapsed)}
          className="w-full flex items-center justify-center p-2 rounded-lg hover:bg-zinc-800/50 transition-colors text-zinc-600 hover:text-foreground"
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          {collapsed ? (
            <ChevronRight className="w-5 h-5" />
          ) : (
            <ChevronLeft className="w-5 h-5" />
          )}
        </motion.button>
      </motion.div>
    </motion.aside>
  );
}
