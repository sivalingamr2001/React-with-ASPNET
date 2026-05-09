'use client';

import { useState, useEffect } from 'react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import { RoleGuard } from '@/components/auth/RoleGuard';
import { DatabaseConfigForm } from '@/components/db-config/DatabaseConfigForm';
import { DatabaseConfigList } from '@/components/db-config/DatabaseConfigList';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Plus } from 'lucide-react';

export default function DatabaseConfigPage() {
  const [showForm, setShowForm] = useState(false);
  const { dbConfigs, loadDatabaseConfigs } = useDataEngine();

  useEffect(() => {
    loadDatabaseConfigs();
  }, [loadDatabaseConfigs]);

  return (
    <RoleGuard requiredPermissions={['manage_db_config']} fallback={
      <div className="flex h-full items-center justify-center">
        <p className="text-gray-500">You don't have permission to access this page</p>
      </div>
    }>
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="border-b border-gray-200 bg-white px-8 py-6 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Database Configuration</h1>
            <p className="text-gray-600 mt-1">Manage database connections and providers</p>
          </div>
          <Button onClick={() => setShowForm(true)} className="gap-2">
            <Plus className="w-4 h-4" />
            Add Configuration
          </Button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto p-8">
          {showForm ? (
            <Card className="max-w-2xl">
              <DatabaseConfigForm onClose={() => setShowForm(false)} />
            </Card>
          ) : (
            <>
              {dbConfigs.length === 0 ? (
                <div className="text-center py-12">
                  <p className="text-gray-500 mb-4">No database configurations yet</p>
                  <Button onClick={() => setShowForm(true)} variant="outline">
                    Create First Configuration
                  </Button>
                </div>
              ) : (
                <DatabaseConfigList configs={dbConfigs} onRefresh={loadDatabaseConfigs} />
              )}
            </>
          )}
        </div>
      </div>
    </RoleGuard>
  );
}
