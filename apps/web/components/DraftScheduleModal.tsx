"use client";

import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/client";
import { useDateFormat } from "@/lib/hooks/useDateFormat";

interface Assignment {
  personName: string;
  taskTypeName: string;
  startsAt: string;
  endsAt: string;
}

interface Props {
  open: boolean;
  onClose: () => void;
  spaceId: string;
  draftVersionId: string;
  isAdmin: boolean;
  onPublish: () => Promise<void>;
  onDiscard: () => Promise<void>;
  onRunAgain: () => void;
}

export default function DraftScheduleModal({
  open, onClose, spaceId, draftVersionId,
  isAdmin, onPublish, onDiscard, onRunAgain,
}: Props) {
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [loading, setLoading] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [discarding, setDiscarding] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showDiscardConfirm, setShowDiscardConfirm] = useState(false);
  const { fDateLong, fTime } = useDateFormat();

  useEffect(() => {
    if (!open || !draftVersionId) return;
    setLoading(true);
    setError(null);
    apiClient.get(`/spaces/${spaceId}/schedule-versions/${draftVersionId}`)
      .then(r => {
        const detail = r.data;
        setAssignments((detail.assignments ?? []).map((a: any) => ({
          personName: a.personName,
          taskTypeName: a.taskTypeName,
          startsAt: a.slotStartsAt,
          endsAt: a.slotEndsAt,
        })));
      })
      .catch(() => setError("שגיאה בטעינת הטיוטה"))
      .finally(() => setLoading(false));
  }, [open, draftVersionId, spaceId]);

  async function handlePublish() {
    setPublishing(true);
    setError(null);
    try {
      await onPublish();
      onClose();
    } catch (e: any) {
      setError(e?.response?.data?.error ?? "שגיאה בפרסום");
    } finally {
      setPublishing(false);
    }
  }

  async function handleDiscard() {
    setDiscarding(true);
    setError(null);
    try {
      await onDiscard();
      onClose();
    } catch (e: any) {
      setError(e?.response?.data?.error ?? "שגיאה בביטול");
    } finally {
      setDiscarding(false);
      setShowDiscardConfirm(false);
    }
  }

  if (!open) return null;

  // Group assignments by date
  const byDate: Record<string, Assignment[]> = {};
  for (const a of assignments) {
    const date = a.startsAt?.split("T")[0] ?? "unknown";
    if (!byDate[date]) byDate[date] = [];
    byDate[date].push(a);
  }
  const sortedDates = Object.keys(byDate).sort();

  return (
    <div
      style={{
        position: "fixed", inset: 0, zIndex: 60,
        background: "rgba(0,0,0,0.5)",
        display: "flex", alignItems: "center", justifyContent: "center",
        padding: "1rem",
      }}
      onClick={onClose}
    >
      <div
        style={{
          background: "white", borderRadius: 20,
          boxShadow: "0 24px 64px rgba(0,0,0,0.18)",
          width: "100%", maxWidth: 640,
          maxHeight: "85vh",
          display: "flex", flexDirection: "column",
          direction: "rtl",
        }}
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div style={{
          padding: "1.25rem 1.5rem",
          borderBottom: "1px solid #e2e8f0",
          display: "flex", alignItems: "center", justifyContent: "space-between",
          flexShrink: 0,
        }}>
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <span style={{
              background: "#fef3c7", color: "#92400e", border: "1px solid #fde68a",
              borderRadius: 999, padding: "2px 10px", fontSize: 12, fontWeight: 700,
            }}>טיוטה</span>
            <h2 style={{ fontSize: "1rem", fontWeight: 700, color: "#0f172a", margin: 0 }}>
              תצוגה מקדימה של הסידור
            </h2>
          </div>
          <button
            onClick={onClose}
            style={{ background: "none", border: "none", cursor: "pointer", color: "#94a3b8", padding: 4 }}
          >
            <svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Body */}
        <div style={{ flex: 1, overflowY: "auto", padding: "1rem 1.5rem" }}>
          {loading ? (
            <div style={{ display: "flex", justifyContent: "center", padding: "3rem 0", color: "#94a3b8" }}>
              <svg className="animate-spin" width="24" height="24" fill="none" viewBox="0 0 24 24">
                <circle style={{ opacity: 0.25 }} cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path style={{ opacity: 0.75 }} fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
            </div>
          ) : error ? (
            <p style={{ color: "#dc2626", fontSize: 14, textAlign: "center", padding: "2rem 0" }}>{error}</p>
          ) : assignments.length === 0 ? (
            <div style={{ textAlign: "center", padding: "2rem 0" }}>
              <p style={{ color: "#94a3b8", fontSize: 14, marginBottom: 12 }}>
                הסידור ריק — לא נמצאו שיבוצים בטיוטה זו.
              </p>
              <p style={{ color: "#64748b", fontSize: 13, marginBottom: 16 }}>
                ייתכן שהסולבר לא הצליח לבנות סידור עם האילוצים הנוכחיים.
              </p>
              {isAdmin && (
                <button
                  onClick={() => { onClose(); onRunAgain(); }}
                  style={{
                    background: "#3b82f6", color: "white", border: "none",
                    borderRadius: 10, padding: "9px 20px", fontSize: 13,
                    fontWeight: 600, cursor: "pointer",
                  }}
                >
                  🔄 הרץ שוב
                </button>
              )}
            </div>
          ) : (
            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
              {sortedDates.map(date => (
                <div key={date}>
                  <p style={{
                    fontSize: 12, fontWeight: 700, color: "#64748b",
                    textTransform: "uppercase", letterSpacing: "0.05em",
                    marginBottom: 8,
                  }}>
                    {fDateLong(date + "T00:00:00")}
                  </p>
                  <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                    {byDate[date].map((a, i) => (
                      <div key={i} style={{
                        display: "flex", alignItems: "center", justifyContent: "space-between",
                        background: "#f8fafc", border: "1px solid #e2e8f0",
                        borderRadius: 10, padding: "8px 14px",
                      }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                          <div style={{
                            width: 32, height: 32, borderRadius: "50%",
                            background: "linear-gradient(135deg, #3b82f6, #6366f1)",
                            display: "flex", alignItems: "center", justifyContent: "center",
                            color: "white", fontSize: 13, fontWeight: 700, flexShrink: 0,
                          }}>
                            {a.personName.charAt(0)}
                          </div>
                          <div>
                            <p style={{ fontSize: 13, fontWeight: 600, color: "#0f172a", margin: 0 }}>{a.personName}</p>
                            <p style={{ fontSize: 11, color: "#64748b", margin: 0 }}>{a.taskTypeName}</p>
                          </div>
                        </div>
                        <p style={{ fontSize: 12, color: "#64748b", margin: 0 }}>
                          {fTime(a.startsAt)}
                          {" – "}
                          {fTime(a.endsAt)}
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        {isAdmin && (
          <div style={{
            padding: "1rem 1.5rem",
            borderTop: "1px solid #e2e8f0",
            display: "flex", alignItems: "center", gap: 10,
            flexShrink: 0,
          }}>
            {showDiscardConfirm ? (
              <>
                <p style={{ fontSize: 13, color: "#dc2626", flex: 1, margin: 0 }}>
                  האם לבטל את הטיוטה? פעולה זו אינה הפיכה.
                </p>
                <button
                  onClick={handleDiscard}
                  disabled={discarding}
                  style={{
                    background: "#ef4444", color: "white", border: "none",
                    borderRadius: 10, padding: "8px 16px", fontSize: 13,
                    fontWeight: 600, cursor: "pointer",
                  }}
                >
                  {discarding ? "מבטל..." : "כן, בטל"}
                </button>
                <button
                  onClick={() => setShowDiscardConfirm(false)}
                  style={{
                    background: "none", border: "1px solid #e2e8f0", borderRadius: 10,
                    padding: "8px 14px", fontSize: 13, color: "#64748b", cursor: "pointer",
                  }}
                >
                  חזור
                </button>
              </>
            ) : (
              <>
                <button
                  onClick={handlePublish}
                  disabled={publishing || discarding || loading}
                  style={{
                    background: "#10b981", color: "white", border: "none",
                    borderRadius: 10, padding: "9px 20px", fontSize: 13,
                    fontWeight: 600, cursor: "pointer", opacity: publishing ? 0.6 : 1,
                  }}
                >
                  {publishing ? "מפרסם..." : "✓ פרסם סידור"}
                </button>
                <button
                  onClick={() => { onClose(); onRunAgain(); }}
                  disabled={publishing || discarding}
                  style={{
                    background: "#3b82f6", color: "white", border: "none",
                    borderRadius: 10, padding: "9px 20px", fontSize: 13,
                    fontWeight: 600, cursor: "pointer",
                  }}
                >
                  🔄 הרץ שוב
                </button>
                <button
                  onClick={() => setShowDiscardConfirm(true)}
                  disabled={publishing || discarding}
                  style={{
                    background: "none", border: "1px solid #fca5a5", color: "#dc2626",
                    borderRadius: 10, padding: "9px 16px", fontSize: 13,
                    cursor: "pointer", marginRight: "auto",
                  }}
                >
                  ✕ בטל טיוטה
                </button>
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
