"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { register } from "@/lib/api/auth";

export default function RegisterPage() {
  const router = useRouter();

  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [birthday, setBirthday] = useState("");
  const [profileImageUrl, setProfileImageUrl] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (password !== confirmPassword) {
      setError("הסיסמאות אינן תואמות");
      return;
    }
    if (password.length < 8) {
      setError("הסיסמה חייבת להכיל לפחות 8 תווים");
      return;
    }

    setLoading(true);
    try {
      await register(email, displayName, password, "he", phoneNumber || undefined, profileImageUrl || undefined, birthday || undefined);
      router.push("/login?registered=1");
    } catch (err: any) {
      const msg = err?.response?.data?.error ?? err?.response?.data?.message;
      if (msg?.toLowerCase().includes("already registered")) {
        setError("כתובת האימייל כבר רשומה במערכת");
      } else {
        setError(msg ?? "שגיאה בהרשמה, נסה שוב");
      }
    } finally {
      setLoading(false);
    }
  }

  const eyeIcon = (visible: boolean) => (
    <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      {visible ? (
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
          d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
      ) : (
        <>
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
        </>
      )}
    </svg>
  );

  return (
    <main style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", background: "#f8fafc", padding: "1rem" }}>
      <div style={{ width: "100%", maxWidth: "400px" }}>
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

        {/* Card */}
        <div style={{ background: "white", borderRadius: 16, boxShadow: "0 4px 24px rgba(0,0,0,0.08)", border: "1px solid #e2e8f0", padding: "2rem" }}>
          <div style={{ marginBottom: "1.5rem" }}>
            <h1 style={{ fontSize: "1.25rem", fontWeight: 600, color: "#0f172a", margin: 0 }}>יצירת חשבון</h1>
            <p style={{ fontSize: "0.875rem", color: "#64748b", marginTop: "0.25rem" }}>הצטרף ל-Shifter</p>
          </div>

          <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: "1rem" }} dir="rtl">

            {/* Display name */}
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                שם מלא <span style={{ color: "#ef4444" }}>*</span>
              </label>
              <input
                type="text"
                required
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                placeholder="ישראל ישראלי"
                style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
              />
            </div>

            {/* Email */}
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                אימייל <span style={{ color: "#ef4444" }}>*</span>
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

            {/* Phone (optional) */}
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                מספר טלפון <span style={{ color: "#94a3b8", fontWeight: 400 }}>(אופציונלי)</span>
              </label>
              <input
                type="tel"
                value={phoneNumber}
                onChange={e => setPhoneNumber(e.target.value)}
                placeholder="050-0000000"
                style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
              />
              <p style={{ fontSize: "0.75rem", color: "#94a3b8", marginTop: "0.25rem" }}>
                יוצג לחברי הקבוצות שלך כדי שיוכלו ליצור איתך קשר
              </p>
            </div>

            {/* Birthday (optional) */}
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                תאריך לידה <span style={{ color: "#94a3b8", fontWeight: 400 }}>(אופציונלי)</span>
              </label>
              <input
                type="date"
                value={birthday}
                onChange={e => setBirthday(e.target.value)}
                style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
              />
            </div>

            {/* Profile image URL (optional) */}
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                תמונת פרופיל <span style={{ color: "#94a3b8", fontWeight: 400 }}>(URL, אופציונלי)</span>
              </label>
              <input
                type="url"
                value={profileImageUrl}
                onChange={e => setProfileImageUrl(e.target.value)}
                placeholder="https://example.com/photo.jpg"
                style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
              />
            </div>

            {/* Password */}
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                סיסמה <span style={{ color: "#ef4444" }}>*</span>
              </label>
              <div style={{ position: "relative" }}>
                <input
                  type={showPassword ? "text" : "password"}
                  required
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder="לפחות 8 תווים"
                  style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 2.5rem 0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
                />
                <button type="button" onClick={() => setShowPassword(!showPassword)}
                  style={{ position: "absolute", left: "0.75rem", top: "50%", transform: "translateY(-50%)", background: "none", border: "none", cursor: "pointer", color: "#94a3b8", padding: 0, display: "flex", alignItems: "center" }}>
                  {eyeIcon(showPassword)}
                </button>
              </div>
            </div>

            {/* Confirm password */}
            <div>
              <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 500, color: "#374151", marginBottom: "0.375rem" }}>
                אימות סיסמה <span style={{ color: "#ef4444" }}>*</span>
              </label>
              <input
                type={showPassword ? "text" : "password"}
                required
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                placeholder="הזן שוב את הסיסמה"
                style={{ width: "100%", border: "1px solid #e2e8f0", borderRadius: 10, padding: "0.625rem 0.875rem", fontSize: "0.875rem", color: "#0f172a", outline: "none", boxSizing: "border-box" }}
              />
            </div>

            {/* Error */}
            {error && (
              <div style={{ background: "#fef2f2", border: "1px solid #fecaca", borderRadius: 10, padding: "0.625rem 0.875rem", display: "flex", alignItems: "center", gap: "0.5rem" }}>
                <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="#ef4444" strokeWidth={2} style={{ flexShrink: 0 }}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p style={{ fontSize: "0.875rem", color: "#dc2626", margin: 0 }}>{error}</p>
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              style={{ width: "100%", background: loading ? "#93c5fd" : "#3b82f6", color: "white", border: "none", borderRadius: 10, padding: "0.75rem", fontSize: "0.875rem", fontWeight: 600, cursor: loading ? "not-allowed" : "pointer", marginTop: "0.25rem" }}
            >
              {loading ? "נרשם..." : "צור חשבון"}
            </button>
          </form>

          <p style={{ textAlign: "center", fontSize: "0.875rem", color: "#64748b", marginTop: "1.25rem" }}>
            כבר יש לך חשבון?{" "}
            <Link href="/login" style={{ color: "#3b82f6", fontWeight: 500, textDecoration: "none" }}>
              התחבר
            </Link>
          </p>
        </div>
      </div>
    </main>
  );
}

