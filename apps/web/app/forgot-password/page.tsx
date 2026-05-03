"use client";

import { useState } from "react";
import Link from "next/link";
import { forgotPassword } from "@/lib/api/auth";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    await forgotPassword(email);
    setSubmitted(true);
    setLoading(false);
  }

  return (
    <main style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", background: "#f8fafc", padding: "1rem" }}>
      <div style={{ width: "100%", maxWidth: "380px" }}>
        {/* Logo */}
        <div style={{ display: "flex", justifyContent: "center", marginBottom: "2rem" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, background: "#3b82f6", display: "flex", alignItems: "center", justifyContent: "center" }}>
              <svg width="20" height="20" fill="none" viewBox="0 0 32 32" stroke="white" strokeWidth={2.5}>
                <path strokeLinecap="round" d="M9 12 C9 9.5 11 8 15 8 C19 8 21 9.5 21 12 C21 14.5 19 15 15 15 C11 15 9 16.5 9 19 C9 21.5 11 23 15 23 C19 23 21 21.5 21 19" />
              </svg>
            </div>
            <span style={{ fontSize: "1.5rem", fontWeight: 700, color: "#0f172a" }}>Shifter</span>
          </div>
        </div>

        <div style={{ background: "white", borderRadius: 16, boxShadow: "0 4px 24px rgba(0,0,0,0.08)", border: "1px solid #e2e8f0", padding: "2rem" }}>
          <div style={{ marginBottom: "1.5rem" }}>
            <h1 style={{ fontSize: "1.25rem", fontWeight: 600, color: "#0f172a", margin: 0 }}>שכחת סיסמה?</h1>
            <p style={{ fontSize: "0.875rem", color: "#64748b", marginTop: "0.25rem" }}>הזן את כתובת האימייל שלך ונשלח לך קישור לאיפוס</p>
          </div>

          {submitted ? (
            <div style={{ background: "#f0fdf4", border: "1px solid #bbf7d0", borderRadius: 10, padding: "1rem", textAlign: "center" }}>
              <p style={{ fontSize: "0.875rem", color: "#15803d", margin: 0 }}>
                אם הכתובת רשומה במערכת, תקבל הודעה בקרוב.
              </p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: "1rem" }} dir="rtl">
              <div>
                <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                  אימייל
                </label>
                <input
                  type="email"
                  required
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="you@example.com"
                  style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
                />
              </div>
              <button
                type="submit"
                disabled={loading}
                style={{ width: "100%", background: loading ? "#93c5fd" : "#3b82f6", color: "white", border: "none", borderRadius: 10, padding: "0.75rem", fontSize: "0.875rem", fontWeight: 600, cursor: loading ? "not-allowed" : "pointer" }}
              >
                {loading ? "שולח..." : "שלח קישור לאיפוס"}
              </button>
            </form>
          )}

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

