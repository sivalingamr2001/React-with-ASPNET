'use client';

import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Edit, Trash2, ArrowRight } from 'lucide-react';

// Mock data for demonstration
const mockMappings = [
  {
    id: '1',
    name: 'User to Customer',
    sourceField: 'user_name',
    targetField: 'customer_name',
    transformation: 'uppercase',
  },
  {
    id: '2',
    name: 'Email Normalization',
    sourceField: 'email_address',
    targetField: 'email',
    transformation: 'lowercase',
  },
  {
    id: '3',
    name: 'Phone Number Format',
    sourceField: 'phone',
    targetField: 'phone_formatted',
    transformation: 'custom',
  },
];

export function FieldMappingList() {
  if (mockMappings.length === 0) {
    return (
      <Card>
        <CardContent className="py-12 flex items-center justify-center">
          <p className="text-gray-500">No field mappings yet</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      {mockMappings.map((mapping) => (
        <Card key={mapping.id}>
          <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-4">
            <div>
              <h3 className="font-semibold text-gray-900">{mapping.name}</h3>
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="ghost">
                <Edit className="w-4 h-4" />
              </Button>
              <Button size="sm" variant="ghost" className="text-red-600 hover:bg-red-50">
                <Trash2 className="w-4 h-4" />
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Field Mapping */}
            <div className="flex items-center gap-3">
              <div className="flex-1">
                <p className="text-xs text-gray-500 mb-1">Source</p>
                <p className="font-mono text-sm bg-gray-50 p-2 rounded text-gray-900">
                  {mapping.sourceField}
                </p>
              </div>
              <ArrowRight className="w-4 h-4 text-gray-400 flex-shrink-0 mt-4" />
              <div className="flex-1">
                <p className="text-xs text-gray-500 mb-1">Target</p>
                <p className="font-mono text-sm bg-gray-50 p-2 rounded text-gray-900">
                  {mapping.targetField}
                </p>
              </div>
            </div>

            {/* Transformation */}
            {mapping.transformation && (
              <div>
                <p className="text-xs text-gray-500 mb-2">Transformation</p>
                <div className="bg-blue-50 border border-blue-200 px-3 py-2 rounded">
                  <p className="text-sm text-blue-900 capitalize">{mapping.transformation}</p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
