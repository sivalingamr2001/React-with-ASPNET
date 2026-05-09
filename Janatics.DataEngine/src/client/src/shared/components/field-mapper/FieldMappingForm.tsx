'use client';

import { useState } from 'react';
import { CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { FieldMapping } from '@/types/dataEngine';

const TRANSFORMATIONS = [
  { value: 'uppercase', label: 'Uppercase' },
  { value: 'lowercase', label: 'Lowercase' },
  { value: 'trim', label: 'Trim Whitespace' },
  { value: 'replace', label: 'Replace Text' },
  { value: 'substring', label: 'Substring' },
  { value: 'concatenate', label: 'Concatenate' },
  { value: 'custom', label: 'Custom Function' },
];

interface FieldMappingFormProps {
  onClose: () => void;
  mapping?: FieldMapping;
}

export function FieldMappingForm({ onClose, mapping }: FieldMappingFormProps) {
  const [formData, setFormData] = useState<FieldMapping>(
    mapping || {
      name: '',
      sourceField: '',
      targetField: '',
      transformation: '',
    }
  );

  const handleChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Save logic would go here
    console.log('[v0] Saving field mapping:', formData);
    onClose();
  };

  return (
    <>
      <CardHeader>
        <CardTitle>{mapping ? 'Edit' : 'Create'} Field Mapping</CardTitle>
        <CardDescription>Define how fields are mapped and transformed</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Mapping Name */}
          <div>
            <label className="text-sm font-medium block mb-2">Mapping Name</label>
            <Input
              value={formData.name}
              onChange={(e) => handleChange('name', e.target.value)}
              placeholder="e.g., User to Customer"
              required
            />
          </div>

          {/* Source & Target Fields */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-sm font-medium block mb-2">Source Field</label>
              <Input
                value={formData.sourceField}
                onChange={(e) => handleChange('sourceField', e.target.value)}
                placeholder="user_name"
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium block mb-2">Target Field</label>
              <Input
                value={formData.targetField}
                onChange={(e) => handleChange('targetField', e.target.value)}
                placeholder="customer_name"
                required
              />
            </div>
          </div>

          {/* Transformation */}
          <div>
            <label className="text-sm font-medium block mb-2">Transformation</label>
            <Select
              value={formData.transformation || ''}
              onValueChange={(value) => handleChange('transformation', value)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select transformation (optional)" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">None</SelectItem>
                {TRANSFORMATIONS.map((t) => (
                  <SelectItem key={t.value} value={t.value}>
                    {t.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Custom Transformation */}
          {formData.transformation === 'custom' && (
            <div>
              <label className="text-sm font-medium block mb-2">Custom Function</label>
              <Textarea
                value={formData.transformation || ''}
                onChange={(e) => handleChange('transformation', e.target.value)}
                placeholder="function transform(value) { return value.toUpperCase(); }"
                className="font-mono text-xs"
                rows={6}
              />
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <Button type="submit" className="flex-1">
              Save Mapping
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
