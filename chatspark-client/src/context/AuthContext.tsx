import { createContext, useContext, useState, useEffect } from "react";
import type { ReactNode } from "react";
import { authApi } from "../api/authApi";
import { getAccessToken, setTokens, clearTokens } from "../utils/tokenStorage";
import { decodeToken } from "../utils/jwtDecode";
import type { CurrentUser } from "../types/auth";

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: CurrentUser | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, displayName: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      const payload = decodeToken(token);
      if (payload) {
        setUser({ id: payload.sub, email: payload.email, displayName: payload.name });
        setIsAuthenticated(true);
      }
    }
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    const response = await authApi.login({ email, password });
    const { accessToken, refreshToken } = response.data;
    setTokens(accessToken, refreshToken);
    const payload = decodeToken(accessToken);
    if (payload) {
      setUser({ id: payload.sub, email: payload.email, displayName: payload.name });
    }
    setIsAuthenticated(true);
  };

  const register = async (email: string, displayName: string, password: string) => {
    const response = await authApi.register({ email, displayName, password });
    const { accessToken, refreshToken } = response.data;
    setTokens(accessToken, refreshToken);
    const payload = decodeToken(accessToken);
    if (payload) {
      setUser({ id: payload.sub, email: payload.email, displayName: payload.name });
    }
    setIsAuthenticated(true);
  };

  const logout = () => {
    clearTokens();
    setIsAuthenticated(false);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, user, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
