"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store/authStore";
import { apiClient } from "@/lib/api/client";

function AcceptInvitationContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const { isAuthenticated } = useAuthStore();

  const [status, setStatus] = useState<"idle" | "loading" | "success" | "error">("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated || !token) return;
    setStatus("loading");
    apiClient
      .post(`/invitations/accept?token=${encodeURIComponent(token)}`)
      .then(() => setStatus("success"))
      .catch((err) => {
        const msg =
          err?.response?.data?.message ??
          err?.response?.data?.title ??
          "אירעה שגיאה בעת אישור ההזמנה.";
        setErrorMessage(msg);
        setStatus("error");
      });
  }, [isAuthenticated, token]);

  if (!token) {
    return (
      <div style={styles.card}>
        <p style={styles.errorText}>קישור ההזמנה אינו תקין — חסר טוקן.</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    const redirectUrl = `/invitations/accept?token=${encodeURIComponent(token)}`;
    return (
      <div style={styles.card}>
        <p style={styles.bodyText}>יש להתחבר כדי לאשר את ההזמנה.</p>
        <Link href={`/login?redirect=${encodeURIComponent(redirectUrl)}`} style={styles.link}>
          התחבר
        </Link>
      </div>
    );
  }

  if (status === "loading") {
    return (
      <div style={styles.card}>
        <div style={styles.spinner} />
        <p style={styles.bodyText}>מאשר הזמנה…</p>
      </div>
    );
  }

  if (status === "success") {
    return (
      <div style={styles.card}>
        <div style={styles.successIcon}>✓</div>
        <p style={styles.successText}>ההזמנה אושרה בהצלחה! ברוך הבא לקבוצה.</p>
        <Link href="/groups" style={styles.link}>
          עבור לקבוצות
        </Link>
      </div>
    );
  }

  if (status === "error") {
    return (
      <div style={styles.card}>
        <p style={styles.errorText}>{errorMessage}</p>
        <Link href="/groups" style={styles.link}>
          חזור לקבוצות
        </Link>
      </div>
    );
  }

  return null;
}

export default function AcceptInvitationPage() {
  return (
    <div style={styles.page}>
      <Suspense fallback={<div style={styles.card}><div style={styles.spinner} /></div>}>
        <AcceptInvitationContent />
      </Suspense>
    </div>
  );
}

const styles = {
  page: {
    minHeight: "100vh",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    background: "#f8fafc",
    direction: "rtl" as const,
  },
  card: {
    background: "white",
    borderRadius: 16,
    padding: "40px 48px",
    boxShadow: "0 4px 24px rgba(0,0,0,0.08)",
    display: "flex",
    flexDirection: "column" as const,
    alignItems: "center",
    gap: 16,
    minWidth: 320,
    textAlign: "center" as const,
  },
  bodyText: {
    fontSize: 15,
    color: "#334155",
    margin: 0,
  },
  successText: {
    fontSize: 15,
    color: "#166534",
    fontWeight: 600,
    margin: 0,
  },
  errorText: {
    fontSize: 14,
    color: "#dc2626",
    margin: 0,
  },
  link: {
    fontSize: 14,
    color: "#3b82f6",
    textDecoration: "none",
    fontWeight: 500,
  },
  successIcon: {
    width: 48,
    height: 48,
    borderRadius: "50%",
    background: "#dcfce7",
    color: "#16a34a",
    fontSize: 22,
    fontWeight: 700,
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
  },
  spinner: {
    width: 32,
    height: 32,
    border: "3px solid #e2e8f0",
    borderTop: "3px solid #3b82f6",
    borderRadius: "50%",
    animation: "spin 0.8s linear infinite",
  },
};
