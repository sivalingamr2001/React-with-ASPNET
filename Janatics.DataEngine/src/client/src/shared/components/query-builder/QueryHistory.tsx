'use client';

import type { Query } from '@/types/dataEngine';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Trash2, Copy } from 'lucide-react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import { toast } from 'sonner';

interface QueryHistoryProps {
  queries: Query[];
}

export function QueryHistory({ queries }: QueryHistoryProps) {
  const { removeQuery } = useDataEngine();

  const handleCopy = (query: Query) => {
    const queryText = query.sql || JSON.stringify(query.fields);
    navigator.clipboard.writeText(queryText);
    toast.success('Query copied to clipboard');
  };

  const handleDelete = (id: string | undefined) => {
    if (!id) return;
    if (confirm('Are you sure you want to delete this query?')) {
      removeQuery(id);
    }
  };

  if (queries.length === 0) {
    return (
      <Card>
        <CardContent className="py-12 flex items-center justify-center">
          <p className="text-gray-500">No saved queries yet</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {queries.map((query) => (
        <Card key={query.id}>
          <CardHeader className="pb-3">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1">
                <CardTitle className="text-lg">{query.name}</CardTitle>
                <CardDescription className="mt-1">
                  Created: {query.createdAt ? new Date(query.createdAt).toLocaleDateString() : 'N/A'}
                </CardDescription>
              </div>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => handleCopy(query)}
                  className="gap-2"
                >
                  <Copy className="w-4 h-4" />
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => handleDelete(query.id)}
                  className="text-red-600 hover:text-red-700 hover:bg-red-50 gap-2"
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {query.sql ? (
              <pre className="bg-gray-50 p-3 rounded-lg overflow-x-auto text-xs font-mono text-gray-700">
                {query.sql}
              </pre>
            ) : query.fields ? (
              <div className="space-y-2">
                <p className="text-sm font-medium text-gray-700">Fields:</p>
                <div className="grid grid-cols-2 gap-2">
                  {query.fields.map((field, idx) => (
                    <div key={idx} className="bg-gray-50 px-3 py-2 rounded text-xs text-gray-600">
                      <span className="font-mono">{field.columnName}</span>
                      {field.alias && <span> as {field.alias}</span>}
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <p className="text-gray-500 text-sm">No query details available</p>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
