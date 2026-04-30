"use client";

import Modal from "@/components/Modal";
import ConstraintPayloadEditor from "@/components/ConstraintPayloadEditor";
import type { ConstraintDto } from "@/lib/api/constraints";
import { SEVERITY_STYLES, SEVERITY_DOTS } from "../types";

const RULE_TYPES = [
  { value: "min_rest_hours", label: "מינימום שעות מנוחה" },
  { value: "max_kitchen_per_week", label: "מקסימום מטבח בשבוע" },
  { value: "no_consecutive_burden", label: "ללא עומס רצוף" },
  { value: "min_base_headcount", label: "מינימום כוח אדם בבסיס" },
  { value: "no_task_type_restriction", label: "הגבלת סוג משימה" },
  { value: "emergency_person_bypass", label: "🚨 חריגת חירום — אדם" },
  { value: "emergency_slot_bypass", label: "🚨 חריגת חירום — משמרת" },
  { value: "emergency_space_bypass", label: "🚨 חריגת חירום — כל המרחב" },
];

function formatPayload(ruleType: string, json: string): string {
  try {
    const p = JSON.parse(json);
    switch (ruleType) {
      case "min_rest_hours": return `${p.hours ?? 8} שעות מנוחה`;
      case "max_kitchen_per_week": return `מקסימום ${p.max ?? 2} מטבח בשבוע`;
      case "no_consecutive_burden": return `ללא ${p.burden_level ?? "hated"} רצוף`;
      case "min_base_headcount": return `מינימום ${p.min ?? 3} אנשים בכל ${p.window_hours ?? 24} שעות`;
      case "no_task_type_restriction": return `הגבלה על משימה: ${p.task_type_id ?? "—"}`;
      default: return json;
    }
  } catch { return json; }
}

interface Props {
  isAdmin: boolean;
  constraints: ConstraintDto[];
  constraintsLoading: boolean;
  constraintDeleteErrors: Record<string, string>;
  showConstraintForm: boolean;
  newConstraintRuleType: string;
  newConstraintSeverity: string;
  newConstraintPayload: string;
  newConstraintFrom: string;
  newConstraintUntil: string;
  constraintSaving: boolean;
  constraintError: string | null;
  editingConstraintId: string | null;
  editConstraintPayload: string;
  editConstraintFrom: string;
  editConstraintUntil: string;
  editConstraintSeverity: string;
  editConstraintSaving: boolean;
  editConstraintError: string | null;
  onOpenCreate: () => void;
  onCloseCreate: () => void;
  onRuleTypeChange: (v: string) => void;
  onSeverityChange: (v: string) => void;
  onPayloadChange: (v: string) => void;
  onFromChange: (v: string) => void;
  onUntilChange: (v: string) => void;
  onCreateSubmit: (e: React.FormEvent) => void;
  onDeleteConstraint: (id: string) => void;
  onStartEdit: (c: ConstraintDto) => void;
  onCloseEdit: () => void;
  onEditPayloadChange: (v: string) => void;
  onEditFromChange: (v: string) => void;
  onEditUntilChange: (v: string) => void;
  onEditSeverityChange: (v: string) => void;
  onUpdateConstraint: (id: string) => void;
}

export default function ConstraintsTab({
  isAdmin, constraints, constraintsLoading, constraintDeleteErrors,
  showConstraintForm, newConstraintRuleType, newConstraintSeverity, newConstraintPayload, newConstraintFrom, newConstraintUntil, constraintSaving, constraintError,
  editingConstraintId, editConstraintPayload, editConstraintFrom, editConstraintUntil, editConstraintSeverity, editConstraintSaving, editConstraintError,
  onOpenCreate, onCloseCreate, onRuleTypeChange, onSeverityChange, onPayloadChange, onFromChange, onUntilChange, onCreateSubmit,
  onDeleteConstraint, onStartEdit, onCloseEdit, onEditPayloadChange, onEditFromChange, onEditUntilChange, onEditSeverityChange, onUpdateConstraint,
}: Props) {
  const editingConstraint = constraints.find(c => c.id === editingConstraintId) ?? null;

  return (
    <div className="space-y-4">
      {isAdmin && (
        <button onClick={onOpenCreate} className="flex items-center gap-2 text-sm font-medium text-blue-600 border border-blue-200 bg-blue-50 hover:bg-blue-100 px-4 py-2.5 rounded-xl transition-colors">
          + אילוץ חדש
        </button>
      )}

      {constraintsLoading && <p className="text-sm text-slate-400 py-8">טוען אילוצים...</p>}

      {!constraintsLoading && constraints.length === 0 && (
        <div className="flex flex-col items-center justify-center py-16 text-center bg-white rounded-xl border border-slate-200">
          <p className="text-slate-400 text-sm">אין אילוצים מוגדרים</p>
        </div>
      )}

      <div className="space-y-2">
        {constraints.map(c => {
          // severity may arrive as a string ("Hard"/"Soft") or integer (0=Hard, 1=Soft)
          // depending on API version — normalise to lowercase string
          const sevRaw = c.severity;
          const sev = typeof sevRaw === "number"
            ? (sevRaw === 0 ? "hard" : "soft")
            : (String(sevRaw).toLowerCase());
          return (
            <div key={c.id} className="bg-white border border-slate-200 rounded-xl p-4">
              <div className="flex items-start justify-between gap-2">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium border ${SEVERITY_STYLES[sev] ?? "bg-slate-100 text-slate-500 border-slate-200"}`}>
                      <span className={`w-1.5 h-1.5 rounded-full ${SEVERITY_DOTS[sev] ?? "bg-slate-400"}`} />
                      {sev === "hard" ? "קשה" : sev === "emergency" ? "🚨 חירום" : "רך"}
                    </span>
                    <span className="text-sm font-medium text-slate-700">{RULE_TYPES.find(r => r.value === c.ruleType)?.label ?? c.ruleType}</span>
                  </div>
                  <p className="text-xs text-slate-500">{formatPayload(c.ruleType, c.rulePayloadJson)}</p>
                </div>
                {isAdmin && (
                  <div className="flex gap-1.5 flex-shrink-0">
                    <button onClick={() => onStartEdit(c)} className="text-xs text-slate-500 hover:text-slate-700 border border-slate-200 px-2 py-1 rounded-lg hover:bg-slate-50 transition-colors">ערוך</button>
                    <button onClick={() => onDeleteConstraint(c.id)} className="text-xs text-red-500 hover:text-red-700 border border-red-100 px-2 py-1 rounded-lg hover:bg-red-50 transition-colors">מחק</button>
                  </div>
                )}
              </div>
              {constraintDeleteErrors[c.id] && <p className="text-xs text-red-600 mt-1">{constraintDeleteErrors[c.id]}</p>}
            </div>
          );
        })}
      </div>

      {/* Create modal */}
      <Modal title="אילוץ חדש" open={showConstraintForm} onClose={onCloseCreate} maxWidth={520}>
        <form onSubmit={onCreateSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-slate-500 mb-1">סוג אילוץ</label>
              <select value={newConstraintRuleType} onChange={e => onRuleTypeChange(e.target.value)} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {RULE_TYPES.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs text-slate-500 mb-1">חומרה</label>
              <select value={newConstraintSeverity} onChange={e => onSeverityChange(e.target.value)} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="hard">קשה (Hard)</option>
                <option value="soft">רך (Soft)</option>
                <option value="emergency">🚨 חירום (Emergency)</option>
              </select>
            </div>
          </div>
          <ConstraintPayloadEditor ruleType={newConstraintRuleType} value={newConstraintPayload} onChange={onPayloadChange} />
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-slate-500 mb-1">בתוקף מ <span className="text-slate-400">(אופציונלי)</span></label>
              <input type="date" value={newConstraintFrom} onChange={e => onFromChange(e.target.value)} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs text-slate-500 mb-1">בתוקף עד <span className="text-slate-400">(אופציונלי)</span></label>
              <input type="date" value={newConstraintUntil} onChange={e => onUntilChange(e.target.value)} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          {constraintError && <p className="text-sm text-red-600">{constraintError}</p>}
          <div className="flex gap-2">
            <button type="submit" disabled={constraintSaving} className="bg-blue-500 hover:bg-blue-600 text-white text-sm font-medium px-4 py-2.5 rounded-xl disabled:opacity-50 transition-colors">
              {constraintSaving ? "שומר..." : "צור"}
            </button>
            <button type="button" onClick={onCloseCreate} className="text-sm text-slate-500 border border-slate-200 px-4 py-2.5 rounded-xl hover:bg-slate-50 transition-colors">ביטול</button>
          </div>
        </form>
      </Modal>

      {/* Edit modal */}
      {editingConstraint && (
        <Modal title="עריכת אילוץ" open={!!editingConstraintId} onClose={onCloseEdit} maxWidth={520}>
          <div className="space-y-4">
            <div>
              <label className="block text-xs text-slate-500 mb-1">חומרה</label>
              <select value={editConstraintSeverity} onChange={e => onEditSeverityChange(e.target.value)} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="hard">קשה (Hard)</option>
                <option value="soft">רך (Soft)</option>
                <option value="emergency">🚨 חירום (Emergency)</option>
              </select>
            </div>
            <ConstraintPayloadEditor ruleType={editingConstraint.ruleType} value={editConstraintPayload} onChange={onEditPayloadChange} />
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs text-slate-500 mb-1">בתוקף מ</label>
                <input type="date" value={editConstraintFrom} onChange={e => onEditFromChange(e.target.value)} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-xs text-slate-500 mb-1">בתוקף עד</label>
                <input type="date" value={editConstraintUntil} onChange={e => onEditUntilChange(e.target.value)} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
            {editConstraintError && <p className="text-sm text-red-600">{editConstraintError}</p>}
            <div className="flex gap-2">
              <button onClick={() => onUpdateConstraint(editingConstraint.id)} disabled={editConstraintSaving} className="bg-blue-500 hover:bg-blue-600 text-white text-sm font-medium px-4 py-2.5 rounded-xl disabled:opacity-50 transition-colors">
                {editConstraintSaving ? "שומר..." : "שמור"}
              </button>
              <button onClick={onCloseEdit} className="text-sm text-slate-500 border border-slate-200 px-4 py-2.5 rounded-xl hover:bg-slate-50 transition-colors">ביטול</button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}
