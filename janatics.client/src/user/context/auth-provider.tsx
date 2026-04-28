import type { User } from "@/types";
import { createContext, useContext, useState } from "react";

export const AuthContext = createContext(undefined);

const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);

  const register = (userData: User) => {
    setUser(userData);
  };

  const login = (userData) => {
    setUser(userData);
  };

  const logout = () => {
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export default AuthProvider;

const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

export { useAuth };
