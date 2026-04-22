"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/lib/store/authStore";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { clsx } from "clsx";
import NotificationBell from "@/components/shell/NotificationBell";

interface AppShellProps {
  children: React.ReactNode;
}

// Inline SVG icons
function IconCalendarToday() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
    </svg>
  );
}

function IconCalendarNext() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 13l2 2 4-4" />
    </svg>
  );
}

function IconSchedule() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
    </svg>
  );
}

function IconPeople() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
    </svg>
  );
}

function IconTasks() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
  );
}

function IconConstraints() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
    </svg>
  );
}

function IconGroups() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
    </svg>
  );
}

function IconLogs() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
    </svg>
  );
}

function IconMenu() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
    </svg>
  );
}

function IconX() {
  return (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
    </svg>
  );
}

function IconLogout() {
  return (
    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
    </svg>
  );
}

function IconShield() {
  return (
    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
    </svg>
  );
}

interface NavLinkProps {
  href: string;
  icon: React.ReactNode;
  label: string;
  isAdmin?: boolean;
  onClick?: () => void;
}

function NavLink({ href, icon, label, isAdmin, onClick }: NavLinkProps) {
  const pathname = usePathname();
  const isActive = pathname === href || pathname.startsWith(href + "/");

  return (
    <Link
      href={href}
      onClick={onClick}
      className={clsx(
        "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150",
        isActive
          ? isAdmin
            ? "bg-amber-500/20 text-amber-300"
            : "bg-blue-500/20 text-blue-300"
          : isAdmin
            ? "text-amber-200/70 hover:bg-amber-500/10 hover:text-amber-200"
            : "text-slate-300 hover:bg-slate-700/60 hover:text-white"
      )}
    >
      <span className={clsx(
        "shrink-0",
        isActive
          ? isAdmin ? "text-amber-400" : "text-blue-400"
          : isAdmin ? "text-amber-400/60" : "text-slate-400"
      )}>
        {icon}
      </span>
      <span>{label}</span>
      {isActive && (
        <span className={clsx(
          "ms-auto w-1.5 h-1.5 rounded-full",
          isAdmin ? "bg-amber-400" : "bg-blue-400"
        )} />
      )}
    </Link>
  );
}

export default function AppShell({ children }: AppShellProps) {
  const t = useTranslations();
  const { displayName, isAdminMode, enterAdminMode, exitAdminMode, logout } = useAuthStore();
  const { currentSpaceName } = useSpaceStore();
  const router = useRouter();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  async function handleLogout() {
    await logout();
    router.push("/login");
  }

  const sidebarContent = (
    <div className="flex flex-col h-full">
      {/* Logo / Space name */}
      <div className="px-4 py-5 border-b border-slate-700/50">
        <Link href="/spaces" className="flex items-center gap-2.5 group">
          <div className="w-8 h-8 rounded-lg bg-blue-500 flex items-center justify-center shrink-0">
            <svg className="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </div>
          <div className="min-w-0">
            <p className="text-white font-semibold text-sm leading-tight truncate">Jobuler</p>
            {currentSpaceName && (
              <p className="text-slate-400 text-xs truncate group-hover:text-slate-300 transition-colors">
                {currentSpaceName}
              </p>
            )}
          </div>
        </Link>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {/* Regular nav */}
        <p className="text-slate-500 text-[10px] font-semibold uppercase tracking-widest px-3 mb-2">
          {t("nav.schedule") || "Schedule"}
        </p>
        <NavLink
          href="/schedule/today"
          icon={<IconCalendarToday />}
          label={t("nav.today")}
          onClick={() => setSidebarOpen(false)}
        />
        <NavLink
          href="/schedule/tomorrow"
          icon={<IconCalendarNext />}
          label={t("nav.tomorrow")}
          onClick={() => setSidebarOpen(false)}
        />

        {/* Admin nav */}
        {isAdminMode && (
          <>
            <div className="pt-4 pb-2">
              <p className="text-amber-500/70 text-[10px] font-semibold uppercase tracking-widest px-3">
                {t("admin.title")}
              </p>
            </div>
            <NavLink
              href="/admin/schedule"
              icon={<IconSchedule />}
              label={t("admin.title")}
              isAdmin
              onClick={() => setSidebarOpen(false)}
            />
            <NavLink
              href="/admin/people"
              icon={<IconPeople />}
              label={t("admin.people")}
              isAdmin
              onClick={() => setSidebarOpen(false)}
            />
            <NavLink
              href="/admin/tasks"
              icon={<IconTasks />}
              label={t("admin.tasks")}
              isAdmin
              onClick={() => setSidebarOpen(false)}
            />
            <NavLink
              href="/admin/constraints"
              icon={<IconConstraints />}
              label={t("admin.constraints")}
              isAdmin
              onClick={() => setSidebarOpen(false)}
            />
            <NavLink
              href="/admin/groups"
              icon={<IconGroups />}
              label={t("admin.groups")}
              isAdmin
              onClick={() => setSidebarOpen(false)}
            />
            <NavLink
              href="/admin/logs"
              icon={<IconLogs />}
              label={t("nav.logs")}
              isAdmin
              onClick={() => setSidebarOpen(false)}
            />
          </>
        )}
      </nav>

      {/* Bottom user section */}
      <div className="px-3 py-4 border-t border-slate-700/50 space-y-2">
        {displayName && (
          <div className="px-3 py-2">
            <p className="text-slate-400 text-xs">Signed in as</p>
            <p className="text-white text-sm font-medium truncate">{displayName}</p>
          </div>
        )}
        <button
          onClick={handleLogout}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-slate-400 hover:text-white hover:bg-slate-700/60 transition-all duration-150"
        >
          <IconLogout />
          <span>{t("auth.logout")}</span>
        </button>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen flex bg-slate-50">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex flex-col w-64 bg-slate-900 fixed inset-y-0 start-0 z-30">
        {sidebarContent}
      </aside>

      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Mobile sidebar drawer */}
      <aside className={clsx(
        "fixed inset-y-0 start-0 z-50 w-64 bg-slate-900 flex flex-col lg:hidden transition-transform duration-300",
        sidebarOpen ? "translate-x-0" : "-translate-x-full"
      )}>
        {sidebarContent}
      </aside>

      {/* Main content area */}
      <div className="flex-1 flex flex-col lg:ms-64 min-w-0">
        {/* Top bar */}
        <header className={clsx(
          "sticky top-0 z-20 flex items-center justify-between px-4 md:px-6 h-14 border-b backdrop-blur-sm",
          isAdminMode
            ? "bg-amber-50/95 border-amber-200"
            : "bg-white/95 border-slate-200"
        )}>
          {/* Left: mobile menu + page context */}
          <div className="flex items-center gap-3">
            <button
              onClick={() => setSidebarOpen(true)}
              className="lg:hidden p-2 rounded-lg text-slate-500 hover:bg-slate-100 hover:text-slate-700 transition-colors"
              aria-label="Open menu"
            >
              <IconMenu />
            </button>

            {isAdminMode && (
              <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-amber-100 border border-amber-200">
                <IconShield />
                <span className="text-xs font-semibold text-amber-700 uppercase tracking-wide">
                  {t("admin.title")}
                </span>
              </div>
            )}
          </div>

          {/* Right: actions */}
          <div className="flex items-center gap-2">
            <NotificationBell />

            {isAdminMode ? (
              <button
                onClick={exitAdminMode}
                className="flex items-center gap-1.5 text-xs font-medium text-amber-700 bg-amber-100 border border-amber-200 rounded-lg px-3 py-1.5 hover:bg-amber-200 transition-colors"
              >
                {t("admin.exitAdmin")}
              </button>
            ) : (
              <button
                onClick={enterAdminMode}
                className="flex items-center gap-1.5 text-xs font-medium text-slate-600 bg-slate-100 border border-slate-200 rounded-lg px-3 py-1.5 hover:bg-slate-200 transition-colors"
              >
                <IconShield />
                {t("admin.enterAdmin")}
              </button>
            )}
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 p-4 md:p-6 lg:p-8">
          {children}
        </main>
      </div>
    </div>
  );
}
