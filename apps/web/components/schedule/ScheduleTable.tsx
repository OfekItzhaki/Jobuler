"use client";

import { useTranslations } from "next-intl";
import { AssignmentDto } from "@/lib/api/schedule";
import { clsx } from "clsx";

interface ScheduleTableProps {
  assignments: AssignmentDto[];
  filterDate?: string; // ISO date string — show only slots on this date
  title?: string;
}

export default function ScheduleTable({ assignments, filterDate, title }: ScheduleTableProps) {
  const t = useTranslations("schedule");

  const filtered = filterDate
    ? assignments.filter((a) => a.slotStartsAt.startsWith(filterDate))
    : assignments;

  const formatTime = (iso: string) =>
    new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });

  const sourceColor = (source: string) =>
    source === "Override" ? "text-amber-600 font-semibold" : "text-gray-700";

  return (
    <div className="space-y-3">
      {title && <h2 className="text-lg font-semibold">{title}</h2>}

      {filtered.length === 0 ? (
        <p className="text-gray-400 text-sm">{t("noAssignments")}</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-gray-200">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-gray-500 uppercase text-xs">
              <tr>
                <th className="px-4 py-3 text-start">{t("person")}</th>
                <th className="px-4 py-3 text-start">{t("task")}</th>
                <th className="px-4 py-3 text-start">{t("time")}</th>
                <th className="px-4 py-3 text-start">Source</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {filtered.map((a) => (
                <tr key={a.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 font-medium">{a.personName}</td>
                  <td className="px-4 py-3">{a.taskTypeName}</td>
                  <td className="px-4 py-3 tabular-nums text-gray-500">
                    {formatTime(a.slotStartsAt)} – {formatTime(a.slotEndsAt)}
                  </td>
                  <td className={clsx("px-4 py-3", sourceColor(a.source))}>
                    {a.source}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
