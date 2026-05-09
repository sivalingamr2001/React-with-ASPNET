'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Loader2, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface QueryResultsProps {
  data: any;
  isLoading: boolean;
}

export function QueryResults({ data, isLoading }: QueryResultsProps) {
  if (isLoading) {
    return (
      <Card>
        <CardContent className="py-12 flex items-center justify-center">
          <div className="text-center">
            <Loader2 className="w-8 h-8 animate-spin text-blue-600 mx-auto mb-3" />
            <p className="text-gray-600">Executing query...</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!data) {
    return (
      <Card>
        <CardContent className="py-12 flex items-center justify-center">
          <p className="text-gray-500">Execute a query to see results</p>
        </CardContent>
      </Card>
    );
  }

  const rows = Array.isArray(data) ? data : [data];
  const columns = rows.length > 0 ? Object.keys(rows[0]) : [];

  const handleExport = () => {
    const csv = [
      columns.join(','),
      ...rows.map((row: any) =>
        columns.map((col) => {
          const value = row[col];
          // Escape CSV values
          if (value === null || value === undefined) return '';
          const stringValue = String(value);
          return stringValue.includes(',') ? `"${stringValue}"` : stringValue;
        }).join(',')
      ),
    ].join('\n');

    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'results.csv';
    a.click();
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0">
        <div>
          <CardTitle>Query Results</CardTitle>
          <CardDescription>{rows.length} rows returned</CardDescription>
        </div>
        <Button onClick={handleExport} variant="outline" size="sm" className="gap-2">
          <Download className="w-4 h-4" />
          Export CSV
        </Button>
      </CardHeader>
      <CardContent>
        {rows.length === 0 ? (
          <p className="text-gray-500 py-4">No results</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 bg-gray-50">
                  {columns.map((col) => (
                    <th
                      key={col}
                      className="px-4 py-3 text-left font-semibold text-gray-900"
                    >
                      {col}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((row: any, idx: number) => (
                  <tr key={idx} className="border-b border-gray-200 hover:bg-gray-50">
                    {columns.map((col) => (
                      <td key={`${idx}-${col}`} className="px-4 py-3 text-gray-600">
                        <code className="bg-gray-100 px-2 py-1 rounded text-xs">
                          {String(row[col] ?? '-')}
                        </code>
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
