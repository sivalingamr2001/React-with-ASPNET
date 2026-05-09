'use client';

import { useState } from 'react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import type { Query } from '@/types/dataEngine';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Loader2, Plus, Trash2 } from 'lucide-react';

interface QueryBuilderProps {
  onResults: (data: any) => void;
  isExecuting: boolean;
  setIsExecuting: (loading: boolean) => void;
}

export function QueryBuilder({ onResults, isExecuting, setIsExecuting }: QueryBuilderProps) {
  const { dbConfigs, selectedConfigId, executeQuery, saveQuery } =
    useDataEngine();

  const [query, setQuery] = useState<Query>({
    name: '',
    databaseConfigId: selectedConfigId || '',
    fields: [],
    filters: [],
  });

  const [sqlMode, setSqlMode] = useState(false);

  const handleExecute = async () => {
    setIsExecuting(true);
    try {
      const result = await executeQuery(query);
      onResults(result);
    } finally {
      setIsExecuting(false);
    }
  };

  const handleSave = async () => {
    if (!query.name) {
      alert('Please enter a query name');
      return;
    }
    await saveQuery(query);
  };

  return (
    <div className="space-y-6">
      {/* Configuration Selection */}
      <Card>
        <CardHeader>
          <CardTitle>Database Selection</CardTitle>
          <CardDescription>Choose which database to query</CardDescription>
        </CardHeader>
        <CardContent>
          <Select value={query.databaseConfigId} onValueChange={(value) =>
            setQuery((prev) => ({ ...prev, databaseConfigId: value }))
          }>
            <SelectTrigger>
              <SelectValue placeholder="Select database" />
            </SelectTrigger>
            <SelectContent>
              {dbConfigs.map((config) => (
                <SelectItem key={config.id} value={config.id || ''}>
                  {config.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      {/* Query Mode Selection */}
      <Card>
        <CardHeader>
          <CardTitle>Query Mode</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4">
            <button
              onClick={() => setSqlMode(false)}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                !sqlMode
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              Visual Builder
            </button>
            <button
              onClick={() => setSqlMode(true)}
              className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                sqlMode
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              SQL Mode
            </button>
          </div>
        </CardContent>
      </Card>

      {/* Query Builder */}
      {sqlMode ? (
        <Card>
          <CardHeader>
            <CardTitle>SQL Query</CardTitle>
            <CardDescription>Write your SQL query directly</CardDescription>
          </CardHeader>
          <CardContent>
            <textarea
              value={query.sql || ''}
              onChange={(e) => setQuery((prev) => ({ ...prev, sql: e.target.value }))}
              placeholder="SELECT * FROM table_name WHERE condition"
              className="w-full h-64 p-3 font-mono text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </CardContent>
        </Card>
      ) : (
        <>
          {/* Fields Selection */}
          <Card>
            <CardHeader>
              <CardTitle>Select Fields</CardTitle>
              <CardDescription>Choose columns to retrieve</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {query.fields?.map((field, idx) => (
                <div key={idx} className="flex gap-2">
                  <Input value={field.columnName} placeholder="Column name" readOnly />
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() =>
                      setQuery((prev) => ({
                        ...prev,
                        fields: prev.fields?.filter((_, i) => i !== idx),
                      }))
                    }
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              ))}
              <Button variant="outline" size="sm" className="w-full gap-2">
                <Plus className="w-4 h-4" />
                Add Field
              </Button>
            </CardContent>
          </Card>

          {/* Filters */}
          <Card>
            <CardHeader>
              <CardTitle>Filters</CardTitle>
              <CardDescription>Add conditions to your query</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {query.filters?.map((filter, idx) => (
                <div key={idx} className="flex gap-2 items-center">
                  <Input value={filter.field} placeholder="Field" readOnly className="flex-1" />
                  <Select defaultValue={filter.operator}>
                    <SelectTrigger className="w-24">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="eq">equals</SelectItem>
                      <SelectItem value="neq">not equals</SelectItem>
                      <SelectItem value="gt">greater</SelectItem>
                      <SelectItem value="gte">greater or equal</SelectItem>
                      <SelectItem value="lt">less</SelectItem>
                      <SelectItem value="lte">less or equal</SelectItem>
                      <SelectItem value="in">in list</SelectItem>
                      <SelectItem value="like">contains</SelectItem>
                    </SelectContent>
                  </Select>
                  <Input
                    value={String(filter.value)}
                    placeholder="Value"
                    className="flex-1"
                    readOnly
                  />
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() =>
                      setQuery((prev) => ({
                        ...prev,
                        filters: prev.filters?.filter((_, i) => i !== idx),
                      }))
                    }
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              ))}
              <Button variant="outline" size="sm" className="w-full gap-2">
                <Plus className="w-4 h-4" />
                Add Filter
              </Button>
            </CardContent>
          </Card>
        </>
      )}

      {/* Query Name & Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Save Query</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <Input
            value={query.name}
            onChange={(e) => setQuery((prev) => ({ ...prev, name: e.target.value }))}
            placeholder="Query name (optional)"
          />
          <div className="flex gap-3">
            <Button
              onClick={handleExecute}
              disabled={isExecuting || !query.databaseConfigId}
              className="flex-1 gap-2"
            >
              {isExecuting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Executing...
                </>
              ) : (
                'Execute Query'
              )}
            </Button>
            <Button
              variant="outline"
              onClick={handleSave}
              disabled={!query.name || !query.databaseConfigId}
              className="flex-1"
            >
              Save Query
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
