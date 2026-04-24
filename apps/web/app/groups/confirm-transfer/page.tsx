"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";

export default function ConfirmTransferPage() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token");
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [errorMessage, setErrorMessage] = useState<string>("");

  useEffect(() => {
    if (!token) {
      setStatus("error");
      setErrorMessage("טוקן חסר בקישור.");
      return;
    }
    // Use plain fetch — no auth headers needed
    fetch(`http://localhost:5000/groups/confirm-transfer?token=${encodeURIComponent(token)}`)
      .then(async res => {
        if (res.ok) {
          setStatus("success");
        } else {
          const body = await res.json().catch(() => ({}));
          setStatus("error");
          setErrorMessage(body.error ?? "הקישור אינו תקף או שפג תוקפו.");
        }
      })
      .catch(() => {
        setStatus("error");
        setErrorMessage("שגיאת רשת. אנא נסה שוב.");
      });
  }, [token]);

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-8 max-w-md w-full text-center space-y-4">
        {status === "loading" && (
          <>
            <div className="w-12 h-12 rounded-full bg-blue-50 flex items-center justify-center mx-auto">
              <svg className="animate-spin h-6 w-6 text-blue-500" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
            </div>
            <p className="text-slate-600 text-sm">מאמת העברת בעלות...</p>
          </>
        )}
        {status === "success" && (
          <>
            <div className="w-12 h-12 rounded-full bg-emerald-50 flex items-center justify-center mx-auto">
              <svg className="w-6 h-6 text-emerald-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h1 className="text-xl font-bold text-slate-900">הבעלות הועברה בהצלחה</h1>
            <p className="text-slate-500 text-sm">אתה כעת הבעלים של הקבוצה.</p>
            <Link href="/groups" className="inline-block mt-2 text-sm text-blue-500 hover:text-blue-700">
              עבור לקבוצות
            </Link>
          </>
        )}
        {status === "error" && (
          <>
            <div className="w-12 h-12 rounded-full bg-red-50 flex items-center justify-center mx-auto">
              <svg className="w-6 h-6 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h1 className="text-xl font-bold text-slate-900">שגיאה</h1>
            <p className="text-slate-500 text-sm">{errorMessage}</p>
            <Link href="/groups" className="inline-block mt-2 text-sm text-blue-500 hover:text-blue-700">
              חזרה לקבוצות
            </Link>
          </>
        )}
      </div>
    </div>
  );
}
