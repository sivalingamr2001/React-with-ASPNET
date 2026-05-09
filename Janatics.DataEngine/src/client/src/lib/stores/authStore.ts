import { create } from 'zustand';
import { UserRole } from '@/types/auth';
import type { User } from '@/types/auth';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  setUser: (user: User | null) => void;
}

// Mock users database
const mockUsers: Record<string, { password: string; user: User }> = {
  'master@example.com': {
    password: 'master123',
    user: {
      id: '1',
      email: 'master@example.com',
      name: 'Master Admin',
      role: UserRole.MASTER,
      avatar: '👑',
    },
  },
  'admin@example.com': {
    password: 'admin123',
    user: {
      id: '2',
      email: 'admin@example.com',
      name: 'Admin User',
      role: UserRole.ADMIN,
      avatar: '🔧',
    },
  },
  'user@example.com': {
    password: 'user123',
    user: {
      id: '3',
      email: 'user@example.com',
      name: 'Regular User',
      role: UserRole.USER,
      avatar: '👤',
    },
  },
  'vendor@example.com': {
    password: 'vendor123',
    user: {
      id: '4',
      email: 'vendor@example.com',
      name: 'Outside Firm Vendor',
      role: UserRole.OUTSIDE_FIRM_VENDOR,
      avatar: '🤝',
    },
  },
};

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: false,

  login: async (email: string, password: string) => {
    set({ isLoading: true });
    try {
      // Simulate API call delay
      await new Promise((resolve) => setTimeout(resolve, 500));

      const mockUser = mockUsers[email];
      if (!mockUser || mockUser.password !== password) {
        throw new Error('Invalid email or password');
      }

      // Store in localStorage
      localStorage.setItem('authUser', JSON.stringify(mockUser.user));

      set({
        user: mockUser.user,
        isAuthenticated: true,
        isLoading: false,
      });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  logout: () => {
    localStorage.removeItem('authUser');
    set({
      user: null,
      isAuthenticated: false,
    });
  },

  setUser: (user: User | null) => {
    if (user) {
      localStorage.setItem('authUser', JSON.stringify(user));
      set({ user, isAuthenticated: true });
    } else {
      localStorage.removeItem('authUser');
      set({ user: null, isAuthenticated: false });
    }
  },
}));

// Initialize auth from localStorage
if (typeof window !== 'undefined') {
  const savedUser = localStorage.getItem('authUser');
  if (savedUser) {
    try {
      const user = JSON.parse(savedUser);
      useAuthStore.setState({ user, isAuthenticated: true });
    } catch {
      localStorage.removeItem('authUser');
    }
  }
}
