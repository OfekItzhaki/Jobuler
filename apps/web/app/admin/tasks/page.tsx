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

const BURDEN_COLORS: Record<string, string> = {
  Favorable: "bg-green-50 text-green-700",
  Neutral:   "bg-gray-100 text-gray-600",
  Disliked:  "bg-amber-50 text-amber-700",
  Hated:     "bg-red-50 text-red-700",
};

export default function TasksPage() {
  const { currentSpaceId } = useSpaceStore();
  const { isAdminMode } = useAuthStore();

  const [taskTypes, setTaskTypes] = useState<TaskTypeDto[]>([]);
  const [slots, setSlots] = useState<TaskSlotDto[]>([]);
  const [tab, setTab] = useState<"types" | "slots">("types");
  const [loading, setLoading] = useState(true);

  // Create task type form
  const [showTypeForm, setShowTypeForm] = useState(false);
  const [typeName, setTypeName] = useState("");
  const [typeDesc, setTypeDesc] = useState("");
  const [burden, setBurden] = useState("Neutral");
  const [priority, setPriority] = useState(5);
  const [allowsOverlap, setAllowsOverlap] = useState(false);
  const [savingType, setSavingType] = useState(false);

  // Create slot form
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
    return <AppShell><p className="text-gray-500 text-sm">Admin mode required.</p></AppShell>;
  }

  return (
    <AppShell>
      <div className="space-y-4 max-w-4xl">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-semibold">Tasks</h1>
          <div className="flex gap-2">
            <button onClick={() => { setTab("types"); setShowTypeForm(true); setShowSlotForm(false); }}
              className="bg-blue-600 text-white text-sm px-3 py-1.5 rounded-lg hover:bg-blue-700">
              + Task type
            </button>
            <button onClick={() => { setTab("slots"); setShowSlotForm(true); setShowTypeForm(false); }}
              className="bg-green-600 text-white text-sm px-3 py-1.5 rounded-lg hover:bg-green-700">
              + Task slot
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex gap-1 border-b">
          {(["types", "slots"] as const).map(t => (
            <button key={t} onClick={() => setTab(t)}
              className={clsx("px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors",
                tab === t ? "border-blue-600 text-blue-600" : "border-transparent text-gray-500 hover:text-gray-700")}>
              {t === "types" ? "Task Types" : "Task Slots"}
            </button>
          ))}
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}

        {/* Task type form */}
        {tab === "types" && showTypeForm && (
          <form onSubmit={handleCreateType}
            className="bg-white border border-gray-200 rounded-xl p-4 space-y-3">
            <h2 className="text-sm font-semibold">New task type</h2>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-gray-500">Name *</label>
                <input value={typeName} onChange={e => setTypeName(e.target.value)} required
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" placeholder="e.g. Guard Post 1" />
              </div>
              <div>
                <label className="text-xs text-gray-500">Burden level</label>
                <select value={burden} onChange={e => setBurden(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1">
                  {["Favorable", "Neutral", "Disliked", "Hated"].map(b => (
                    <option key={b} value={b}>{b}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs text-gray-500">Description</label>
                <input value={typeDesc} onChange={e => setTypeDesc(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" placeholder="Optional" />
              </div>
              <div>
                <label className="text-xs text-gray-500">Default priority (1–10)</label>
                <input type="number" min={1} max={10} value={priority}
                  onChange={e => setPriority(Number(e.target.value))}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
              </div>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={allowsOverlap}
                onChange={e => setAllowsOverlap(e.target.checked)} />
              Allows overlap
            </label>
            <div className="flex gap-2">
              <button type="submit" disabled={savingType}
                className="bg-blue-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50">
                {savingType ? "..." : "Save"}
              </button>
              <button type="button" onClick={() => setShowTypeForm(false)}
                className="text-sm text-gray-500 hover:underline">Cancel</button>
            </div>
          </form>
        )}

        {/* Task slot form */}
        {tab === "slots" && showSlotForm && (
          <form onSubmit={handleCreateSlot}
            className="bg-white border border-gray-200 rounded-xl p-4 space-y-3">
            <h2 className="text-sm font-semibold">New task slot</h2>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-gray-500">Task type *</label>
                <select value={slotTypeId} onChange={e => setSlotTypeId(e.target.value)} required
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1">
                  <option value="">Select type...</option>
                  {taskTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
              </div>
              <div>
                <label className="text-xs text-gray-500">Headcount</label>
                <input type="number" min={1} value={headcount}
                  onChange={e => setHeadcount(Number(e.target.value))}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
              </div>
              <div>
                <label className="text-xs text-gray-500">Starts at *</label>
                <input type="datetime-local" value={slotStart}
                  onChange={e => setSlotStart(e.target.value)} required
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
              </div>
              <div>
                <label className="text-xs text-gray-500">Ends at *</label>
                <input type="datetime-local" value={slotEnd}
                  onChange={e => setSlotEnd(e.target.value)} required
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
              </div>
              <div>
                <label className="text-xs text-gray-500">Location</label>
                <input value={location} onChange={e => setLocation(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" placeholder="Optional" />
              </div>
              <div>
                <label className="text-xs text-gray-500">Priority (1–10)</label>
                <input type="number" min={1} max={10} value={slotPriority}
                  onChange={e => setSlotPriority(Number(e.target.value))}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
              </div>
            </div>
            <div className="flex gap-2">
              <button type="submit" disabled={savingSlot}
                className="bg-green-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-green-700 disabled:opacity-50">
                {savingSlot ? "..." : "Save"}
              </button>
              <button type="button" onClick={() => setShowSlotForm(false)}
                className="text-sm text-gray-500 hover:underline">Cancel</button>
            </div>
          </form>
        )}

        {loading && <p className="text-gray-400 text-sm">Loading...</p>}

        {/* Task types list */}
        {tab === "types" && !loading && (
          <div className="overflow-x-auto rounded-lg border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-gray-500 uppercase text-xs">
                <tr>
                  <th className="px-4 py-3 text-start">Name</th>
                  <th className="px-4 py-3 text-start">Burden</th>
                  <th className="px-4 py-3 text-start">Priority</th>
                  <th className="px-4 py-3 text-start">Overlap</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {taskTypes.map(t => (
                  <tr key={t.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{t.name}</td>
                    <td className="px-4 py-3">
                      <span className={clsx("text-xs px-2 py-0.5 rounded-full",
                        BURDEN_COLORS[t.burdenLevel] ?? "bg-gray-100")}>
                        {t.burdenLevel}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-500">{t.defaultPriority}</td>
                    <td className="px-4 py-3 text-gray-500">{t.allowsOverlap ? "Yes" : "No"}</td>
                  </tr>
                ))}
                {taskTypes.length === 0 && (
                  <tr><td colSpan={4} className="px-4 py-6 text-center text-gray-400 text-sm">
                    No task types yet.
                  </td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Task slots list */}
        {tab === "slots" && !loading && (
          <div className="overflow-x-auto rounded-lg border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-gray-500 uppercase text-xs">
                <tr>
                  <th className="px-4 py-3 text-start">Task type</th>
                  <th className="px-4 py-3 text-start">Starts</th>
                  <th className="px-4 py-3 text-start">Ends</th>
                  <th className="px-4 py-3 text-start">Headcount</th>
                  <th className="px-4 py-3 text-start">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {slots.map(s => (
                  <tr key={s.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{s.taskTypeName}</td>
                    <td className="px-4 py-3 text-gray-500 tabular-nums">
                      {new Date(s.startsAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3 text-gray-500 tabular-nums">
                      {new Date(s.endsAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3 text-gray-500">{s.requiredHeadcount}</td>
                    <td className="px-4 py-3 text-gray-500">{s.status}</td>
                  </tr>
                ))}
                {slots.length === 0 && (
                  <tr><td colSpan={5} className="px-4 py-6 text-center text-gray-400 text-sm">
                    No task slots yet.
                  </td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </AppShell>
  );
}
