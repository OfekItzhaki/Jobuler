import { create } from "zustand";
import { persist } from "zustand/middleware";
import { login as apiLogin, logout as apiLogout } from "@/lib/api/auth";

interface AuthState {
  userId: string | null;
  displayName: string | null;
  preferredLocale: string;
  isAuthenticated: boolean;
  isAdminMode: boolean;

  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  enterAdminMode: () => void;
  exitAdminMode: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      userId: null,
      displayName: null,
      preferredLocale: "he",
      isAuthenticated: false,
      isAdminMode: false,

      login: async (email, password) => {
        const result = await apiLogin(email, password);
        localStorage.setItem("access_token", result.accessToken);
        localStorage.setItem("refresh_token", result.refreshToken);
        // Set cookies for Next.js middleware route guard and SSR
        document.cookie = `access_token=${result.accessToken}; path=/; max-age=900; SameSite=Strict`;
        document.cookie = `locale=${result.preferredLocale}; path=/; max-age=31536000; SameSite=Strict`;
        set({
          userId: result.userId,
          displayName: result.displayName,
          preferredLocale: result.preferredLocale,
          isAuthenticated: true,
          isAdminMode: false,
        });
      },

      logout: async () => {
        const refreshToken = localStorage.getItem("refresh_token");
        if (refreshToken) {
          try { await apiLogout(refreshToken); } catch { /* best effort */ }
        }
        localStorage.removeItem("access_token");
        localStorage.removeItem("refresh_token");
        // Clear cookies
        document.cookie = "access_token=; path=/; max-age=0";
        document.cookie = "locale=; path=/; max-age=0";
        set({ userId: null, displayName: null, isAuthenticated: false, isAdminMode: false });
      },

      // Admin mode is a UI state — actual permission is checked by the API
      enterAdminMode: () => set({ isAdminMode: true }),
      exitAdminMode: () => set({ isAdminMode: false }),
    }),
    {
      name: "jobuler-auth",
      partialize: (state) => ({
        userId: state.userId,
        displayName: state.displayName,
        preferredLocale: state.preferredLocale,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
