'use client';

import { AnimatePresence, motion } from 'framer-motion';
import {
  Bell,
  ChevronDown,
  FileText,
  LogOut,
  Search,
  Settings,
  Users,
  Zap
} from 'lucide-react';
import { useEffect, useState } from 'react';

export function Header() {
  const [searchOpen, setSearchOpen] = useState(false);
  const [quickActionsOpen, setQuickActionsOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);

  // Command palette keyboard shortcut
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setSearchOpen(!searchOpen);
      }
      // Close dropdowns with Escape
      if (e.key === 'Escape') {
        setQuickActionsOpen(false);
        setNotificationsOpen(false);
        setProfileOpen(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [searchOpen]);

  const quickActions = [
    {
      icon: Zap,
      label: 'Create Project',
      description: 'Start a new project',
      shortcut: 'Cmd+Shift+P',
    },
    {
      icon: Users,
      label: 'Invite Team',
      description: 'Add team members',
      shortcut: 'Cmd+Shift+I',
    },
    {
      icon: FileText,
      label: 'Documentation',
      description: 'View docs',
      shortcut: 'Cmd+/',
    },
  ];

  const notifications = [
    { id: 1, title: 'Project published', time: '2 minutes ago', unread: true },
    { id: 2, title: 'Team member joined', time: '1 hour ago', unread: true },
    { id: 3, title: 'New feature available', time: '3 hours ago', unread: false },
  ];

  return (
    <header
      className="glassmorphism fixed top-0 left-0 right-0 z-50 h-16 flex items-center justify-between px-6 border-b"
      role="banner"
      aria-label="Main navigation header"
    >
      {/* Left section */}
      <div className="flex items-center gap-8 flex-1">
        <div className="text-xl font-bold bg-gradient-to-r from-blue-400 to-blue-600 bg-clip-text text-transparent">
          Workspace
        </div>

        {/* Command Palette Search */}
        <div className="hidden md:flex items-center flex-1 max-w-xs">
          <motion.div
            className="w-full relative"
            animate={{ width: searchOpen ? 'auto' : '100%' }}
          >
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-zinc-500" />
              <input
                type="text"
                placeholder="Search... (Cmd+K)"
                className="w-full bg-zinc-900/50 border border-zinc-800 rounded-lg pl-10 pr-4 py-2 text-sm text-zinc-100 placeholder-zinc-600 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 transition-all"
                onFocus={() => setSearchOpen(true)}
                onBlur={() => setSearchOpen(false)}
                aria-label="Search workspace"
                aria-describedby="search-hint"
              />
              <span id="search-hint" className="sr-only">
                Press Cmd+K or Ctrl+K to open search
              </span>
            </div>
          </motion.div>
        </div>
      </div>

      {/* Right section */}
      <div className="flex items-center gap-2">
        {/* Quick Actions Dropdown */}
        <div className="relative hidden sm:block">
          <motion.button
            onClick={() => setQuickActionsOpen(!quickActionsOpen)}
            className="relative p-2 rounded-lg hover:bg-zinc-800/50 transition-colors group"
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            aria-label="Quick actions"
            aria-expanded={quickActionsOpen}
            aria-haspopup="menu"
          >
            <Zap className="w-5 h-5 text-zinc-400 group-hover:text-foreground" />
          </motion.button>

          <AnimatePresence>
            {quickActionsOpen && (
              <motion.div
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                transition={{ duration: 0.15 }}
                className="absolute right-0 mt-2 w-64 glassmorphism rounded-xl border p-2 shadow-xl"
                onClick={() => setQuickActionsOpen(false)}
                role="menu"
              >
                {quickActions.map((action, idx) => {
                  const Icon = action.icon;
                  return (
                    <motion.button
                      key={idx}
                      initial={{ opacity: 0, x: -10 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ delay: idx * 0.05 }}
                      className="w-full text-left px-3 py-2 rounded-lg hover:bg-zinc-800/40 transition-colors group"
                    >
                      <div className="flex items-center gap-3">
                        <Icon className="w-4 h-4 text-blue-400" />
                        <div className="flex-1">
                          <div className="text-sm font-medium text-foreground">
                            {action.label}
                          </div>
                          <div className="text-xs text-zinc-500">
                            {action.description}
                          </div>
                        </div>
                        <kbd className="text-xs text-zinc-600 opacity-0 group-hover:opacity-100 transition-opacity">
                          {action.shortcut.split('+').slice(-1)[0]}
                        </kbd>
                      </div>
                    </motion.button>
                  );
                })}
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* Notifications */}
        <div className="relative">
          <motion.button
            onClick={() => setNotificationsOpen(!notificationsOpen)}
            className="relative p-2 rounded-lg hover:bg-zinc-800/50 transition-colors"
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            aria-label="Notifications, 3 unread"
            aria-expanded={notificationsOpen}
            aria-haspopup="menu"
          >
            <Bell className="w-5 h-5 text-zinc-400 hover:text-foreground" />
            <motion.span
              animate={{ scale: [1, 1.2, 1] }}
              transition={{ repeat: Infinity, duration: 2 }}
              className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"
              aria-label="3 unread notifications"
            />
          </motion.button>

          <AnimatePresence>
            {notificationsOpen && (
              <motion.div
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                transition={{ duration: 0.15 }}
                className="absolute right-0 mt-2 w-80 glassmorphism rounded-xl border p-3 shadow-xl max-h-96 overflow-y-auto"
              >
                <div className="text-sm font-semibold text-foreground mb-2">
                  Notifications
                </div>
                {notifications.map((notif) => (
                  <motion.div
                    key={notif.id}
                    initial={{ opacity: 0, x: -10 }}
                    animate={{ opacity: 1, x: 0 }}
                    className={`p-2 rounded-lg mb-2 cursor-pointer hover:bg-zinc-800/40 transition-colors ${
                      notif.unread ? 'bg-zinc-800/30 border-l-2 border-blue-500' : ''
                    }`}
                  >
                    <div className="text-sm text-foreground">{notif.title}</div>
                    <div className="text-xs text-zinc-600 mt-1">{notif.time}</div>
                  </motion.div>
                ))}
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* Premium User Profile */}
        <div className="relative ml-2 pl-2 border-l border-zinc-800">
          <motion.button
            onClick={() => setProfileOpen(!profileOpen)}
            className="flex items-center gap-2 px-2 py-1 rounded-lg hover:bg-zinc-800/50 transition-colors group"
            whileHover={{ scale: 1.02 }}
            whileTap={{ scale: 0.98 }}
          >
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-sm font-bold">
              AJ
            </div>
            <div className="hidden sm:flex flex-col items-start">
              <div className="text-xs font-semibold text-foreground leading-none">
                Alex Johnson
              </div>
              <div className="text-xs text-zinc-600">Pro</div>
            </div>
            <ChevronDown className="w-4 h-4 text-zinc-600 group-hover:text-foreground transition-colors" />
          </motion.button>

          <AnimatePresence>
            {profileOpen && (
              <motion.div
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                transition={{ duration: 0.15 }}
                className="absolute right-0 mt-2 w-56 glassmorphism rounded-xl border p-2 shadow-xl"
                onClick={() => setProfileOpen(false)}
              >
                <div className="px-3 py-2 border-b border-zinc-800 mb-2">
                  <div className="text-sm font-semibold text-foreground">
                    Alex Johnson
                  </div>
                  <div className="text-xs text-zinc-500">alex@example.com</div>
                  <div className="mt-2 inline-block px-2 py-1 bg-blue-500/20 border border-blue-500/50 rounded text-xs text-blue-400 font-medium">
                    Pro Plan
                  </div>
                </div>

                <motion.button className="w-full text-left px-3 py-2 rounded-lg hover:bg-zinc-800/40 transition-colors flex items-center gap-2 text-sm text-foreground">
                  <Settings className="w-4 h-4" />
                  Settings
                </motion.button>
                <motion.button className="w-full text-left px-3 py-2 rounded-lg hover:bg-zinc-800/40 transition-colors flex items-center gap-2 text-sm text-red-400">
                  <LogOut className="w-4 h-4" />
                  Sign out
                </motion.button>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </header>
  );
}
