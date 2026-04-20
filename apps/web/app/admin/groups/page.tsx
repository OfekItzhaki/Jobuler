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

  // Create group type
  const [newTypeName, setNewTypeName] = useState("");
  const [savingType, setSavingType] = useState(false);

  // Create group
  const [newGroupName, setNewGroupName] = useState("");
  const [newGroupTypeId, setNewGroupTypeId] = useState("");
  const [savingGroup, setSavingGroup] = useState(false);

  // Add member
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
    return <AppShell><p className="text-gray-500 text-sm">Admin mode required.</p></AppShell>;
  }

  return (
    <AppShell>
      <div className="space-y-6 max-w-5xl">
        <h1 className="text-xl font-semibold">Groups</h1>
        {error && <p className="text-sm text-red-600">{error}</p>}

        <div className="grid grid-cols-3 gap-6">
          {/* Left: group types + groups */}
          <div className="col-span-2 space-y-4">
            {/* Create group type */}
            <form onSubmit={handleCreateType} className="flex gap-2">
              <input value={newTypeName} onChange={e => setNewTypeName(e.target.value)}
                placeholder="New group type (e.g. Squad)"
                className="flex-1 border rounded-lg px-3 py-2 text-sm" />
              <button type="submit" disabled={savingType}
                className="bg-gray-600 text-white text-sm px-3 py-2 rounded-lg hover:bg-gray-700 disabled:opacity-50">
                {savingType ? "..." : "+ Type"}
              </button>
            </form>

            {/* Create group */}
            <form onSubmit={handleCreateGroup} className="flex gap-2">
              <select value={newGroupTypeId} onChange={e => setNewGroupTypeId(e.target.value)}
                className="border rounded-lg px-3 py-2 text-sm">
                {groupTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
              <input value={newGroupName} onChange={e => setNewGroupName(e.target.value)}
                placeholder="New group name"
                className="flex-1 border rounded-lg px-3 py-2 text-sm" />
              <button type="submit" disabled={savingGroup}
                className="bg-blue-600 text-white text-sm px-3 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50">
                {savingGroup ? "..." : "+ Group"}
              </button>
            </form>

            {/* Groups list */}
            {loading ? <p className="text-gray-400 text-sm">Loading...</p> : (
              <div className="overflow-x-auto rounded-lg border border-gray-200">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 text-gray-500 uppercase text-xs">
                    <tr>
                      <th className="px-4 py-3 text-start">Group</th>
                      <th className="px-4 py-3 text-start">Type</th>
                      <th className="px-4 py-3 text-start">Members</th>
                      <th className="px-4 py-3 text-start"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {groups.map(g => (
                      <tr key={g.id} className="hover:bg-gray-50">
                        <td className="px-4 py-3 font-medium">{g.name}</td>
                        <td className="px-4 py-3 text-gray-500">{g.groupTypeName}</td>
                        <td className="px-4 py-3 text-gray-500">{g.memberCount}</td>
                        <td className="px-4 py-3">
                          <button onClick={() => loadMembers(g)}
                            className="text-xs text-blue-600 hover:underline">
                            Manage
                          </button>
                        </td>
                      </tr>
                    ))}
                    {groups.length === 0 && (
                      <tr><td colSpan={4} className="px-4 py-6 text-center text-gray-400 text-sm">
                        No groups yet.
                      </td></tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Right: members panel */}
          <div className="space-y-3">
            {selectedGroup ? (
              <>
                <h2 className="text-sm font-semibold">{selectedGroup.name} — Members</h2>

                <form onSubmit={handleAddMember} className="flex gap-2">
                  <select value={addPersonId} onChange={e => setAddPersonId(e.target.value)}
                    className="flex-1 border rounded-lg px-2 py-1.5 text-sm">
                    <option value="">Add person...</option>
                    {people
                      .filter(p => !members.some(m => m.personId === p.id))
                      .map(p => (
                        <option key={p.id} value={p.id}>
                          {p.displayName ?? p.fullName}
                        </option>
                      ))}
                  </select>
                  <button type="submit" disabled={addingMember || !addPersonId}
                    className="bg-green-600 text-white text-xs px-2 py-1.5 rounded-lg hover:bg-green-700 disabled:opacity-50">
                    {addingMember ? "..." : "Add"}
                  </button>
                </form>

                <div className="space-y-1">
                  {members.map(m => (
                    <div key={m.personId}
                      className="text-sm px-3 py-2 bg-white border border-gray-200 rounded-lg">
                      {m.displayName ?? m.fullName}
                    </div>
                  ))}
                  {members.length === 0 && (
                    <p className="text-xs text-gray-400">No members yet.</p>
                  )}
                </div>
              </>
            ) : (
              <p className="text-xs text-gray-400">Select a group to manage members.</p>
            )}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
