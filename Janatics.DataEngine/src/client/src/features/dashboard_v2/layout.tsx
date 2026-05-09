'use client';

import { Outlet } from 'react-router-dom';
import { DashboardNav } from '@/components/dashboard/DashboardNav';

export default function DashboardV2Layout() {
  return (
    <div className="flex min-h-screen bg-gray-50">
      <DashboardNav />
      <main className="flex-1 overflow-auto">
        <Outlet />
      </main>
    </div>
  );
}
