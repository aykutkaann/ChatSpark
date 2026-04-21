import { createContext, useContext, useState, useEffect } from "react";
import type { ReactNode } from "react";
import { authApi } from "../api/authApi";
import { profileApi } from "../api/profileApi";
import { getAccessToken, setTokens, clearTokens } from "../utils/tokenStorage";
import { decodeToken } from "../utils/jwtDecode";
import type { CurrentUser } from "../types/auth";
import type { UpdateProfileRequest } from "../types/profile";

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: CurrentUser | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, displayName: string, password: string) => Promise<void>;
  logout: () => void;
  updateProfile: (data: UpdateProfileRequest) => Promise<void>;
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
        // Set basic identity from JWT immediately (synchronous)
        setUser({ id: payload.sub, email: payload.email, displayName: payload.name });
        setIsAuthenticated(true);
        // Then fetch full profile (bio, avatarUrl, etc.) asynchronously
        profileApi.getProfile()
          .then((res) => {
            setUser({
              id: res.data.id,
              email: res.data.email,
              displayName: res.data.displayName,
              avatarUrl: res.data.avatarUrl,
              bio: res.data.bio,
              websiteUrl: res.data.websiteUrl,
            });
          })
          .catch(() => { /* ignore — basic JWT data is still set */ });
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
    // Fetch full profile after login
    profileApi.getProfile()
      .then((res) => {
        setUser({
          id: res.data.id,
          email: res.data.email,
          displayName: res.data.displayName,
          avatarUrl: res.data.avatarUrl,
          bio: res.data.bio,
          websiteUrl: res.data.websiteUrl,
        });
      })
      .catch(() => {});
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

  const updateProfile = async (data: UpdateProfileRequest) => {
    const res = await profileApi.updateProfile(data);
    setUser((prev) =>
      prev
        ? {
            ...prev,
            displayName: res.data.displayName,
            avatarUrl: res.data.avatarUrl,
            bio: res.data.bio,
            websiteUrl: res.data.websiteUrl,
          }
        : null
    );
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, user, login, register, logout, updateProfile }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
