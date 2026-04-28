"use client";

import { useEffect, useState } from "react";
import { getBurdenStats, BurdenStats } from "@/lib/api/schedule";
import type { GroupMemberDto } from "@/lib/api/groups";
import StatsLeaderboard from "@/app/admin/stats/_components/StatsLeaderboard";
import StatsPeopleTable from "@/app/admin/stats/_components/StatsPeopleTable";

interface Props {
  groupId: string;
  spaceId: string;
  members: GroupMemberDto[];
}

export default function StatsTab({ spaceId, members }: Props) {
  const [stats, setStats] = useState<BurdenStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const memberIds = new Set(members.map(m => m.personId));

  useEffect(() => {
    setLoading(true);
    setError(null);
    getBurdenStats(spaceId)
      .then(data => {
        // Filter to only members of this group
        const filtered: BurdenStats = {
          ...data,
          people: data.people.filter(p => memberIds.has(p.personId)),
          mostAssignments: data.mostAssignments.filter(e => memberIds.has(e.personId)),
          mostHatedTasks: data.mostHatedTasks.filter(e => memberIds.has(e.personId)),
          highestBurdenScore: data.highestBurdenScore.filter(e => memberIds.has(e.personId)),
          bestBurdenBalance: data.bestBurdenBalance.filter(e => memberIds.has(e.personId)),
          mostKitchenDuty: data.mostKitchenDuty.filter(e => memberIds.has(e.personId)),
          mostNightMissions: data.mostNightMissions.filter(e => memberIds.has(e.personId)),
          mostFavorableTasks: data.mostFavorableTasks.filter(e => memberIds.has(e.personId)),
          worstBurdenBalance: data.worstBurdenBalance.filter(e => memberIds.has(e.personId)),
          mostConsecutiveBurden: data.mostConsecutiveBurden.filter(e => memberIds.has(e.personId)),
        };
        setStats(filtered);
      })
      .catch(() => setError("שגיאה בטעינת הסטטיסטיקות"))
      .finally(() => setLoading(false));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [spaceId]);

  if (loading) {
    return (
      <div className="flex items-center gap-3 py-12 text-slate-400 text-sm">
        <svg className="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        טוען סטטיסטיקות...
      </div>
    );
  }

  if (error) {
    return <p className="text-sm text-red-600 py-8">{error}</p>;
  }

  if (!stats || stats.people.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 bg-white rounded-xl border border-slate-200">
        <p className="text-slate-400 text-sm">אין נתוני סטטיסטיקה לקבוצה זו</p>
      </div>
    );
  }

  return (
    <div className="space-y-5" dir="rtl">
      {/* Summary */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[
          { label: "חברים פעילים", value: stats.people.length },
          { label: "שיבוצים סה״כ", value: stats.people.reduce((s, p) => s + p.totalAssignmentsAllTime, 0) },
          { label: "ממוצע לאדם", value: stats.people.length > 0 ? Math.round(stats.people.reduce((s, p) => s + p.totalAssignmentsAllTime, 0) / stats.people.length) : 0 },
          { label: "משימות שנואות", value: stats.people.reduce((s, p) => s + p.hatedTasksAllTime, 0) },
        ].map(c => (
          <div key={c.label} className="bg-white border border-slate-200 rounded-xl p-4">
            <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-1">{c.label}</p>
            <p className="text-2xl font-bold text-slate-900">{c.value}</p>
          </div>
        ))}
      </div>

      {/* Leaderboards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <StatsLeaderboard title="הכי הרבה שיבוצים" entries={stats.mostAssignments} />
        <StatsLeaderboard title="הכי הרבה משימות שנואות" entries={stats.mostHatedTasks} valueColor="#dc2626" />
        <StatsLeaderboard title="ציון עומס גבוה ביותר" entries={stats.highestBurdenScore} valueColor="#d97706" />
        <StatsLeaderboard title="איזון עומס הטוב ביותר" entries={stats.bestBurdenBalance} valueColor="#16a34a" />
      </div>

      {/* People table */}
      <div>
        <h2 className="text-sm font-semibold text-slate-700 mb-3">פירוט לפי אדם</h2>
        <StatsPeopleTable people={stats.people} />
      </div>
    </div>
  );
}
