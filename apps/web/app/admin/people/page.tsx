"use client";

import { useEffect, useState } from "react";
import AppShell from "@/components/shell/AppShell";
import { getPeople, createPerson, PersonDto } from "@/lib/api/people";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useAuthStore } from "@/lib/store/authStore";
import Link from "next/link";

export default function PeoplePage() {
  const { currentSpaceId } = useSpaceStore();
  const { isAdminMode } = useAuthStore();
  const [people, setPeople] = useState<PersonDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [fullName, setFullName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!currentSpaceId) { setLoading(false); return; }
    getPeople(currentSpaceId).then(setPeople).finally(() => setLoading(false));
  }, [currentSpaceId]);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!currentSpaceId || !fullName.trim()) return;
    setCreating(true);
    setError(null);
    try {
      await createPerson(currentSpaceId, fullName.trim(), displayName.trim() || null);
      const updated = await getPeople(currentSpaceId);
      setPeople(updated);
      setFullName(""); setDisplayName(""); setShowCreate(false);
    } catch {
      setError("Failed to create person.");
    } finally {
      setCreating(false);
    }
  }

  if (!isAdminMode) {
    return <AppShell><p className="text-gray-500 text-sm">Admin mode required.</p></AppShell>;
  }

  return (
    <AppShell>
      <div className="space-y-4 max-w-3xl">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-semibold">People</h1>
          <button
            onClick={() => setShowCreate(!showCreate)}
            className="bg-blue-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            + Add person
          </button>
        </div>

        {showCreate && (
          <form onSubmit={handleCreate}
            className="bg-white border border-gray-200 rounded-xl p-4 space-y-3">
            <h2 className="text-sm font-semibold">New person</h2>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-gray-500">Full name *</label>
                <input value={fullName} onChange={e => setFullName(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Full name" required />
              </div>
              <div>
                <label className="text-xs text-gray-500">Display name</label>
                <input value={displayName} onChange={e => setDisplayName(e.target.value)}
                  className="w-full border rounded-lg px-3 py-2 text-sm mt-1 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Nickname (optional)" />
              </div>
            </div>
            {error && <p className="text-xs text-red-600">{error}</p>}
            <div className="flex gap-2">
              <button type="submit" disabled={creating}
                className="bg-green-600 text-white text-sm px-4 py-2 rounded-lg hover:bg-green-700 disabled:opacity-50">
                {creating ? "..." : "Save"}
              </button>
              <button type="button" onClick={() => setShowCreate(false)}
                className="text-sm text-gray-500 hover:underline">Cancel</button>
            </div>
          </form>
        )}

        {loading && <p className="text-gray-400 text-sm">Loading...</p>}

        <div className="overflow-x-auto rounded-lg border border-gray-200">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-gray-500 uppercase text-xs">
              <tr>
                <th className="px-4 py-3 text-start">Name</th>
                <th className="px-4 py-3 text-start">Display name</th>
                <th className="px-4 py-3 text-start">Status</th>
                <th className="px-4 py-3 text-start"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {people.map(p => (
                <tr key={p.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium">{p.fullName}</td>
                  <td className="px-4 py-3 text-gray-500">{p.displayName ?? "—"}</td>
                  <td className="px-4 py-3">
                    <span className={p.isActive
                      ? "text-green-600 text-xs" : "text-gray-400 text-xs"}>
                      {p.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <Link href={`/admin/people/${p.id}`}
                      className="text-xs text-blue-600 hover:underline">
                      View
                    </Link>
                  </td>
                </tr>
              ))}
              {!loading && people.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-gray-400 text-sm">
                    No people yet. Add someone above.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </AppShell>
  );
}
