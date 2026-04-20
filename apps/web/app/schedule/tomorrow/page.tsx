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
      <div className="space-y-4 max-w-4xl">
        <div>
          <h1 className="text-xl font-semibold">{t("title")}</h1>
          <p className="text-sm text-gray-500">{tomorrowLabel}</p>
        </div>

        {loading && <p className="text-gray-400 text-sm">Loading...</p>}

        {!loading && data && (
          <ScheduleTable
            assignments={data.assignments}
            filterDate={tomorrow}
          />
        )}

        {!loading && !data && currentSpaceId && (
          <p className="text-gray-400 text-sm">{t("noAssignments")}</p>
        )}
      </div>
    </AppShell>
  );
}
