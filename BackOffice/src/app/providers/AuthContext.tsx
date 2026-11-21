import { createContext, useCallback, useContext, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { hasPermission } from "../../shared/constants/permissions";
import type { Permission } from "../../shared/constants/permissions";

/**
 * Oturum yonetimi (dokuman 7). Token localStorage'da TUTULMAZ;
 * sessionStorage kullanimi sekme kapanisinda oturumu da bitirir.
 * Nihai yetki kontrolu backend'dedir; burada yalnizca UX kurulur.
 */

interface AuthState {
  token: string;
  userId: string;
  email: string;
  role: string;
}

interface AuthContextValue {
  auth: AuthState | null;
  is_authenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  has_permission: (permission: Permission) => boolean;
}

const STORAGE_KEY = "backoffice.auth";

const AuthContext = createContext<AuthContextValue | null>(null);

function readStored(): AuthState | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as AuthState) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(readStored);

  const login = useCallback(async (email: string, password: string) => {
    const response = await fetch("/api/v1/admin/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });

    const body = await response.json().catch(() => null);

    if (!response.ok) {
      throw new Error(
        response.status === 401
          ? "E-posta veya sifre hatali, ya da bu hesap backoffice erisimine sahip degil."
          : "Giris basarisiz. Lutfen tekrar deneyin.",
      );
    }

    const state: AuthState = {
      token: body.accessToken,
      userId: body.userId,
      email: body.email,
      role: body.role,
    };

    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    sessionStorage.setItem("backoffice.token", state.token);
    setAuth(state);
  }, []);

  const logout = useCallback(() => {
    sessionStorage.removeItem(STORAGE_KEY);
    sessionStorage.removeItem("backoffice.token");
    setAuth(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      auth,
      is_authenticated: auth !== null,
      login,
      logout,
      has_permission: (permission) =>
        auth ? hasPermission([auth.role], permission) : false,
    }),
    [auth, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth, AuthProvider icinde kullanilmalidir.");
  return context;
}
