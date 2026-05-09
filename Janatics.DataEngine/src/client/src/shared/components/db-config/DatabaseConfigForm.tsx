'use client';

import { useState } from 'react';
import { useDataEngine } from '@/lib/hooks/useDataEngine';
import type { DatabaseConfig } from '@/types/dataEngine';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Loader2, Check, X } from 'lucide-react';

const DB_PROVIDERS = [
  { value: 'postgresql', label: 'PostgreSQL' },
  { value: 'mysql', label: 'MySQL' },
  { value: 'mongodb', label: 'MongoDB' },
  { value: 'dynamodb', label: 'DynamoDB' },
];

interface DatabaseConfigFormProps {
  onClose: () => void;
  config?: DatabaseConfig;
}

export function DatabaseConfigForm({ onClose, config }: DatabaseConfigFormProps) {
  const { createDatabaseConfig, testConnection, isLoading } = useDataEngine();
  const [formData, setFormData] = useState<DatabaseConfig>(
    config || {
      name: '',
      provider: 'postgresql',
      host: '',
      port: 5432,
      database: '',
      username: '',
      password: '',
      ssl: false,
    }
  );
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<boolean | null>(null);

  const handleChange = (field: string, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleTestConnection = async () => {
    setTesting(true);
    try {
      const result = await testConnection(formData);
      setTestResult(result);
    } finally {
      setTesting(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await createDatabaseConfig(formData);
    onClose();
  };

  return (
    <>
      <CardHeader>
        <CardTitle>{config ? 'Edit' : 'Add'} Database Configuration</CardTitle>
        <CardDescription>Connect to your database provider</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Basic Info */}
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium block mb-2">Configuration Name</label>
              <Input
                value={formData.name}
                onChange={(e) => handleChange('name', e.target.value)}
                placeholder="e.g., Production DB"
                required
              />
            </div>

            <div>
              <label className="text-sm font-medium block mb-2">Database Provider</label>
              <Select value={formData.provider} onValueChange={(value) => handleChange('provider', value)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {DB_PROVIDERS.map((provider) => (
                    <SelectItem key={provider.value} value={provider.value}>
                      {provider.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Connection Details */}
          {formData.provider !== 'mongodb' && formData.provider !== 'dynamodb' && (
            <div className="space-y-4">
              <h3 className="font-semibold text-gray-900">Connection Details</h3>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium block mb-2">Host</label>
                  <Input
                    value={formData.host || ''}
                    onChange={(e) => handleChange('host', e.target.value)}
                    placeholder="localhost"
                  />
                </div>
                <div>
                  <label className="text-sm font-medium block mb-2">Port</label>
                  <Input
                    type="number"
                    value={formData.port || ''}
                    onChange={(e) => handleChange('port', parseInt(e.target.value))}
                    placeholder="5432"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium block mb-2">Database</label>
                  <Input
                    value={formData.database || ''}
                    onChange={(e) => handleChange('database', e.target.value)}
                    placeholder="mydatabase"
                  />
                </div>
                <div>
                  <label className="text-sm font-medium block mb-2">Username</label>
                  <Input
                    value={formData.username || ''}
                    onChange={(e) => handleChange('username', e.target.value)}
                    placeholder="admin"
                  />
                </div>
              </div>
              <div>
                <label className="text-sm font-medium block mb-2">Password</label>
                <Input
                  type="password"
                  value={formData.password || ''}
                  onChange={(e) => handleChange('password', e.target.value)}
                  placeholder="••••••••"
                />
              </div>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.ssl || false}
                  onChange={(e) => handleChange('ssl', e.target.checked)}
                  className="w-4 h-4"
                />
                <span className="text-sm font-medium">Use SSL</span>
              </label>
            </div>
          )}

          {/* Connection String */}
          {(formData.provider === 'mongodb' || formData.provider === 'dynamodb') && (
            <div>
              <label className="text-sm font-medium block mb-2">Connection String</label>
              <Input
                value={formData.connectionString || ''}
                onChange={(e) => handleChange('connectionString', e.target.value)}
                placeholder="mongodb+srv://user:pass@cluster.mongodb.net/db"
              />
            </div>
          )}

          {/* Test Connection */}
          <div className="pt-4 border-t border-gray-200">
            <Button
              type="button"
              variant="outline"
              onClick={handleTestConnection}
              disabled={testing || !formData.name}
              className="w-full gap-2"
            >
              {testing ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Testing...
                </>
              ) : testResult === true ? (
                <>
                  <Check className="w-4 h-4 text-green-600" />
                  Connection Successful
                </>
              ) : testResult === false ? (
                <>
                  <X className="w-4 h-4 text-red-600" />
                  Connection Failed
                </>
              ) : (
                'Test Connection'
              )}
            </Button>
          </div>

          {/* Actions */}
          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <Button
              type="submit"
              disabled={isLoading || !formData.name}
              className="flex-1"
            >
              {isLoading ? 'Saving...' : 'Save Configuration'}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              className="flex-1"
            >
              Cancel
            </Button>
          </div>
        </form>
      </CardContent>
    </>
  );
}
