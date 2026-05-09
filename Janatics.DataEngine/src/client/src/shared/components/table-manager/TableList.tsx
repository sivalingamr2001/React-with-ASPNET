'use client';

import type { Table } from '@/types/dataEngine';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Edit, Trash2, Columns3 } from 'lucide-react';
import { useState } from 'react';
import { TableForm } from './TableForm';

interface TableListProps {
  tables: Table[];
}

export function TableList({ tables }: TableListProps) {
  const { removeTable } = useDataEngine();
  const [editingTable, setEditingTable] = useState<Table | null>(null);

  const handleDelete = async (id: string | undefined) => {
    if (!id) return;
    if (confirm('Are you sure you want to delete this table?')) {
      await removeTable(id);
    }
  };

  const handleEditClose = () => {
    setEditingTable(null);
  };

  if (editingTable) {
    return (
      <Card className="max-w-4xl">
        <TableForm table={editingTable} onClose={handleEditClose} />
      </Card>
    );
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {tables.map((table) => (
        <Card key={table.id}>
          <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-100 rounded-lg">
                <Columns3 className="w-5 h-5 text-green-600" />
              </div>
              <div>
                <h3 className="font-semibold text-gray-900">{table.name}</h3>
                <p className="text-sm text-gray-500">{table.columns.length} columns</p>
              </div>
            </div>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setEditingTable(table)}
              >
                <Edit className="w-4 h-4" />
              </Button>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => handleDelete(table.id)}
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
              >
                <Trash2 className="w-4 h-4" />
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {/* Columns List */}
            <div className="space-y-2 max-h-40 overflow-y-auto">
              {table.columns.map((column) => (
                <div
                  key={column.id}
                  className="flex items-center justify-between p-2 bg-gray-50 rounded text-sm"
                >
                  <div className="flex items-center gap-2">
                    <code className="font-mono text-gray-900">{column.name}</code>
                    <span className="text-xs text-gray-500 capitalize">{column.type}</span>
                  </div>
                  <div className="flex gap-1">
                    {column.primaryKey && (
                      <span className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded">
                        PK
                      </span>
                    )}
                    {column.unique && (
                      <span className="px-2 py-0.5 bg-purple-100 text-purple-700 text-xs rounded">
                        UNIQUE
                      </span>
                    )}
                    {!column.nullable && (
                      <span className="px-2 py-0.5 bg-orange-100 text-orange-700 text-xs rounded">
                        NOT NULL
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>

            {/* Meta Info */}
            <div className="mt-4 pt-4 border-t border-gray-200 text-xs text-gray-500">
              <p>Created: {table.createdAt ? new Date(table.createdAt).toLocaleDateString() : 'N/A'}</p>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
