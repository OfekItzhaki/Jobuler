"use client";

import { useEffect, useState } from "react";
import AppShell from "@/components/shell/AppShell";
import { apiClient } from "@/lib/api/client";
import { getPeople, PersonDto } from "@/lib/api/people";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useAuthStore } from "@/lib/store/authStore";

interface GroupTypeDto { id: string; name: string; }
interface GroupDto { id: string; groupTypeId: string; groupTypeName: string; name: string; memberCount: number; }
interface MemberDto { personId: string; fullName: string; displayName: string | null; }

export default function GroupsPage() {
  const { currentSpaceId } = useSpaceStore();
  const { isAdminMode } = useAuthStore();

  const [groupTypes, setGroupTypes] = useState<GroupTypeDto[]>([]);
  const [groups, setGroups] = useState<GroupDto[]>([]);
  const [people, setPeople] = useState<PersonDto[]>([]);
  const [selectedGroup, setSelectedGroup] = useState<GroupDto | null>(null);
  const [members, setMembers] = useState<MemberDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [newTypeName, setNewTypeName] = useState("");
  const [savingType, setSavingType] = useState(false);

  const [newGroupName, setNewGroupName] = useState("");
  const [newGroupTypeId, setNewGroupTypeId] = useState("");
  const [savingGroup, setSavingGroup] = useState(false);

  const [addPersonId, setAddPersonId] = useState("");
  const [addingMember, setAddingMember] = useState(false);

  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!currentSpaceId) { setLoading(false); return; }
    Promise.all([
      apiClient.get(`/spaces/${currentSpaceId}/group-types`).then(r => r.data),
      apiClient.get(`/spaces/${currentSpaceId}/groups`).then(r => r.data),
      getPeople(currentSpaceId),
    ]).then(([types, grps, ppl]) => {
      setGroupTypes(types); setGroups(grps); setPeople(ppl);
      if (types.length > 0) setNewGroupTypeId(types[0].id);
    }).finally(() => setLoading(false));
  }, [currentSpaceId]);

  async function loadMembers(group: GroupDto) {
    setSelectedGroup(group);
    const { data } = await apiClient.get(
      `/spaces/${currentSpaceId}/groups/${group.id}/members`);
    setMembers(data);
  }

  async function handleCreateType(e: React.FormEvent) {
    e.preventDefault();
    if (!currentSpaceId || !newTypeName.trim()) return;
    setSavingType(true);
    try {
      await apiClient.post(`/spaces/${currentSpaceId}/group-types`, { name: newTypeName, description: null });
      const { data } = await apiClient.get(`/spaces/${currentSpaceId}/group-types`);
      setGroupTypes(data);
      setNewTypeName("");
    } catch { setError("Failed to create group type."); }
    finally { setSavingType(false); }
  }

  async function handleCreateGroup(e: React.FormEvent) {
    e.preventDefault();
    if (!currentSpaceId || !newGroupName.trim() || !newGroupTypeId) return;
    setSavingGroup(true);
    try {
      await apiClient.post(`/spaces/${currentSpaceId}/groups`, {
        groupTypeId: newGroupTypeId, name: newGroupName, description: null,
      });
      const { data } = await apiClient.get(`/spaces/${currentSpaceId}/groups`);
      setGroups(data);
      setNewGroupName("");
    } catch { setError("Failed to create group."); }
    finally { setSavingGroup(false); }
  }

  async function handleAddMember(e: React.FormEvent) {
    e.preventDefault();
    if (!currentSpaceId || !selectedGroup || !addPersonId) return;
    setAddingMember(true);
    try {
      await apiClient.post(
        `/spaces/${currentSpaceId}/groups/${selectedGroup.id}/members`,
        { personId: addPersonId });
      await loadMembers(selectedGroup);
      setAddPersonId("");
    } catch { setError("Failed to add member."); }
    finally { setAddingMember(false); }
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

  const inputClass = "border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow";

  return (
    <AppShell>
      <div className="max-w-5xl space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Groups</h1>
          <p className="text-sm text-slate-500 mt-1">Organize people into groups and squads</p>
        </div>

        {error && (
          <div className="flex items-center gap-3 bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-700">
            <svg className="w-4 h-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            {error}
          </div>
        )}

        <div className="grid grid-cols-3 gap-6">
          {/* Left: group types + groups */}
          <div className="col-span-2 space-y-4">
            {/* Create group type */}
            <form onSubmit={handleCreateType} className="flex gap-2">
              <input
                value={newTypeName}
                onChange={e => setNewTypeName(e.target.value)}
                placeholder="New group type (e.g. Squad)"
                className={`flex-1 ${inputClass}`}
              />
              <button
                type="submit"
                disabled={savingType}
                className="bg-slate-700 hover:bg-slate-800 text-white text-sm font-medium px-4 py-2.5 rounded-xl disabled:opacity-50 transition-colors whitespace-nowrap"
              >
                {savingType ? "..." : "+ Type"}
              </button>
            </form>

            {/* Create group */}
            <form onSubmit={handleCreateGroup} className="flex gap-2">
              <select
                value={newGroupTypeId}
                onChange={e => setNewGroupTypeId(e.target.value)}
                className={inputClass}
              >
                {groupTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
              <input
                value={newGroupName}
                onChange={e => setNewGroupName(e.target.value)}
                placeholder="New group name"
                className={`flex-1 ${inputClass}`}
              />
              <button
                type="submit"
                disabled={savingGroup}
                className="bg-blue-500 hover:bg-blue-600 text-white text-sm font-medium px-4 py-2.5 rounded-xl disabled:opacity-50 transition-colors whitespace-nowrap"
              >
                {savingGroup ? "..." : "+ Group"}
              </button>
            </form>

            {/* Groups table */}
            {loading ? (
              <div className="flex items-center gap-3 text-slate-400 text-sm py-8">
                <svg className="animate-spin h-5 w-5 text-blue-400" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Loading...
              </div>
            ) : (
              <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/80">
                      <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Group</th>
                      <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Type</th>
                      <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider">Members</th>
                      <th className="px-4 py-3 text-start text-xs font-semibold text-slate-500 uppercase tracking-wider"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {groups.map(g => (
                      <tr
                        key={g.id}
                        className={`hover:bg-slate-50/60 transition-colors ${selectedGroup?.id === g.id ? "bg-blue-50/40" : ""}`}
                      >
                        <td className="px-4 py-3.5 font-medium text-slate-900">{g.name}</td>
                        <td className="px-4 py-3.5 text-slate-500">{g.groupTypeName}</td>
                        <td className="px-4 py-3.5">
                          <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-slate-100 text-slate-600 text-xs font-semibold">
                            {g.memberCount}
                          </span>
                        </td>
                        <td className="px-4 py-3.5">
                          <button
                            onClick={() => loadMembers(g)}
                            className="text-xs font-medium text-blue-600 hover:text-blue-700 transition-colors"
                          >
                            Manage →
                          </button>
                        </td>
                      </tr>
                    ))}
                    {groups.length === 0 && (
                      <tr>
                        <td colSpan={4} className="px-4 py-12 text-center text-slate-400 text-sm">
                          No groups yet.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Right: members panel */}
          <div>
            {selectedGroup ? (
              <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm space-y-4">
                <div>
                  <h2 className="text-sm font-semibold text-slate-900">{selectedGroup.name}</h2>
                  <p className="text-xs text-slate-500 mt-0.5">Members</p>
                </div>

                <form onSubmit={handleAddMember} className="flex gap-2">
                  <select
                    value={addPersonId}
                    onChange={e => setAddPersonId(e.target.value)}
                    className="flex-1 border border-slate-200 rounded-xl px-2.5 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  >
                    <option value="">Add person...</option>
                    {people
                      .filter(p => !members.some(m => m.personId === p.id))
                      .map(p => (
                        <option key={p.id} value={p.id}>
                          {p.displayName ?? p.fullName}
                        </option>
                      ))}
                  </select>
                  <button
                    type="submit"
                    disabled={addingMember || !addPersonId}
                    className="bg-emerald-500 hover:bg-emerald-600 text-white text-xs font-medium px-3 py-2 rounded-xl disabled:opacity-50 transition-colors"
                  >
                    {addingMember ? "..." : "Add"}
                  </button>
                </form>

                <div className="space-y-1.5">
                  {members.map(m => (
                    <div
                      key={m.personId}
                      className="flex items-center gap-2.5 px-3 py-2.5 bg-slate-50 border border-slate-100 rounded-xl"
                    >
                      <div className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center shrink-0">
                        <span className="text-xs font-semibold text-blue-600">
                          {(m.displayName ?? m.fullName).charAt(0).toUpperCase()}
                        </span>
                      </div>
                      <span className="text-sm text-slate-700">{m.displayName ?? m.fullName}</span>
                    </div>
                  ))}
                  {members.length === 0 && (
                    <p className="text-xs text-slate-400 text-center py-4">No members yet.</p>
                  )}
                </div>
              </div>
            ) : (
              <div className="flex flex-col items-center justify-center py-12 text-center bg-white border border-slate-200 rounded-2xl">
                <svg className="w-8 h-8 text-slate-200 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <p className="text-xs text-slate-400">Select a group to manage members.</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
