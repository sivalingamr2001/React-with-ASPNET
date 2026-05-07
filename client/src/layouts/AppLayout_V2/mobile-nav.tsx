'use client';

import { motion } from 'framer-motion';
import {
  Home,
  BarChart3,
  Layers,
  Users,
  Settings,
} from 'lucide-react';
import { useState } from 'react';

const navItems = [
  { icon: Home, label: 'Home', id: 'home' },
  { icon: BarChart3, label: 'Analytics', id: 'analytics' },
  { icon: Layers, label: 'Projects', id: 'projects' },
  { icon: Users, label: 'Team', id: 'team' },
  { icon: Settings, label: 'Settings', id: 'settings' },
];

export function MobileNav() {
  const [activeTab, setActiveTab] = useState('home');

  return (
    <motion.nav
      initial={{ y: 100 }}
      animate={{ y: 0 }}
      transition={{ duration: 0.3 }}
      className="fixed bottom-0 left-0 right-0 h-20 bg-zinc-950/90 backdrop-blur-lg border-t border-zinc-800 flex items-center justify-around md:hidden"
      aria-label="Mobile navigation"
    >
      {navItems.map((item) => {
        const Icon = item.icon;
        const isActive = activeTab === item.id;

        return (
          <motion.button
            key={item.id}
            onClick={() => setActiveTab(item.id)}
            className="flex flex-col items-center justify-center gap-1 py-2 px-3 relative flex-1"
            whileTap={{ scale: 0.9 }}
          >
            <motion.div
              animate={{
                scale: isActive ? 1.1 : 1,
                color: isActive ? '#0ea5e9' : '#a1a1aa',
              }}
              transition={{ type: 'spring', damping: 15, stiffness: 200 }}
            >
              <Icon className="w-6 h-6" />
            </motion.div>
            <motion.span
              animate={{ scale: isActive ? 1 : 0.8, opacity: isActive ? 1 : 0.7 }}
              transition={{ duration: 0.2 }}
              className="text-xs font-medium"
            >
              {item.label}
            </motion.span>

            {isActive && (
              <motion.div
                layoutId="mobileActiveIndicator"
                className="absolute bottom-0 left-0 right-0 h-1 bg-gradient-to-r from-blue-500 to-cyan-500"
                transition={{ type: 'spring', damping: 20, stiffness: 300 }}
              />
            )}
          </motion.button>
        );
      })}
    </motion.nav>
  );
}
