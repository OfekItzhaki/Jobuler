"use client";

import { useTranslations } from "next-intl";
import { useAuthStore } from "@/lib/store/authStore";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { clsx } from "clsx";

interface AppShellProps {
  children: React.ReactNode;
}

export default function AppShell({ children }: AppShellProps) {
  const t = useTranslations();
  const { displayName, isAdminMode, enterAdminMode, exitAdminMode, logout } = useAuthStore();
  const { currentSpaceName } = useSpaceStore();
  const router = useRouter();

  async function handleLogout() {
    await logout();
    router.push("/login");
  }

  return (
    <div className="min-h-screen flex flex-col">
      {/* Top navigation bar */}
      <header className={clsx(
        "flex items-center justify-between px-6 py-3 border-b",
        isAdminMode ? "bg-amber-50 border-amber-300" : "bg-white border-gray-200"
      )}>
        <nav className="flex items-center gap-6 text-sm font-medium">
          {currentSpaceName && (
            <Link href="/spaces" className="text-gray-400 hover:text-gray-600 text-xs">
              {currentSpaceName}
            </Link>
          )}
          <Link href="/schedule/today" className="hover:text-blue-600">
            {t("nav.today")}
          </Link>
          <Link href="/schedule/tomorrow" className="hover:text-blue-600">
            {t("nav.tomorrow")}
          </Link>
          {isAdminMode && (
            <>
              <Link href="/admin/schedule" className="text-amber-700 hover:text-amber-900">
                {t("admin.title")}
              </Link>
              <Link href="/admin/people" className="text-amber-700 hover:text-amber-900">
                {t("admin.people")}
              </Link>
              <Link href="/admin/logs" className="text-amber-700 hover:text-amber-900">
                {t("nav.logs")}
              </Link>
            </>
          )}
        </nav>

        <div className="flex items-center gap-4 text-sm">
          {isAdminMode ? (
            <span className="text-amber-700 font-semibold text-xs uppercase tracking-wide">
              {t("admin.title")}
            </span>
          ) : null}

          <span className="text-gray-500">{displayName}</span>

          {isAdminMode ? (
            <button
              onClick={exitAdminMode}
              className="text-xs text-amber-700 border border-amber-300 rounded px-2 py-1 hover:bg-amber-100"
            >
              {t("admin.exitAdmin")}
            </button>
          ) : (
            <button
              onClick={enterAdminMode}
              className="text-xs text-gray-600 border border-gray-300 rounded px-2 py-1 hover:bg-gray-100"
            >
              {t("admin.enterAdmin")}
            </button>
          )}

          <button
            onClick={handleLogout}
            className="text-xs text-red-600 hover:underline"
          >
            {t("auth.logout")}
          </button>
        </div>
      </header>

      {/* Page content */}
      <main className="flex-1 p-6">
        {children}
      </main>
    </div>
  );
}
