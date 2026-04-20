"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import AppShell from "@/components/shell/AppShell";
import { getPersonDetail, addRestriction, PersonDetailDto } from "@/lib/api/people";
import { useSpaceStore } from "@/lib/store/spaceStore";

export default function PersonDetailPage() {
  const { personId } = useParams<{ personId: string }>();
  const { currentSpaceId } = useSpaceStore();
  const [person, setPerson] = useState<PersonDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [showRestriction, setShowRestriction] = useState(false);
  const [restrictionType, setRestrictionType] = useState("no_task_type_restriction");
  const [effectiveFrom, setEffectiveFrom] = useState("");
  const [effectiveUntil, setEffectiveUntil] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!currentSpaceId || !personId) return;
    getPersonDetail(currentSpaceId, personId)
      .then(setPerson)
      .finally(() => setLoading(false));
  }, [currentSpaceId, personId]);

  async function handleAddRestriction(e: React.FormEvent) {
    e.preventDefault();
    if (!currentSpaceId || !personId) return;
    setSaving(true);
    try {
      await addRestriction(
        currentSpaceId, personId, restrictionType,
        effectiveFrom, effectiveUntil || null, note || null, null
      );
      const updated = await getPersonDetail(currentSpaceId, personId);
      setPerson(updated);
      setShowRestriction(false);
      setMessage("Restriction added.");
    } catch {
      setMessage("Failed to add restriction.");
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <AppShell><p className="text-gray-400 text-sm">Loading...</p></AppShell>;
  if (!person) return <AppShell><p className="text-gray-500 text-sm">Person not found.</p></AppShell>;

  return (
    <AppShell>
      <div className="space-y-6 max-w-2xl">
        <div>
          <h1 className="text-xl font-semibold">{person.fullName}</h1>
          {person.displayName && <p className="text-gray-500 text-sm">{person.displayName}</p>}
        </div>

        {message && <p className="text-sm text-green-600">{message}</p>}

        {/* Roles & Groups */}
        <div className="grid grid-cols-2 gap-4">
          <div className="bg-white border border-gray-200 rounded-xl p-4">
            <h2 className="text-xs font-semibold text-gray-500 uppercase mb-2">Roles</h2>
            {person.roleNames.length === 0
              ? <p className="text-xs text-gray-400">No roles assigned</p>
              : person.roleNames.map(r => (
                <span key={r} className="inline-block bg-blue-50 text-blue-700 text-xs px-2 py-0.5 rounded-full me-1 mb-1">{r}</span>
              ))}
          </div>
          <div className="bg-white border border-gray-200 rounded-xl p-4">
            <h2 className="text-xs font-semibold text-gray-500 uppercase mb-2">Groups</h2>
            {person.groupNames.length === 0
              ? <p className="text-xs text-gray-400">No groups</p>
              : person.groupNames.map(g => (
                <span key={g} className="inline-block bg-gray-100 text-gray-700 text-xs px-2 py-0.5 rounded-full me-1 mb-1">{g}</span>
              ))}
          </div>
        </div>

        {/* Qualifications */}
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <h2 className="text-xs font-semibold text-gray-500 uppercase mb-2">Qualifications</h2>
          {person.qualifications.length === 0
            ? <p className="text-xs text-gray-400">None</p>
            : person.qualifications.map(q => (
              <span key={q} className="inline-block bg-green-50 text-green-700 text-xs px-2 py-0.5 rounded-full me-1 mb-1">{q}</span>
            ))}
        </div>

        {/* Restrictions */}
        <div className="bg-white border border-gray-200 rounded-xl p-4 space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="text-xs font-semibold text-gray-500 uppercase">Restrictions</h2>
            <button onClick={() => setShowRestriction(!showRestriction)}
              className="text-xs text-blue-600 hover:underline">+ Add</button>
          </div>

          {showRestriction && (
            <form onSubmit={handleAddRestriction} className="space-y-3 border-t pt-3">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-gray-500">Type</label>
                  <select value={restrictionType} onChange={e => setRestrictionType(e.target.value)}
                    className="w-full border rounded-lg px-3 py-2 text-sm mt-1">
                    <option value="no_task_type_restriction">No specific task</option>
                    <option value="no_night">No night shifts</option>
                    <option value="no_kitchen">No kitchen</option>
                    <option value="medical_leave">Medical leave</option>
                  </select>
                </div>
                <div>
                  <label className="text-xs text-gray-500">From</label>
                  <input type="date" value={effectiveFrom} onChange={e => setEffectiveFrom(e.target.value)}
                    required className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Until (optional)</label>
                  <input type="date" value={effectiveUntil} onChange={e => setEffectiveUntil(e.target.value)}
                    className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
                </div>
                <div>
                  <label className="text-xs text-gray-500">Note</label>
                  <input value={note} onChange={e => setNote(e.target.value)}
                    className="w-full border rounded-lg px-3 py-2 text-sm mt-1" placeholder="Optional note" />
                </div>
              </div>
              <button type="submit" disabled={saving}
                className="bg-blue-600 text-white text-xs px-3 py-1.5 rounded-lg hover:bg-blue-700 disabled:opacity-50">
                {saving ? "..." : "Save restriction"}
              </button>
            </form>
          )}

          {person.restrictions.length === 0
            ? <p className="text-xs text-gray-400">No restrictions</p>
            : person.restrictions.map(r => (
              <div key={r.id} className="text-sm border-t pt-2">
                <span className="font-medium">{r.restrictionType}</span>
                <span className="text-gray-400 text-xs ms-2">
                  {r.effectiveFrom} {r.effectiveUntil ? `→ ${r.effectiveUntil}` : "onwards"}
                </span>
                {r.operationalNote && <p className="text-xs text-gray-500 mt-0.5">{r.operationalNote}</p>}
              </div>
            ))}
        </div>
      </div>
    </AppShell>
  );
}
