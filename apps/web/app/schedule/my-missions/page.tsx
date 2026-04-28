"use client";

import { useEffect, useState } from "react";
import AppShell from "@/components/shell/AppShell";
import Modal from "@/components/Modal";
import { apiClient } from "@/lib/api/client";
import { useSpaceStore } from "@/lib/store/spaceStore";

interface MyAssignmentDto {
  id: string;
  groupId: string;
  groupName: string;
  taskTypeName: string;
  slotStartsAt: string;
  slotEndsAt: string;
  source: string;
}

type Range = "today" | "week" | "month" | "year";

const RANGE_LABELS: Record<Range, string> = {
  today: "היום",
  week: "השבוע",
  month: "החודש",
  year: "השנה",
};

const DAY_NAMES_HE = ["ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת"];

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString("he-IL", { hour: "2-digit", minute: "2-digit" });
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString("he-IL", { weekday: "short", day: "numeric", month: "short" });
}

/** Returns the ISO date strings for the current week (Sun–Sat) */
function getCurrentWeekDays(): string[] {
  const today = new Date();
  const dayOfWeek = today.getDay(); // 0=Sun
  const sunday = new Date(today);
  sunday.setDate(today.getDate() - dayOfWeek);
  return Array.from({ length: 7 }, (_, i) => {
    const d = new Date(sunday);
    d.setDate(sunday.getDate() + i);
    return d.toISOString().split("T")[0];
  });
}

export default function MyMissionsPage() {
  const { currentSpaceId } = useSpaceStore();
  const [range, setRange] = useState<Range>("week");
  const [assignments, setAssignments] = useState<MyAssignmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  // Modal state for day missions
  const [selectedDay, setSelectedDay] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    if (!currentSpaceId) { setLoading(false); return; }
    setLoading(true);
    apiClient.get(`/spaces/${currentSpaceId}/my-assignments?range=${range}`)
      .then(r => setAssignments(r.data))
      .catch(() => setAssignments([]))
      .finally(() => setLoading(false));
  }, [currentSpaceId, range]);

  const filtered = assignments.filter(a =>
    !search ||
    a.taskTypeName.toLowerCase().includes(search.toLowerCase()) ||
    a.groupName.toLowerCase().includes(search.toLowerCase())
  );

  const byDate = filtered.reduce<Record<string, MyAssignmentDto[]>>((acc, a) => {
    const day = a.slotStartsAt.split("T")[0];
    if (!acc[day]) acc[day] = [];
    acc[day].push(a);
    return acc;
  }, {});

  // Week day buttons (only shown when range === "week")
  const weekDays = getCurrentWeekDays();
  const todayStr = new Date().toISOString().split("T")[0];

  function openDayModal(dayIso: string) {
    setSelectedDay(dayIso);
    setModalOpen(true);
  }

  const selectedDayMissions = selectedDay ? (byDate[selectedDay] ?? []) : [];
  const selectedDayIndex = selectedDay ? new Date(selectedDay + "T00:00:00").getDay() : -1;
  const selectedDayLabel = selectedDayIndex >= 0
    ? `${DAY_NAMES_HE[selectedDayIndex]} — ${new Date(selectedDay! + "T00:00:00").toLocaleDateString("he-IL", { day: "numeric", month: "long" })}`
    : "";

  return (
    <AppShell>
      <div className="max-w-3xl space-y-6">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">המשימות שלי</h1>
            <p className="text-sm text-slate-500 mt-1">כל המשימות שלך בכל הקבוצות</p>
          </div>
        </div>

        {/* Range selector */}
        <div className="flex gap-1 bg-slate-100 p-1 rounded-xl w-fit">
          {(Object.keys(RANGE_LABELS) as Range[]).map(r => (
            <button key={r} onClick={() => setRange(r)}
              className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-all ${
                range === r
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500 hover:text-slate-700"
              }`}>
              {RANGE_LABELS[r]}
            </button>
          ))}
        </div>

        {/* Week day buttons — shown only in week range */}
        {range === "week" && (
          <div className="flex flex-wrap gap-2">
            {weekDays.map((dayIso, i) => {
              const hasMissions = !!byDate[dayIso]?.length;
              const isToday = dayIso === todayStr;
              return (
                <button
                  key={dayIso}
                  onClick={() => openDayModal(dayIso)}
                  style={{
                    padding: "0.5rem 1rem",
                    borderRadius: 10,
                    fontSize: "0.875rem",
                    fontWeight: 600,
                    cursor: "pointer",
                    border: isToday ? "2px solid #3b82f6" : "1px solid #e2e8f0",
                    background: isToday ? "#eff6ff" : hasMissions ? "#f0fdf4" : "white",
                    color: isToday ? "#1d4ed8" : hasMissions ? "#15803d" : "#64748b",
                    position: "relative",
                    transition: "all 0.15s",
                  }}
                >
                  {DAY_NAMES_HE[i]}
                  {hasMissions && (
                    <span style={{
                      position: "absolute", top: -4, right: -4,
                      width: 8, height: 8, borderRadius: "50%",
                      background: "#22c55e", border: "2px solid white",
                    }} />
                  )}
                </button>
              );
            })}
          </div>
        )}

        {/* Search */}
        <div className="relative max-w-sm">
          <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="#94a3b8" strokeWidth={2}
            style={{ position: "absolute", left: 12, top: "50%", transform: "translateY(-50%)" }}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="חיפוש לפי סוג משימה או קבוצה..."
            className="w-full border border-slate-200 rounded-xl pl-9 pr-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {loading ? (
          <p className="text-slate-400 text-sm py-8">טוען...</p>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center bg-white rounded-xl border border-slate-200">
            <svg className="w-10 h-10 text-slate-200 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
            <p className="text-slate-400 text-sm">אין משימות ב{RANGE_LABELS[range]}</p>
          </div>
        ) : (
          <div className="space-y-6">
            {Object.entries(byDate).map(([day, items]) => (
              <div key={day}>
                <h2 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">
                  {formatDate(day + "T00:00:00")}
                </h2>
                <div className="space-y-2">
                  {items.map(a => (
                    <div key={a.id}
                      className="flex items-center gap-4 bg-white border border-slate-200 rounded-xl px-4 py-3 shadow-sm">
                      <div className="text-xs tabular-nums text-slate-500 w-24 shrink-0">
                        {formatTime(a.slotStartsAt)}<span className="mx-1 text-slate-300">–</span>{formatTime(a.slotEndsAt)}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-slate-900 truncate">{a.taskTypeName}</p>
                        <p className="text-xs text-slate-400 truncate">{a.groupName}</p>
                      </div>
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                        a.source === "Override"
                          ? "bg-amber-50 text-amber-700 border border-amber-200"
                          : "bg-slate-100 text-slate-500"
                      }`}>
                        {a.source === "Override" ? "עקיפה" : "סולבר"}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Day missions modal */}
      <Modal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        title={selectedDayLabel}
        maxWidth={560}
      >
        {selectedDayMissions.length === 0 ? (
          <div style={{ textAlign: "center", padding: "2rem 0", color: "#94a3b8", fontSize: "0.875rem" }}>
            אין משימות ביום זה
          </div>
        ) : (
          <div style={{ overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.875rem" }}>
              <thead>
                <tr style={{ borderBottom: "2px solid #f1f5f9" }}>
                  <th style={{ textAlign: "right", padding: "0.5rem 0.75rem", fontWeight: 600, color: "#64748b", fontSize: "0.75rem" }}>
                    שם המשימה
                  </th>
                  <th style={{ textAlign: "right", padding: "0.5rem 0.75rem", fontWeight: 600, color: "#64748b", fontSize: "0.75rem" }}>
                    שעות
                  </th>
                  <th style={{ textAlign: "right", padding: "0.5rem 0.75rem", fontWeight: 600, color: "#64748b", fontSize: "0.75rem" }}>
                    קבוצה
                  </th>
                </tr>
              </thead>
              <tbody>
                {selectedDayMissions.map((a, idx) => (
                  <tr
                    key={a.id}
                    style={{
                      borderBottom: idx < selectedDayMissions.length - 1 ? "1px solid #f1f5f9" : "none",
                      background: idx % 2 === 0 ? "white" : "#f8fafc",
                    }}
                  >
                    <td style={{ padding: "0.75rem", fontWeight: 500, color: "#0f172a" }}>
                      {a.taskTypeName}
                    </td>
                    <td style={{ padding: "0.75rem", color: "#475569", fontVariantNumeric: "tabular-nums", direction: "ltr", textAlign: "left" }}>
                      {formatTime(a.slotStartsAt)} – {formatTime(a.slotEndsAt)}
                    </td>
                    <td style={{ padding: "0.75rem", color: "#64748b" }}>
                      {a.groupName}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Modal>
    </AppShell>
  );
}
