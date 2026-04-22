"use client";

import { useEffect, useState } from "react";
import AppShell from "@/components/shell/AppShell";
import {
  getTaskTypes, createTaskType, getTaskSlots, createTaskSlot,
  TaskTypeDto, TaskSlotDto,
} from "@/lib/api/tasks";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useAuthStore } from "@/lib/store/authStore";
import { clsx } from "clsx";

const BURDEN_STYLES: Record<string, string> = {
  Favorable: "bg-emerald-50 text-emerald-700 border-emerald-200",
  Neutral:   "bg-slate-100 text-slate-600 border-slate-200",
  Disliked:  "bg-amber-50 text-amber-700 border-amber-200",
  Hated:     "bg-red-50 text-red-700 border-red-200",
};

const BURDEN_DOTS: Record<string, string> = {
  Favorable: "bg-emerald-500",
  Neutral:   "bg-slate-400",
  Disliked:  "bg-amber-500",
  Hated:     "bg-red-500",
};

export default function TasksPage() {
  const { currentSpaceId } = useSpaceStore();
  const { isAdminMode } = useAuthStore();

  const [taskTypes, setTaskTypes] = useState<TaskTypeDto[]>([]);
  const [slots, setSlots] = useState<TaskSlotDto[]>([]);
  const [tab, setTab] = useState<"types" | "slots">("types");
  const [loading, setLoading] = useState(true);

  const [showTypeForm, setShowTypeForm] = useState(false);
  const [typeName, setTypeName] = useState("");
  const [typeDesc, setTypeDesc] = useState("");
  const [burden, setBurden] = useState("Neutral");
  const [priority, setPriority] = useState(5);
  const [allowsOverlap, setAllowsOverlap] = useState(false);
  const [savingType, setSavingType] = useState(false);

  const [showSlotForm, setShowSlotForm] = useState(false);
  const [slotTypeId, setSlotTypeId] = useState("");
  const [slotStart, setSlotStart] = useState("");
  const [slotEnd, setSlotEnd] = useState("");
  const [headcount, setHeadcount] = useState(1);
  const [slotPriority, setSlotPriority] = useState(5);
  const [location, setLocation] = useState("");
  const [savingSlot, setSavingSlot] = useState(false);

  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!currentSpaceId) { setLoading(false); return; }
    Promise.all([
      getTaskTypes(currentSpaceId),
      getTaskSlots(currentSpaceId),
    ]).then(([types, s]) => { setTaskTypes(types); setSlots(s); })
      .finally(() => setLoading(false));
  }, [currentSpaceId]);

  async function handleCreateType(e: React.FormEvent) {
    e.preventDefault();
    if (!currentSpaceId) return;
    setSavingType(true); setError(null);
    try {
      await createTaskType(currentSpaceId, typeName, typeDesc || null, burden, priority, allowsOverlap);
      const updated = await getTaskTypes(currentSpaceId);
      setTaskTypes(updated);
      setTypeName(""); setTypeDesc(""); setBurden("Neutral"); setPriority(5);
      setShowTypeForm(false);
    } catch { setError("Failed to create task type."); }
    finally { setSavingType(false); }
  }

  async function handleCreateSlot(e: React.FormEvent) {
    e.preventDefault();
    if (!currentSpaceId || !slotTypeId) return;
    setSavingSlot(true); setError(null);
    try {
      await createTaskSlot(currentSpaceId, slotTypeId,
        new Date(slotStart).toISOString(),
        new Date(slotEnd).toISOString(),
        headcount, slotPriority, location || null);
      const updated = await getTaskSlots(currentSpaceId);
      setSlots(updated);
      setSlotStart(""); setSlotEnd(""); setHeadcount(1); setLocation("");
      setShowSlotForm(false);
    } catch { setError("Failed to create task slot."); }
    finally { setSavingSlot(false); }
  }

  if (!isAdminMode) {
    return (
      <AppShell>
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <svg className="w-12 h-12 text-slate-200 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
          <p className="text-slate-500 text-sm">Admin mode required.</p>
        </div>
      </AppShell>
    );
  }

  const inputClass = "w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow";

  return (
    <AppShell>
      <div className="max-w-4xl space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">Tasks</h1>
            <p className="text-sm text-slate-500 mt-1">Manage task types and scheduled slots</p>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => { setTab("types"); setShowTypeForm(true); setShowSlotForm(false); }}
              className="flex items-center gap-2 bg-blue-500 hover:bg-blue-600 text-white text-sm font-medium px-3.5 py-2.5 rounded-xl shadow-sm shadow-blue-500/20 transition-all"
            >
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
              </svg>
              Task type
            </button>
            <button
              onClick={() => { setTab("slots"); setShowSlotForm(true); setShowTypeForm(false); }}
              className="flex items-center gap-2 bg-emerald-500 hover:bg-emerald-600 text-white text-sm font-medium px-3.5 py-2.5 rounded-xl shadow-sm shadow-emerald-500/20 transition-all"
            >
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
              </svg>
              Task slot
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex gap-1 border-b border-slate-200">
          {(["types", "slots"] as const).map(t => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={clsx(
                "px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors",
                tab === t
                  ? "border-blue-500 text-blue-600"
                  : "border-transparent text-slate-500 hover:text-slate-700"
              )}
            >
              {t === "types" ? "Task Types" : "Task Slots"}
            </button>
          ))}
        </div>

        {error && (
          <div className="flex items-center gap-2 bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-700">
            <svg className="w-4 h-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            {error}
          </div>
        )}

        {/* Task type form */}
        {tab === "types" && showTypeForm && (
          <form onSubmit={handleCreateType}
            className="bg-white border border-slate-200 rounded-2xl p-5 space-y-4 shadow-sm">
            <h2 className="text-sm font-semibold text-slate-900">New task type</h2>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Name *</label>
                <input value={typeName} onChange={e => setTypeName(e.target.value)} required
                  className={inputClass} placeholder="e.g. Guard Post 1" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Burden level</label>
                <select value={burden} onChange={e => setBurden(e.target.value)} className={inputClass}>
                  {["Favorable", "Neutral", "Disliked", "Hated"].map(b => (
                    <option key={b} value={b}>{b}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Description</label>
                <input value={typeDesc} onChange={e => setTypeDesc(e.target.value)}
                  className={inputClass} placeholder="Optional" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Default priority (1–10)</label>
                <input type="number" min={1} max={10} value={priority}
                  onChange={e => setPriority(Number(e.target.value))} className={inputClass} />
              </div>
            </div>
            <label className="flex items-center gap-2.5 text-sm text-slate-700 cursor-pointer">
              <input type="checkbox" checked={allowsOverlap}
                onChange={e => setAllowsOverlap(e.target.checked)}
                className="w-4 h-4 rounded border-slate-300 text-blue-500 focus:ring-blue-500" />
              Allows overlap
            </label>
            <div className="flex gap-2">
              <button type="submit" disabled={savingType}
                className="bg-blue-500 hover:bg-blue-600 text-white text-sm font-medium px-4 py-2.5 rounded-xl disabled:opacity-50 transition-colors">
                {savingType ? "Saving..." : "Save"}
              </button>
              <button type="button" onClick={() => setShowTypeForm(false)}
                className="text-sm text-slate-500 hover:text-slate-700 px-3 transition-colors">Cancel</button>
            </div>
          </form>
        )}

        {/* Task slot form */}
        {tab === "slots" && showSlotForm && (
          <form onSubmit={handleCreateSlot}
            className="bg-white border border-slate-200 rounded-2xl p-5 space-y-4 shadow-sm">
            <h2 className="text-sm font-semibold text-slate-900">New task slot</h2>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Task type *</label>
                <select value={slotTypeId} onChange={e => setSlotTypeId(e.target.value)} required className={inputClass}>
                  <option value="">Select type...</option>
                  {taskTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Headcount</label>
                <input type="number" min={1} value={headcount}
                  onChange={e => setHeadcount(Number(e.target.value))} className={inputClass} />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Starts at *</label>
                <input type="datetime-local" value={slotStart}
                  onChange={e => setSlotStart(e.target.value)} required className={inputClass} />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Ends at *</label>
                <input type="datetime-local" value={slotEnd}
                  onChange={e => setSlotEnd(e.target.value)} required className={inputClass} />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Location</label>
                <input value={location} onChange={e => setLocation(e.target.value)}
                  className={inputClass} placeholder="Optional" />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-500 mb-1.5">Priority (1–10)</label>
                <input type="number" min={1} max={10} value={slotPriority}
                  onChange={e => setSlotPriority(Number(e.target.value))} className={inputClass} />
              </div>
            </div>
            <div className="flex gap-2">
              <button type="submit" disabled={savingSlot}
                className="bg-emerald-500 hover:bg-emerald-600 text-white text-sm font-medium px-4 py-2.5 rounded-xl disabled:opacity-50 transition-colors">
                {savingSlot ? "Saving..." : "Save"}
              </button>
              <button type="button" onClick={() => setShowSlotForm(false)}
                className="text-sm text-slate-500 hover:text-slate-700 px-3 transition-colors">Cancel</button>
            </div>
          </form>
        )}

        {loading && (
          <div className="flex items-center gap-3 text-slate-400 text-sm py-8">
            <svg className="animate-spin h-5 w-5 text-blue-400" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            Loading...
          </div>
        )}

        {/* Task types table */}
        {tab === "types" && !loading && (
          <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50/80">
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Name</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Burden</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Priority</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Overlap</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {taskTypes.map(t => (
                  <tr key={t.id} className="hover:bg-slate-50/60 transition-colors">
                    <td className="px-4 py-3.5 font-medium text-slate-900">{t.name}</td>
                    <td className="px-4 py-3.5">
                      <span className={clsx(
                        "inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium border",
                        BURDEN_STYLES[t.burdenLevel] ?? "bg-slate-100 text-slate-600 border-slate-200"
                      )}>
                        <span className={clsx("w-1.5 h-1.5 rounded-full", BURDEN_DOTS[t.burdenLevel] ?? "bg-slate-400")} />
                        {t.burdenLevel}
                      </span>
                    </td>
                    <td className="px-4 py-3.5 text-slate-500">{t.defaultPriority}</td>
                    <td className="px-4 py-3.5">
                      <span className={clsx(
                        "text-xs font-medium",
                        t.allowsOverlap ? "text-emerald-600" : "text-slate-400"
                      )}>
                        {t.allowsOverlap ? "Yes" : "No"}
                      </span>
                    </td>
                  </tr>
                ))}
                {taskTypes.length === 0 && (
                  <tr>
                    <td colSpan={4} className="px-4 py-12 text-center text-slate-400 text-sm">
                      No task types yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Task slots table */}
        {tab === "slots" && !loading && (
          <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50/80">
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Task type</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Starts</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Ends</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Headcount</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {slots.map(s => (
                  <tr key={s.id} className="hover:bg-slate-50/60 transition-colors">
                    <td className="px-4 py-3.5 font-medium text-slate-900">{s.taskTypeName}</td>
                    <td className="px-4 py-3.5 text-slate-500 tabular-nums text-xs">
                      {new Date(s.startsAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3.5 text-slate-500 tabular-nums text-xs">
                      {new Date(s.endsAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3.5 text-slate-500">{s.requiredHeadcount}</td>
                    <td className="px-4 py-3.5 text-slate-500">{s.status}</td>
                  </tr>
                ))}
                {slots.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-12 text-center text-slate-400 text-sm">
                      No task slots yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </AppShell>
  );
}
