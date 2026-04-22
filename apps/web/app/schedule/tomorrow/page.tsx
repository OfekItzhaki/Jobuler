"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import AppShell from "@/components/shell/AppShell";
import ScheduleTable from "@/components/schedule/ScheduleTable";
import { getCurrentSchedule, ScheduleVersionDetailDto } from "@/lib/api/schedule";
import { useSpaceStore } from "@/lib/store/spaceStore";

export default function TomorrowPage() {
  const t = useTranslations("schedule");
  const { currentSpaceId } = useSpaceStore();
  const [data, setData] = useState<ScheduleVersionDetailDto | null>(null);
  const [loading, setLoading] = useState(true);

  const tomorrow = new Date(Date.now() + 86400000).toISOString().split("T")[0];
  const tomorrowLabel = new Date(Date.now() + 86400000).toLocaleDateString(undefined, {
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
        <div>
          <h1 className="text-2xl font-bold text-slate-900">{t("title")}</h1>
          <p className="text-sm text-slate-500 mt-1 capitalize">{tomorrowLabel}</p>
        </div>

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
            filterDate={tomorrow}
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
