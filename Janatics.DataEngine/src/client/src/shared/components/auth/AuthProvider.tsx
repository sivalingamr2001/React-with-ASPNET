'use client';

import { type ReactNode, useEffect } from 'react';
import { useAuth } from '@/lib/hooks/useAuth';

export function AuthProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    // Check if user is authenticated on mount
    if (typeof window !== 'undefined') {
      const savedUser = localStorage.getItem('authUser');
      if (!savedUser) {
        console.log('[v0] No authenticated user found');
      }
    }
  }, [isAuthenticated]);

  return <>{children}</>;
}
