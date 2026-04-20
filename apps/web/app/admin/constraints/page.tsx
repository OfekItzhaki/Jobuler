"use client";

import { useEffect, useState } from "react";
import AppShell from "@/components/shell/AppShell";
import { getConstraints, createConstraint, ConstraintDto } from "@/lib/api/constraints";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useAuthStore } from "@/lib/store/authStore";
import AiConstraintParser from "@/components/admin/AiConstraintParser";
import { clsx } from "clsx";

const SEVERITY_COLORS: Record<string, string> = {
  hard: "bg-red-50 text-red-700",
  soft: "bg-blue-50 text-blue-700",
};

export default function ConstraintsPage() {
  const { currentSpaceId } = useSpaceStore();
  const { isAdminMode } = useAuthStore();
  const [constraints, setConstraints] = useState<ConstraintDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showManual, setShowManual] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Manual form state
  const [scopeType, setScopeType] = useState("space");
  const [severity, setSeverity] = useState("hard");
  const [ruleType, setRuleType] = useState("min_rest_hours");
  const [payload, setPayload] = useState('{"hours": 8}');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!currentSpaceId) { setLoading(false); return; }
    getConstraints(currentSpaceId).then(setConstraints).finally(() => setLoading(false));
  }, [currentSpaceId]);

  async function handleSave(
    st: string, sid: string | null, sev: string,
    rt: string, pl: string
  ) {
    if (!currentSpaceId) return;
    setSaving(true); setError(null);
    try {
      await createConstraint(currentSpaceId, st, sid, sev, rt, pl, null, null);
      const updated = await getConstraints(currentSpaceId);
      setConstraints(updated);
      setShowManual(false);
      setSuccess("Constraint saved.");
      setTimeout(() => setSuccess(null), 3000);
    } catch { setError("Failed to save constraint."); }
    finally { setSaving(false); }
  }

  async function handleManualSubmit(e: React.FormEvent) {
    e.preventDefault();
    await handleSave(scopeType, null, severity, ruleType, payload);
  }

  // Called when admin confirms an AI-parsed constraint
  async function handleAiConfirm(parsed: any) {
    if (!parsed.ruleType || !parsed.scopeType) return;
    await handleSave(
      parsed.scopeType, null,
      "hard",  // AI-parsed constraints default to hard — admin can change manually
      parsed.ruleType,
      parsed.rulePayloadJson ?? "{}"
    );
  }

  if (!isAdminMode) {
    return <AppShell><p className="text-gray-500 text-sm">Admin mode required.</p></AppShell>;
  }

  return (
    <AppShell>
      <div className="space-y-4 max-w-4xl">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-semibold">Constraints</h1>
          <button onClick={() => setShowManual(!showManual)}
            className="bg-blue-600 text-white text-sm px-3 py-1.5 rounded-lg hover:bg-blue-700">
            + Add manually
          </button>
        </div>

        {/* AI parser */}
        <AiConstraintParser onConfirm={handleAiConfirm} />

        {success && <p className="text-sm text-green-600">{success}</p>}
        {error && <p className="text-sm text-red-600">{error}</p>}

        {/* Manual form */}
        {showManual && (
          <form onSubmit={handleManualSubmit}
            className="bg-white border border-gray-200 rounded-xl p-4 space-y-3">
            <h2 className="text-sm font-semibold">New constraint</h2>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-gray-500">Scope type</label>
                <select value={scopeType} onChange={e => setScopeType(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1">
                  {["space", "person", "role", "group", "task_type"].map(s => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs text-gray-500">Severity</label>
                <select value={severity} onChange={e => setSeverity(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1">
                  <option value="hard">Hard</option>
                  <option value="soft">Soft</option>
                </select>
              </div>
              <div>
                <label className="text-xs text-gray-500">Rule type</label>
                <select value={ruleType} onChange={e => {
                  setRuleType(e.target.value);
                  const defaults: Record<string, string> = {
                    min_rest_hours: '{"hours": 8}',
                    max_kitchen_per_week: '{"max": 2, "task_type_name": "kitchen"}',
                    no_consecutive_burden: '{"burden_level": "disliked"}',
                    min_base_headcount: '{"min": 3, "window_hours": 24}',
                    no_task_type_restriction: '{"task_type_id": ""}',
                  };
                  setPayload(defaults[e.target.value] ?? "{}");
                }} className="w-full border rounded-lg px-3 py-2 text-sm mt-1">
                  <option value="min_rest_hours">min_rest_hours</option>
                  <option value="max_kitchen_per_week">max_kitchen_per_week</option>
                  <option value="no_consecutive_burden">no_consecutive_burden</option>
                  <option value="min_base_headcount">min_base_headcount</option>
                  <option value="no_task_type_restriction">no_task_type_restriction</option>
                </select>
              </div>
              <div>
                <label className="text-xs text-gray-500">Payload (JSON)</label>
                <input value={payload} onChange={e => setPayload(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1 font-mono" />
              </div>
            </div>
            <div className="flex gap-2">
              <button type="submit" disabled={saving}
                className="bg-blue-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50">
                {saving ? "..." : "Save"}
              </button>
              <button type="button" onClick={() => setShowManual(false)}
                className="text-sm text-gray-500 hover:underline">Cancel</button>
            </div>
          </form>
        )}

        {loading && <p className="text-gray-400 text-sm">Loading...</p>}

        <div className="overflow-x-auto rounded-lg border border-gray-200">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-gray-500 uppercase text-xs">
              <tr>
                <th className="px-4 py-3 text-start">Rule type</th>
                <th className="px-4 py-3 text-start">Scope</th>
                <th className="px-4 py-3 text-start">Severity</th>
                <th className="px-4 py-3 text-start">Payload</th>
                <th className="px-4 py-3 text-start">Active</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {constraints.map(c => (
                <tr key={c.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-xs">{c.ruleType}</td>
                  <td className="px-4 py-3 text-gray-500">{c.scopeType}</td>
                  <td className="px-4 py-3">
                    <span className={clsx("text-xs px-2 py-0.5 rounded-full",
                      SEVERITY_COLORS[c.severity] ?? "bg-gray-100")}>
                      {c.severity}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-500 max-w-xs truncate">
                    {c.rulePayloadJson}
                  </td>
                  <td className="px-4 py-3">
                    <span className={c.isActive ? "text-green-600 text-xs" : "text-gray-400 text-xs"}>
                      {c.isActive ? "Yes" : "No"}
                    </span>
                  </td>
                </tr>
              ))}
              {!loading && constraints.length === 0 && (
                <tr><td colSpan={5} className="px-4 py-6 text-center text-gray-400 text-sm">
                  No constraints yet.
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </AppShell>
  );
}
