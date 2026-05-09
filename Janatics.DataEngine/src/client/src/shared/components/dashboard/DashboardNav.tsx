'use client';

import { useAuth } from '@/lib/hooks/useAuth';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Database,
  Search,
  MapPin,
  FileText,
  Table2,
  LogOut,
} from 'lucide-react';
import { Button } from '@/components/ui/button';

const NAV_ITEMS = [
  {
    label: 'Dashboard',
    href: '/dashboard',
    icon: LayoutDashboard,
    permission: 'view_dashboard',
  },
  {
    label: 'DB Config',
    href: '/dashboard/db-config',
    icon: Database,
    permission: 'manage_db_config',
  },
  {
    label: 'Query Builder',
    href: '/dashboard/query-builder',
    icon: Search,
    permission: 'use_query_builder',
  },
  {
    label: 'Field Mapper',
    href: '/dashboard/field-mapper',
    icon: MapPin,
    permission: 'use_field_mapper',
  },
  {
    label: 'Logs',
    href: '/dashboard/logs',
    icon: FileText,
    permission: 'view_logs',
  },
  {
    label: 'Table Manager',
    href: '/dashboard/table-manager',
    icon: Table2,
    permission: 'manage_tables',
  },
];

export function DashboardNav() {
  const { user, logout, hasPermission } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <aside className="w-64 border-r border-gray-200 bg-white flex flex-col">
      {/* Header */}
      <div className="p-6 border-b border-gray-200">
        <h1 className="text-xl font-bold text-gray-900">Data Engine</h1>
        <p className="text-sm text-gray-500 mt-1">{user?.name}</p>
        <p className="text-xs text-gray-400 capitalize">{user?.role}</p>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto p-4 space-y-2">
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          const hasAccess = hasPermission(item.permission as any);

          if (!hasAccess) return null;

          return (
            <NavLink
              key={item.href}
              to={item.href}
              className={({ isActive }) =>
                `flex items-center gap-3 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-blue-50 text-blue-600'
                    : 'text-gray-600 hover:bg-gray-50'
                }`
              }
            >
              <Icon className="w-4 h-4" />
              {item.label}
            </NavLink>
          );
        })}
      </nav>

      {/* Footer */}
      <div className="border-t border-gray-200 p-4 space-y-2">
        <Button
          variant="outline"
          className="w-full justify-start"
          onClick={handleLogout}
        >
          <LogOut className="w-4 h-4 mr-2" />
          Logout
        </Button>
      </div>
    </aside>
  );
}
