'use client';

import { useState, useEffect } from 'react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import { RoleGuard } from '@/components/auth/RoleGuard';
import { QueryBuilder } from '@/components/query-builder/QueryBuilder';
import { QueryResults } from '@/components/query-builder/QueryResults';
import { QueryHistory } from '@/components/query-builder/QueryHistory';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

export default function QueryBuilderPage() {
  const { selectedConfigId, queries, loadQueries } = useDataEngine();
  const [results, setResults] = useState<any>(null);
  const [isExecuting, setIsExecuting] = useState(false);

  useEffect(() => {
    if (selectedConfigId) {
      loadQueries(selectedConfigId);
    }
  }, [selectedConfigId, loadQueries]);

  return (
    <RoleGuard requiredPermissions={['use_query_builder']} fallback={
      <div className="flex h-full items-center justify-center">
        <p className="text-gray-500">You don't have permission to access this page</p>
      </div>
    }>
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="border-b border-gray-200 bg-white px-8 py-6">
          <h1 className="text-3xl font-bold text-gray-900">Query Builder</h1>
          <p className="text-gray-600 mt-1">Build and execute database queries</p>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto">
          <Tabs defaultValue="builder" className="h-full">
            <TabsList className="border-b border-gray-200 rounded-none bg-white px-8 gap-8">
              <TabsTrigger value="builder">Builder</TabsTrigger>
              <TabsTrigger value="results">Results</TabsTrigger>
              <TabsTrigger value="history">History</TabsTrigger>
            </TabsList>

            <TabsContent value="builder" className="p-8 data-[state=active]:flex data-[state=active]:flex-col">
              <QueryBuilder
                onResults={setResults}
                isExecuting={isExecuting}
                setIsExecuting={setIsExecuting}
              />
            </TabsContent>

            <TabsContent value="results" className="p-8">
              <QueryResults data={results} isLoading={isExecuting} />
            </TabsContent>

            <TabsContent value="history" className="p-8">
              <QueryHistory queries={queries} />
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </RoleGuard>
  );
}
