'use client';

import { useState, useEffect } from 'react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import { RoleGuard } from '@/components/auth/RoleGuard';
import { TableForm } from '@/components/table-manager/TableForm';
import { TableList } from '@/components/table-manager/TableList';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Plus } from 'lucide-react';

export default function TableManagerPage() {
  const [showForm, setShowForm] = useState(false);
  const { tables, loadTables, selectedConfigId } = useDataEngine();

  useEffect(() => {
    if (selectedConfigId) {
      loadTables(selectedConfigId);
    }
  }, [selectedConfigId, loadTables]);

  return (
    <RoleGuard requiredPermissions={['manage_tables']} fallback={
      <div className="flex h-full items-center justify-center">
        <p className="text-gray-500">You don't have permission to access this page</p>
      </div>
    }>
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="border-b border-gray-200 bg-white px-8 py-6 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Table Manager</h1>
            <p className="text-gray-600 mt-1">Create and manage database tables</p>
          </div>
          <Button 
            onClick={() => setShowForm(true)} 
            className="gap-2"
            disabled={!selectedConfigId}
          >
            <Plus className="w-4 h-4" />
            New Table
          </Button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto p-8">
          {!selectedConfigId ? (
            <div className="text-center py-12">
              <p className="text-gray-500 mb-4">Please configure a database first</p>
            </div>
          ) : showForm ? (
            <Card className="max-w-4xl">
              <TableForm onClose={() => setShowForm(false)} />
            </Card>
          ) : (
            <>
              {tables.length === 0 ? (
                <div className="text-center py-12">
                  <p className="text-gray-500 mb-4">No tables yet</p>
                  <Button onClick={() => setShowForm(true)} variant="outline">
                    Create First Table
                  </Button>
                </div>
              ) : (
                <TableList tables={tables} />
              )}
            </>
          )}
        </div>
      </div>
    </RoleGuard>
  );
}
