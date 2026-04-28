import React, { createContext, useContext, useEffect, useState } from "react";
import type { UserType } from "@/types";

type AuthContextType = {
  user: UserType | null;
  token: string | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<UserType | null>(null);
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    const storedToken = localStorage.getItem("token");
    const storedUser = localStorage.getItem("user");
    if (storedToken) setToken(storedToken);
    if (storedUser) setUser(JSON.parse(storedUser));
  }, []);

  //   const login = async (username: string, password: string) => {
  //     const res = await fetch("/api/auth/login", {
  //       method: "POST",
  //       headers: { "Content-Type": "application/json" },
  //       body: JSON.stringify({ username, password }),
  //     });
  //     if (!res.ok) {
  //       const txt = await res.text();
  //       throw new Error(txt || "Invalid credentials");
  //     }
  //     const data = await res.json();
  //     const u = {
  //       id: data.user.id,
  //       name: data.user.name,
  //       email: data.user.email,
  //       role: data.user.role,
  //       token: data.token,
  //     } as UserType;
  //     setToken(data.token);
  //     setUser(u);
  //     localStorage.setItem("token", data.token);
  //     localStorage.setItem("user", JSON.stringify(data.user));
  //   };

  const login = async (username: string, password: string) => {
    await new Promise((resolve) => setTimeout(resolve, 500));

    if (password === "password123") {
      const isMockAdmin = username === "admin";

      const mockData = {
        token: "mock-jwt-token-xyz",
        user: {
          id: "1",
          name: isMockAdmin ? "Admin User" : "Regular User",
          email: `${username}@example.com`,
          role: isMockAdmin ? "admin" : "user",
        },
      };

      const u = mockData.user as UserType;

      setToken(mockData.token);
      setUser(u);
      localStorage.setItem("token", mockData.token);
      localStorage.setItem("user", JSON.stringify(u));
    } else {
      throw new Error("Invalid credentials. Try 'password123'");
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem("token");
    localStorage.removeItem("user");
  };

  return (
    <AuthContext.Provider value={{ user, token, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
};

export default AuthProvider;
export { useAuth };
