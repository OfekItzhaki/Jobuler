"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import AppShell from "@/components/shell/AppShell";
import ScheduleTable from "@/components/schedule/ScheduleTable";
import { getCurrentSchedule, ScheduleVersionDetailDto } from "@/lib/api/schedule";
import { useSpaceStore } from "@/lib/store/spaceStore";

export default function TodayPage() {
  const t = useTranslations("schedule");
  const { currentSpaceId } = useSpaceStore();
  const [data, setData] = useState<ScheduleVersionDetailDto | null>(null);
  const [loading, setLoading] = useState(true);

  const today = new Date().toISOString().split("T")[0];
  const todayLabel = new Date().toLocaleDateString(undefined, {
    weekday: "long", year: "numeric", month: "long", day: "numeric"
  });

  useEffect(() => {
    if (!currentSpaceId) { setLoading(false); return; }
    getCurrentSchedule(currentSpaceId)
      .then(setData)
      .finally(() => setLoading(false));
  }, [currentSpaceId]);

  return (
    <AppShell>
      <div className="max-w-4xl space-y-6">
        {/* Page header */}
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">{t("title")}</h1>
            <p className="text-sm text-slate-500 mt-1 capitalize">{todayLabel}</p>
          </div>
          <div className="flex items-center gap-1.5 px-3 py-1.5 bg-blue-50 border border-blue-100 rounded-full">
            <span className="w-2 h-2 rounded-full bg-blue-500 animate-pulse" />
            <span className="text-xs font-medium text-blue-600">Live</span>
          </div>
        </div>

        {/* No space selected */}
        {!currentSpaceId && (
          <div className="flex items-center gap-3 bg-amber-50 border border-amber-200 rounded-xl px-4 py-3.5">
            <svg className="w-5 h-5 text-amber-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <p className="text-sm text-amber-700">No space selected. Go to Settings to select a space.</p>
          </div>
        )}

        {/* Loading */}
        {loading && (
          <div className="flex items-center gap-3 text-slate-400 text-sm py-8">
            <svg className="animate-spin h-5 w-5 text-blue-400" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            Loading schedule...
          </div>
        )}

        {/* Schedule table */}
        {!loading && data && (
          <ScheduleTable
            assignments={data.assignments}
            filterDate={today}
          />
        )}

        {/* No data */}
        {!loading && !data && currentSpaceId && (
          <div className="flex flex-col items-center justify-center py-16 text-center bg-white rounded-xl border border-slate-200">
            <svg className="w-12 h-12 text-slate-200 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <p className="text-slate-500 text-sm">{t("noAssignments")}</p>
          </div>
        )}
      </div>
    </AppShell>
  );
}
