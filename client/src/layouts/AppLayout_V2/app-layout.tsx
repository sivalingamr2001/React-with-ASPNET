'use client';

import { motion } from 'framer-motion';
import { Header } from './header';
import { Sidebar } from './sidebar';
import { MobileNav } from './mobile-nav';

interface AppLayoutProps {
  children: React.ReactNode;
}

export function AppLayout({ children }: AppLayoutProps) {
  return (
    <div className="relative min-h-screen bg-background text-foreground">
      {/* Header */}
      <Header />

      {/* Main layout - Sidebar on desktop, hidden on mobile */}
      <div className="flex">
        {/* Sidebar - hidden on mobile */}
        <div className="hidden md:block">
          <Sidebar />
        </div>

        {/* Main content area */}
        <main className="flex-1 pt-16 pb-20 md:pb-6 md:ml-20 lg:ml-0">
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
            className="p-6 lg:p-8 h-full"
          >
            {children}
          </motion.div>
        </main>
      </div>

      {/* Mobile bottom navigation - visible only on small screens */}
      <MobileNav />
    </div>
  );
}
