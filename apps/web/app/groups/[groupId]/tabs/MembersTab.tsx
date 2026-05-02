"use client";

import { useState } from "react";
import Modal from "@/components/Modal";
import ImageUpload from "@/components/ImageUpload";
import type { GroupMemberDto, GroupRoleDto } from "@/lib/api/groups";

interface Props {
  isAdmin: boolean;
  /** True only when the current user is the group owner — can edit member roles */
  isOwner: boolean;
  members: GroupMemberDto[];
  membersLoading: boolean;
  membersError: string | null;
  membersSearch: string;
  removeErrors: Record<string, string>;
  groupRoles: GroupRoleDto[];
  onSearchChange: (v: string) => void;
  onSelectMember: (m: GroupMemberDto) => void;
  onRemoveMember: (id: string) => void;
  onOpenAddMember: () => void;
  onOpenInvite: (id: string) => void;
  onUpdateMemberRole: (personId: string, roleId: string | null) => Promise<void>;
}

export default function MembersTab({
  isAdmin, isOwner, members, membersLoading, membersError, membersSearch, removeErrors,
  groupRoles, onSearchChange, onSelectMember, onRemoveMember, onOpenAddMember,
  onOpenInvite, onUpdateMemberRole,
}: Props) {
  const [editingRoleFor, setEditingRoleFor] = useState<string | null>(null);
  const [roleEditValue, setRoleEditValue] = useState<string>("");
  const [roleSaving, setRoleSaving] = useState(false);
  const [roleErrors, setRoleErrors] = useState<Record<string, string>>({});
  const [confirmRemove, setConfirmRemove] = useState<string | null>(null);

  const filtered = members.filter(m =>
    !membersSearch ||
    m.fullName.toLowerCase().includes(membersSearch.toLowerCase()) ||
    (m.displayName ?? "").toLowerCase().includes(membersSearch.toLowerCase())
  );

  async function handleSaveRole(personId: string) {
    setRoleSaving(true);
    setRoleErrors(prev => ({ ...prev, [personId]: "" }));
    try {
      await onUpdateMemberRole(personId, roleEditValue || null);
      setEditingRoleFor(null);
    } catch {
      setRoleErrors(prev => ({ ...prev, [personId]: "שגיאה בעדכון תפקיד" }));
    } finally {
      setRoleSaving(false);
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div className="relative flex-1 max-w-xs">
          <input
            type="text"
            value={membersSearch}
            onChange={e => onSearchChange(e.target.value)}
            placeholder="חיפוש חברים..."
            className="w-full border border-slate-200 rounded-xl px-3.5 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 pr-9"
          />
          <svg className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        {isAdmin && (
          <button onClick={onOpenAddMember} className="flex items-center gap-1.5 text-sm font-medium text-blue-600 border border-blue-200 bg-blue-50 hover:bg-blue-100 px-3 py-2 rounded-xl transition-colors">
            + הוסף חבר
          </button>
        )}
      </div>

      {membersLoading && <p className="text-sm text-slate-400 py-8">טוען חברים...</p>}
      {membersError && <p className="text-sm text-red-600">{membersError}</p>}

      {!membersLoading && filtered.length === 0 && (
        <div className="flex flex-col items-center justify-center py-16 text-center bg-white rounded-xl border border-slate-200">
          <svg className="w-10 h-10 text-slate-200 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          <p className="text-slate-400 text-sm">אין חברים בקבוצה</p>
        </div>
      )}

      <div className="space-y-2">
        {filtered.map(m => (
          <div key={m.personId} className="bg-white border border-slate-200 rounded-xl px-4 py-3 hover:border-slate-300 transition-colors">
            <div className="flex items-center gap-3">
              {/* Avatar */}
              <div className="w-9 h-9 rounded-full bg-blue-500 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
                {m.fullName.charAt(0).toUpperCase()}
              </div>

              {/* Name + role badge */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <p className="text-sm font-medium text-slate-900 truncate">{m.fullName}</p>
                  {m.isOwner && (
                    <span className="text-xs px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700 border border-amber-200 flex-shrink-0">
                      בעלים
                    </span>
                  )}
                  {!m.isOwner && m.roleName && (
                    <span className="text-xs px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200 flex-shrink-0">
                      {m.roleName}
                    </span>
                  )}
                </div>
                {m.displayName && m.displayName !== m.fullName && (
                  <p className="text-xs text-slate-400 truncate">{m.displayName}</p>
                )}
                {m.phoneNumber && <p className="text-xs text-slate-400 tabular-nums" dir="ltr">{m.phoneNumber}</p>}
              </div>

              {/* Actions */}
              <div className="flex items-center gap-2 flex-shrink-0">
                <button onClick={() => onSelectMember(m)} className="text-xs text-blue-600 hover:underline">פרטים</button>
                {isAdmin && !m.isOwner && (
                  <>
                    {isOwner && (
                      <button
                        onClick={() => {
                          setEditingRoleFor(m.personId);
                          setRoleEditValue(m.roleId ?? "");
                          setRoleErrors(prev => ({ ...prev, [m.personId]: "" }));
                        }}
                        className="text-xs text-slate-500 hover:text-slate-700 border border-slate-200 px-2 py-1 rounded-lg hover:bg-slate-50 transition-colors"
                      >
                        תפקיד
                      </button>
                    )}
                    <button onClick={() => onOpenInvite(m.personId)} className="text-xs text-slate-500 hover:text-slate-700 border border-slate-200 px-2 py-1 rounded-lg hover:bg-slate-50 transition-colors">הזמן</button>
                    {confirmRemove === m.personId ? (
                      <>
                        <span className="text-xs text-slate-600">הסרה קבועה?</span>
                        <button
                          onClick={() => { setConfirmRemove(null); onRemoveMember(m.personId); }}
                          className="text-xs text-white bg-red-500 hover:bg-red-600 px-2 py-1 rounded-lg transition-colors"
                        >
                          אישור
                        </button>
                        <button
                          onClick={() => setConfirmRemove(null)}
                          className="text-xs text-slate-500 border border-slate-200 px-2 py-1 rounded-lg hover:bg-slate-50 transition-colors"
                        >
                          ביטול
                        </button>
                      </>
                    ) : (
                      <button
                        onClick={() => setConfirmRemove(m.personId)}
                        className="text-xs text-red-500 hover:text-red-700 border border-red-100 px-2 py-1 rounded-lg hover:bg-red-50 transition-colors"
                      >
                        הסר
                      </button>
                    )}
                  </>
                )}
              </div>
            </div>

            {/* Inline role editor — owner only */}
            {editingRoleFor === m.personId && (
              <div className="mt-3 pt-3 border-t border-slate-100 flex items-center gap-2 flex-wrap">
                <select
                  value={roleEditValue}
                  onChange={e => setRoleEditValue(e.target.value)}
                  className="flex-1 min-w-0 border border-slate-200 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">ללא תפקיד</option>
                  {groupRoles
                    .filter(r => r.isActive)
                    .map(r => (
                      <option key={r.id} value={r.id}>
                        {r.name}{r.isDefault ? " (ברירת מחדל)" : ""}
                      </option>
                    ))}
                </select>
                <button
                  onClick={() => handleSaveRole(m.personId)}
                  disabled={roleSaving}
                  className="bg-blue-500 hover:bg-blue-600 text-white text-xs font-medium px-3 py-1.5 rounded-lg disabled:opacity-50 transition-colors"
                >
                  {roleSaving ? "שומר..." : "שמור"}
                </button>
                <button
                  onClick={() => setEditingRoleFor(null)}
                  className="text-xs text-slate-500 border border-slate-200 px-3 py-1.5 rounded-lg hover:bg-slate-50 transition-colors"
                >
                  ביטול
                </button>
                {roleErrors[m.personId] && (
                  <p className="w-full text-xs text-red-600">{roleErrors[m.personId]}</p>
                )}
              </div>
            )}

            {removeErrors[m.personId] && (
              <p className="text-xs text-red-600 mt-1">{removeErrors[m.personId]}</p>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

// ── Member profile modal ──────────────────────────────────────────────────────
interface MemberProfileModalProps {
  member: GroupMemberDto;
  isAdmin: boolean;
  editForm: { fullName: string; displayName: string; phoneNumber: string; profileImageUrl: string; birthday: string } | null;
  saving: boolean;
  error: string | null;
  onClose: () => void;
  onStartEdit: () => void;
  onCancelEdit: () => void;
  onChangeForm: (f: { fullName: string; displayName: string; phoneNumber: string; profileImageUrl: string; birthday: string }) => void;
  onSave: (personId: string) => void;
}

export function MemberProfileModal({ member, isAdmin, editForm, saving, error, onClose, onStartEdit, onCancelEdit, onChangeForm, onSave }: MemberProfileModalProps) {
  return (
    <Modal title="פרטי חבר" open onClose={onClose} maxWidth={480}>
      {editForm ? (
        <div className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">שם מלא</label>
            <input type="text" value={editForm.fullName} onChange={e => onChangeForm({ ...editForm, fullName: e.target.value })} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">שם תצוגה</label>
            <input type="text" value={editForm.displayName} onChange={e => onChangeForm({ ...editForm, displayName: e.target.value })} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">טלפון</label>
            <input type="tel" value={editForm.phoneNumber} onChange={e => onChangeForm({ ...editForm, phoneNumber: e.target.value })} dir="ltr" className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">תמונת פרופיל</label>
            <ImageUpload value={editForm.profileImageUrl || null} onChange={url => onChangeForm({ ...editForm, profileImageUrl: url })} shape="circle" size={64} label="העלה תמונה" disabled={saving} />
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wide mb-1">תאריך לידה</label>
            <input type="date" value={editForm.birthday} onChange={e => onChangeForm({ ...editForm, birthday: e.target.value })} className="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          {error && <p className="text-sm text-red-600">{error}</p>}
          <div className="flex gap-2 pt-1">
            <button onClick={() => onSave(member.personId)} disabled={saving} className="bg-blue-500 hover:bg-blue-600 text-white text-sm font-medium px-4 py-2.5 rounded-xl disabled:opacity-50 transition-colors">
              {saving ? "שומר..." : "שמור"}
            </button>
            <button onClick={onCancelEdit} className="text-sm text-slate-500 border border-slate-200 px-4 py-2.5 rounded-xl hover:bg-slate-50 transition-colors">ביטול</button>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          <div className="flex items-center gap-4">
            <div className="w-16 h-16 rounded-full bg-blue-500 flex items-center justify-center text-white text-2xl font-bold flex-shrink-0">
              {(member.displayName ?? member.fullName).charAt(0).toUpperCase()}
            </div>
            <div>
              <p className="text-lg font-semibold text-slate-900">{member.displayName ?? member.fullName}</p>
              {member.roleName && (
                <p className="text-sm text-slate-500">{member.roleName}</p>
              )}
              {member.phoneNumber && <p className="text-sm text-slate-500 tabular-nums" dir="ltr">{member.phoneNumber}</p>}
            </div>
          </div>
          {isAdmin && (
            <button onClick={onStartEdit} className="text-sm text-blue-600 border border-blue-200 bg-blue-50 hover:bg-blue-100 px-4 py-2 rounded-xl transition-colors">
              ערוך פרטים
            </button>
          )}
        </div>
      )}
    </Modal>
  );
}
