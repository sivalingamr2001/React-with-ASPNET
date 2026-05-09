'use client';

import { useEffect, useState } from 'react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Activity, Database, AlertCircle, Clock } from 'lucide-react';

export default function DashboardPage() {
  const { loadStats, stats, dbConfigs, tables, queries, logs } = useDataEngine();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (mounted) {
      const interval = setInterval(() => {
        loadStats();
      }, 5000);

      return () => clearInterval(interval);
    }
  }, [loadStats, mounted]);

  const StatCard = ({ 
    icon: Icon, 
    label, 
    value, 
    unit = '',
    color = 'blue' 
  }: { 
    icon: any; 
    label: string; 
    value: string | number; 
    unit?: string;
    color?: string;
  }) => {
    const colorClasses = {
      blue: 'bg-blue-50 text-blue-600',
      green: 'bg-green-50 text-green-600',
      amber: 'bg-amber-50 text-amber-600',
      red: 'bg-red-50 text-red-600',
    };

    return (
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-start justify-between">
            <div>
              <p className="text-sm text-gray-600">{label}</p>
              <p className="text-3xl font-bold mt-2 text-gray-900">
                {value}
                <span className="text-lg ml-1">{unit}</span>
              </p>
            </div>
            <div className={`p-3 rounded-lg ${colorClasses[color as keyof typeof colorClasses]}`}>
              <Icon className="w-6 h-6" />
            </div>
          </div>
        </CardContent>
      </Card>
    );
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="border-b border-gray-200 bg-white px-8 py-6">
        <h1 className="text-3xl font-bold text-gray-900">Monitoring</h1>
        <p className="text-gray-600 mt-1">Real-time data engine statistics and health</p>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-8">
        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <StatCard
            icon={Activity}
            label="Active Connections"
            value={stats?.activeConnections || 0}
            color="green"
          />
          <StatCard
            icon={Database}
            label="Total Queries"
            value={stats?.totalQueries || 0}
            color="blue"
          />
          <StatCard
            icon={AlertCircle}
            label="Total Errors"
            value={stats?.totalErrors || 0}
            color="red"
          />
          <StatCard
            icon={Clock}
            label="Avg Query Time"
            value={stats?.averageQueryTime || 0}
            unit="ms"
            color="amber"
          />
        </div>

        {/* System Overview */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Database Configs */}
          <Card>
            <CardHeader>
              <CardTitle>Database Configurations</CardTitle>
              <CardDescription>Connected database providers</CardDescription>
            </CardHeader>
            <CardContent>
              {dbConfigs.length === 0 ? (
                <p className="text-gray-500 text-sm py-4">No database configurations</p>
              ) : (
                <div className="space-y-3">
                  {dbConfigs.map((config) => (
                    <div key={config.id} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                      <div>
                        <p className="font-medium text-gray-900">{config.name}</p>
                        <p className="text-xs text-gray-500 capitalize">{config.provider}</p>
                      </div>
                      <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Quick Stats */}
          <Card>
            <CardHeader>
              <CardTitle>System Overview</CardTitle>
              <CardDescription>Current resource usage</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <div className="flex justify-between mb-2">
                  <span className="text-sm font-medium text-gray-700">Database Tables</span>
                  <span className="text-sm text-gray-600">{tables.length}</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-blue-500 h-2 rounded-full" 
                    style={{ width: `${Math.min(tables.length * 10, 100)}%` }}
                  ></div>
                </div>
              </div>
              <div>
                <div className="flex justify-between mb-2">
                  <span className="text-sm font-medium text-gray-700">Saved Queries</span>
                  <span className="text-sm text-gray-600">{queries.length}</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-green-500 h-2 rounded-full" 
                    style={{ width: `${Math.min(queries.length * 10, 100)}%` }}
                  ></div>
                </div>
              </div>
              <div>
                <div className="flex justify-between mb-2">
                  <span className="text-sm font-medium text-gray-700">System Logs</span>
                  <span className="text-sm text-gray-600">{logs.length}</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-amber-500 h-2 rounded-full" 
                    style={{ width: `${Math.min(logs.length * 2, 100)}%` }}
                  ></div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Recent Activity */}
        <Card className="mt-6">
          <CardHeader>
            <CardTitle>Recent Activity</CardTitle>
            <CardDescription>Latest system events</CardDescription>
          </CardHeader>
          <CardContent>
            {logs.length === 0 ? (
              <p className="text-gray-500 text-sm py-4">No recent activity</p>
            ) : (
              <div className="space-y-3 max-h-64 overflow-y-auto">
                {logs.slice(0, 10).map((log) => (
                  <div key={log.id} className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg text-sm">
                    <div className={`w-2 h-2 rounded-full mt-1 flex-shrink-0 ${
                      log.level === 'error' ? 'bg-red-500' :
                      log.level === 'warning' ? 'bg-amber-500' :
                      log.level === 'debug' ? 'bg-gray-500' :
                      'bg-green-500'
                    }`}></div>
                    <div className="flex-1">
                      <p className="text-gray-900 font-medium">{log.action}</p>
                      <p className="text-gray-500">{log.message}</p>
                      <p className="text-xs text-gray-400 mt-1">{log.timestamp}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
