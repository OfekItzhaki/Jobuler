"use client";

import { useEffect, useState } from "react";
import AppShell from "@/components/shell/AppShell";
import { apiClient } from "@/lib/api/client";
import { useSpaceStore } from "@/lib/store/spaceStore";
import { useDateFormat } from "@/lib/hooks/useDateFormat";

interface NotificationDto {
  id: string;
  eventType: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

export default function NotificationsPage() {
  const { currentSpaceId } = useSpaceStore();
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!currentSpaceId) { setLoading(false); return; }
    apiClient.get(`/spaces/${currentSpaceId}/notifications`)
      .then(r => setNotifications(r.data))
      .catch(() => setNotifications([]))
      .finally(() => setLoading(false));
  }, [currentSpaceId]);

  async function markRead(id: string) {
    await apiClient.patch(`/spaces/${currentSpaceId}/notifications/${id}/read`).catch(() => {});
    setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
  }

  const unread = notifications.filter(n => !n.isRead);
  const read = notifications.filter(n => n.isRead);

  return (
    <AppShell>
      <div className="max-w-2xl space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">הודעות</h1>
          <p className="text-sm text-slate-500 mt-1">
            {unread.length > 0 ? `${unread.length} הודעות שלא נקראו` : "כל ההודעות נקראו"}
          </p>
        </div>

        {loading ? (
          <p className="text-slate-400 text-sm py-8">טוען...</p>
        ) : notifications.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center bg-white rounded-xl border border-slate-200">
            <svg className="w-10 h-10 text-slate-200 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
            <p className="text-slate-400 text-sm">אין הודעות</p>
          </div>
        ) : (
          <div className="space-y-2">
            {[...unread, ...read].map(n => (
              <div
                key={n.id}
                onClick={() => !n.isRead && markRead(n.id)}
                className={`flex items-start gap-4 bg-white border rounded-xl px-4 py-3.5 transition-colors ${
                  n.isRead
                    ? "border-slate-200 cursor-default"
                    : "border-blue-200 bg-blue-50/30 hover:bg-blue-50 cursor-pointer"
                }`}
              >
                <div className={`w-2 h-2 rounded-full mt-2 flex-shrink-0 ${n.isRead ? "bg-slate-200" : "bg-blue-500"}`} />
                <div className="flex-1 min-w-0">
                  <p className={`text-sm font-medium ${n.isRead ? "text-slate-600" : "text-slate-900"}`}>{n.title}</p>
                  <p className="text-xs text-slate-500 mt-0.5 line-clamp-2">{n.body}</p>
                  <p className="text-xs text-slate-400 mt-1">{new Date(n.createdAt).toLocaleString("he-IL")}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </AppShell>
  );
}
