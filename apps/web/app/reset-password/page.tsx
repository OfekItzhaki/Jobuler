"use client";

import { useState, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { resetPassword } from "@/lib/api/auth";

function ResetPasswordForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (newPassword !== confirmPassword) {
      setError("הסיסמאות אינן תואמות");
      return;
    }
    if (newPassword.length < 8) {
      setError("הסיסמה חייבת להכיל לפחות 8 תווים");
      return;
    }
    if (!token) {
      setError("קישור לא תקין — חסר טוקן");
      return;
    }

    setLoading(true);
    try {
      await resetPassword(token, newPassword);
      router.push("/login?reset=1");
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { error?: string; message?: string } }; message?: string };
      const msg = axiosErr?.response?.data?.error ?? axiosErr?.response?.data?.message ?? axiosErr?.message;
      if (msg?.includes("Invalid or expired")) {
        setError("הקישור אינו תקין או שפג תוקפו. בקש קישור חדש.");
      } else {
        setError(msg ?? "שגיאה באיפוס הסיסמה");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: "1rem" }} dir="rtl">
      <div>
        <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
          סיסמה חדשה
        </label>
        <input
          type="password"
          required
          value={newPassword}
          onChange={e => setNewPassword(e.target.value)}
          placeholder="לפחות 8 תווים"
          style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
        />
      </div>
      <div>
        <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
          אימות סיסמה
        </label>
        <input
          type="password"
          required
          value={confirmPassword}
          onChange={e => setConfirmPassword(e.target.value)}
          placeholder="הזן שוב את הסיסמה"
          style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
        />
      </div>
      {error && (
        <div style={{ background: "#fef2f2", border: "1px solid #fecaca", borderRadius: 10, padding: "0.625rem 0.875rem" }}>
          <p style={{ fontSize: "0.875rem", color: "#dc2626", margin: 0 }}>{error}</p>
        </div>
      )}
      <button
        type="submit"
        disabled={loading}
        style={{ width: "100%", background: loading ? "#93c5fd" : "#3b82f6", color: "white", border: "none", borderRadius: 10, padding: "0.75rem", fontSize: "0.875rem", fontWeight: 600, cursor: loading ? "not-allowed" : "pointer" }}
      >
        {loading ? "מאפס..." : "אפס סיסמה"}
      </button>
    </form>
  );
}

export default function ResetPasswordPage() {
  return (
    <main style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", background: "#f8fafc", padding: "1rem" }}>
      <div style={{ width: "100%", maxWidth: "380px" }}>
        <div style={{ display: "flex", justifyContent: "center", marginBottom: "2rem" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, background: "#3b82f6", display: "flex", alignItems: "center", justifyContent: "center" }}>
              <svg width="20" height="20" fill="none" viewBox="0 0 24 24" stroke="white" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <span style={{ fontSize: "1.5rem", fontWeight: 700, color: "#0f172a" }}>Jobuler</span>
          </div>
        </div>

        <div style={{ background: "white", borderRadius: 16, boxShadow: "0 4px 24px rgba(0,0,0,0.08)", border: "1px solid #e2e8f0", padding: "2rem" }}>
          <div style={{ marginBottom: "1.5rem" }}>
            <h1 style={{ fontSize: "1.25rem", fontWeight: 600, color: "#0f172a", margin: 0 }}>איפוס סיסמה</h1>
            <p style={{ fontSize: "0.875rem", color: "#64748b", marginTop: "0.25rem" }}>הזן סיסמה חדשה לחשבונך</p>
          </div>
          <Suspense fallback={<p style={{ color: "#64748b", fontSize: "0.875rem" }}>טוען...</p>}>
            <ResetPasswordForm />
          </Suspense>
          <p style={{ textAlign: "center", fontSize: "0.875rem", color: "#64748b", marginTop: "1.25rem" }}>
            <Link href="/login" style={{ color: "#3b82f6", fontWeight: 500, textDecoration: "none" }}>
              ← חזרה להתחברות
            </Link>
          </p>
        </div>
      </div>
    </main>
  );
}
