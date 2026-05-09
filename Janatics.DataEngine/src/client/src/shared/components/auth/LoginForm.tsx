'use client';

import { useState } from 'react';
import { useAuth } from '@/lib/hooks/useAuth';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from 'sonner';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/core/store/authStore';

const DEMO_USERS = [
  { email: 'master@example.com', password: 'master123', role: 'Master' },
  { email: 'admin@example.com', password: 'admin123', role: 'Admin' },
  { email: 'user@example.com', password: 'user123', role: 'User' },
  { email: 'vendor@example.com', password: 'vendor123', role: 'Vendor' },
];


export function LoginForm() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const setCredentials = useAuthStore((s) => s.setCredentials);
  
  const from = (location.state as { from?: Location })?.from?.pathname ?? "/dashboard";
  
  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      await login(email, password);
      toast.success('Logged in successfully');
      setCredentials(
      {
        id: "1",
        email: "admin@example.com",
        name: "Admin User",
        role: "admin",
      },
      "demo-token"
    );
      navigate(from, { replace: true });
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Login failed');
    } finally {
      setIsLoading(false);
    }
  };

  const handleQuickLogin = async (demoEmail: string, demoPassword: string) => {
    setIsLoading(true);
    try {
      await login(demoEmail, demoPassword);
      toast.success('Logged in successfully');
      navigate('/dashboard');
    } catch (error) {
      toast.error('Login failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl">Data Engine Console</CardTitle>
          <CardDescription>
            Sign in to access the data management platform
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <form onSubmit={handleLogin} className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Email</label>
              <Input
                type="email"
                placeholder="Enter your email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isLoading}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Password</label>
              <Input
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isLoading}
              />
            </div>
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? 'Signing in...' : 'Sign In'}
            </Button>
          </form>

          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-200"></div>
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="bg-white px-2 text-gray-500">Demo Accounts</span>
            </div>
          </div>

          <div className="space-y-2">
            <p className="text-xs text-gray-500 font-medium">Quick Access</p>
            {DEMO_USERS.map((user) => (
              <button
                key={user.email}
                onClick={() => handleQuickLogin(user.email, user.password)}
                className="w-full rounded-lg border border-gray-200 bg-white p-3 text-left text-sm hover:bg-gray-50 transition-colors"
                disabled={isLoading}
              >
                <div className="font-medium text-gray-900">{user.role}</div>
                <div className="text-xs text-gray-500">{user.email}</div>
              </button>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
