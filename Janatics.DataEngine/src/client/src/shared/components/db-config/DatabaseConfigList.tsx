'use client';

import type { DatabaseConfig } from '@/types/dataEngine';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Trash2, Edit, Database } from 'lucide-react';
import { useState } from 'react';
import { DatabaseConfigForm } from './DatabaseConfigForm';

interface DatabaseConfigListProps {
  configs: DatabaseConfig[];
  onRefresh: () => void;
}

export function DatabaseConfigList({ configs, onRefresh }: DatabaseConfigListProps) {
  const { removeDatabaseConfig } = useDataEngine();
  const [editingConfig, setEditingConfig] = useState<DatabaseConfig | null>(null);

  const handleDelete = async (id: string | undefined) => {
    if (!id) return;
    if (confirm('Are you sure you want to delete this configuration?')) {
      await removeDatabaseConfig(id);
      onRefresh();
    }
  };

  const handleEditClose = () => {
    setEditingConfig(null);
    onRefresh();
  };

  if (editingConfig) {
    return (
      <Card className="max-w-2xl">
        <DatabaseConfigForm config={editingConfig} onClose={handleEditClose} />
      </Card>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      {configs.map((config) => (
        <Card key={config.id}>
          <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <Database className="w-5 h-5 text-blue-600" />
              </div>
              <div>
                <h3 className="font-semibold text-gray-900">{config.name}</h3>
                <p className="text-sm text-gray-500 capitalize">{config.provider}</p>
              </div>
            </div>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setEditingConfig(config)}
              >
                <Edit className="w-4 h-4" />
              </Button>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => handleDelete(config.id)}
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
              >
                <Trash2 className="w-4 h-4" />
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-2 text-sm text-gray-600">
            {config.host && (
              <div className="flex justify-between">
                <span>Host:</span>
                <span className="font-mono text-gray-900">{config.host}:{config.port}</span>
              </div>
            )}
            {config.database && (
              <div className="flex justify-between">
                <span>Database:</span>
                <span className="font-mono text-gray-900">{config.database}</span>
              </div>
            )}
            {config.username && (
              <div className="flex justify-between">
                <span>User:</span>
                <span className="font-mono text-gray-900">{config.username}</span>
              </div>
            )}
            {config.ssl && (
              <div className="flex justify-between">
                <span>SSL:</span>
                <span className="font-mono text-green-600">Enabled</span>
              </div>
            )}
            <div className="pt-3 border-t border-gray-200">
              <p className="text-xs text-gray-500">
                Created: {config.createdAt ? new Date(config.createdAt).toLocaleDateString() : 'N/A'}
              </p>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
