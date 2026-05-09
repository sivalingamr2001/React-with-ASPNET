'use client';

import { useState } from 'react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import type { Table, Column } from '@/types/dataEngine';
import { CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

const COLUMN_TYPES = [
  'string',
  'number',
  'boolean',
  'date',
  'timestamp',
  'json',
];

interface TableFormProps {
  onClose: () => void;
  table?: Table;
}

export function TableForm({ onClose, table }: TableFormProps) {
  const { createTable, selectedConfigId, isLoading } = useDataEngine();

  const [formData, setFormData] = useState<Table>(
    table || {
      name: '',
      databaseConfigId: selectedConfigId || '',
      columns: [
        { id: '1', name: 'id', type: 'string', primaryKey: true, unique: true },
      ],
    }
  );

  const handleTableNameChange = (name: string) => {
    setFormData((prev) => ({ ...prev, name }));
  };

  const handleColumnChange = (idx: number, field: string, value: any) => {
    setFormData((prev) => {
      const newColumns = [...prev.columns];
      newColumns[idx] = { ...newColumns[idx], [field]: value };
      return { ...prev, columns: newColumns };
    });
  };

  const handleAddColumn = () => {
    const newColumn: Column = {
      id: Date.now().toString(),
      name: `column_${formData.columns.length + 1}`,
      type: 'string',
    };
    setFormData((prev) => ({
      ...prev,
      columns: [...prev.columns, newColumn],
    }));
  };

  const handleRemoveColumn = (idx: number) => {
    if (formData.columns.length <= 1) {
      toast.error('Table must have at least one column');
      return;
    }
    setFormData((prev) => ({
      ...prev,
      columns: prev.columns.filter((_, i) => i !== idx),
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name) {
      toast.error('Table name is required');
      return;
    }
    await createTable(formData);
    onClose();
  };

  return (
    <>
      <CardHeader>
        <CardTitle>{table ? 'Edit' : 'Create'} Table</CardTitle>
        <CardDescription>Define table structure and columns</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Table Name */}
          <div>
            <label className="text-sm font-medium block mb-2">Table Name</label>
            <Input
              value={formData.name}
              onChange={(e) => handleTableNameChange(e.target.value)}
              placeholder="e.g., users"
              required
            />
          </div>

          {/* Columns */}
          <div>
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-semibold text-gray-900">Columns</h3>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleAddColumn}
                className="gap-2"
              >
                <Plus className="w-4 h-4" />
                Add Column
              </Button>
            </div>

            <div className="space-y-4 max-h-96 overflow-y-auto">
              {formData.columns.map((column, idx) => (
                <div key={column.id} className="border border-gray-200 rounded-lg p-4">
                  <div className="grid grid-cols-2 gap-4 mb-3">
                    <Input
                      value={column.name}
                      onChange={(e) => handleColumnChange(idx, 'name', e.target.value)}
                      placeholder="Column name"
                      required
                    />
                    <Select
                      value={column.type}
                      onValueChange={(value) => handleColumnChange(idx, 'type', value)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {COLUMN_TYPES.map((type) => (
                          <SelectItem key={type} value={type}>
                            {type}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  {/* Column Options */}
                  <div className="flex items-center gap-4 flex-wrap">
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={column.nullable || false}
                        onChange={(e) => handleColumnChange(idx, 'nullable', e.target.checked)}
                        className="w-4 h-4"
                      />
                      <span className="text-sm text-gray-700">Nullable</span>
                    </label>

                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={column.primaryKey || false}
                        onChange={(e) => handleColumnChange(idx, 'primaryKey', e.target.checked)}
                        className="w-4 h-4"
                      />
                      <span className="text-sm text-gray-700">Primary Key</span>
                    </label>

                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={column.unique || false}
                        onChange={(e) => handleColumnChange(idx, 'unique', e.target.checked)}
                        className="w-4 h-4"
                      />
                      <span className="text-sm text-gray-700">Unique</span>
                    </label>

                    {formData.columns.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => handleRemoveColumn(idx)}
                        className="ml-auto text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <Button type="submit" disabled={isLoading} className="flex-1">
              {isLoading ? 'Creating...' : 'Create Table'}
            </Button>
            <Button type="button" variant="outline" onClick={onClose} className="flex-1">
              Cancel
            </Button>
          </div>
        </form>
      </CardContent>
    </>
  );
}
