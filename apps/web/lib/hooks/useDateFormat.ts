import { useAuthStore } from "@/lib/store/authStore";
import {
  formatDate,
  formatDateLong,
  formatDateTime,
  formatDateTimeShort,
  formatTime,
  formatDateRange,
} from "@/lib/utils/dateFormat";

/**
 * Returns locale-aware date formatting functions bound to the current user's locale.
 *
 * Usage:
 *   const { fDate, fDateTime, fTime } = useDateFormat();
 *   fDate("2026-04-27")          // "27/04/2026" (Israel) or "04/27/2026" (US)
 *   fDateTime("2026-04-27T07:15") // "27/04/2026, 07:15"
 */
export function useDateFormat() {
  const locale = useAuthStore(s => s.preferredLocale);

  return {
    fDate:      (d: string | Date | null | undefined) => formatDate(d, locale),
    fDateLong:  (d: string | Date | null | undefined) => formatDateLong(d, locale),
    fDateTime:  (d: string | Date | null | undefined) => formatDateTime(d, locale),
    fDateShort: (d: string | Date | null | undefined) => formatDateTimeShort(d, locale),
    fTime:      (d: string | Date | null | undefined) => formatTime(d, locale),
    fRange:     (from: string | Date | null | undefined, to: string | Date | null | undefined) =>
                  formatDateRange(from, to, locale),
    locale,
  };
}
