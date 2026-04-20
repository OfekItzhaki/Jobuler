"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { getMySpaces, createSpace, SpaceDto } from "@/lib/api/spaces";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useAuthStore } from "@/lib/store/authStore";

export default function SpacesPage() {
  const t = useTranslations();
  const router = useRouter();
  const { setCurrentSpace } = useSpaceStore();
  const { preferredLocale } = useAuthStore();

  const [spaces, setSpaces] = useState<SpaceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMySpaces()
      .then(s => {
        setSpaces(s);
        // Auto-select if only one space
        if (s.length === 1) {
          setCurrentSpace(s[0].id, s[0].name);
          router.push("/schedule/today");
        }
      })
      .finally(() => setLoading(false));
  }, []);

  function handleSelect(space: SpaceDto) {
    setCurrentSpace(space.id, space.name);
    router.push("/schedule/today");
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!newName.trim()) return;
    setCreating(true);
    setError(null);
    try {
      await createSpace(newName.trim(), null, preferredLocale);
      const updated = await getMySpaces();
      setSpaces(updated);
      setNewName("");
      setShowCreate(false);
    } catch {
      setError("Failed to create space.");
    } finally {
      setCreating(false);
    }
  }

  return (
    <main className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
      <div className="w-full max-w-md space-y-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold">{t("app.name")}</h1>
          <p className="text-sm text-gray-500 mt-1">Select a space to continue</p>
        </div>

        {loading && <p className="text-center text-gray-400 text-sm">Loading...</p>}

        {!loading && spaces.length === 0 && (
          <p className="text-center text-gray-500 text-sm">
            You don't belong to any spaces yet. Create one below.
          </p>
        )}

        <div className="space-y-2">
          {spaces.map(space => (
            <button
              key={space.id}
              onClick={() => handleSelect(space)}
              className="w-full text-start bg-white border border-gray-200 rounded-xl px-4 py-3 hover:border-blue-400 hover:bg-blue-50 transition-colors"
            >
              <div className="font-medium">{space.name}</div>
              {space.description && (
                <div className="text-xs text-gray-400 mt-0.5">{space.description}</div>
              )}
            </button>
          ))}
        </div>

        {!showCreate ? (
          <button
            onClick={() => setShowCreate(true)}
            className="w-full text-sm text-blue-600 hover:underline text-center"
          >
            + Create new space
          </button>
        ) : (
          <form onSubmit={handleCreate} className="space-y-3 bg-white border border-gray-200 rounded-xl p-4">
            <h2 className="text-sm font-semibold">New space</h2>
            <input
              type="text"
              value={newName}
              onChange={e => setNewName(e.target.value)}
              placeholder="Space name"
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              autoFocus
            />
            {error && <p className="text-xs text-red-600">{error}</p>}
            <div className="flex gap-2">
              <button
                type="submit"
                disabled={creating || !newName.trim()}
                className="flex-1 bg-blue-600 text-white text-sm py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                {creating ? "..." : "Create"}
              </button>
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="text-sm text-gray-500 hover:underline px-3"
              >
                Cancel
              </button>
            </div>
          </form>
        )}
      </div>
    </main>
  );
}
