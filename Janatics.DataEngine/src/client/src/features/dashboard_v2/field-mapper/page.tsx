'use client';

import { useState } from 'react';
import { RoleGuard } from '@/components/auth/RoleGuard';
import { FieldMappingForm } from '@/components/field-mapper/FieldMappingForm';
import { FieldMappingList } from '@/components/field-mapper/FieldMappingList';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Plus } from 'lucide-react';

export default function FieldMapperPage() {
  const [showForm, setShowForm] = useState(false);

  return (
    <RoleGuard requiredPermissions={['use_field_mapper']} fallback={
      <div className="flex h-full items-center justify-center">
        <p className="text-gray-500">You don't have permission to access this page</p>
      </div>
    }>
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="border-b border-gray-200 bg-white px-8 py-6 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Field Mapper</h1>
            <p className="text-gray-600 mt-1">Map and transform data fields</p>
          </div>
          <Button onClick={() => setShowForm(true)} className="gap-2">
            <Plus className="w-4 h-4" />
            New Mapping
          </Button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto p-8">
          {showForm ? (
            <Card className="max-w-2xl">
              <FieldMappingForm onClose={() => setShowForm(false)} />
            </Card>
          ) : (
            <FieldMappingList />
          )}
        </div>
      </div>
    </RoleGuard>
  );
}
