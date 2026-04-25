export const SEVERITY_BADGE: Record<string, { bg: string; text: string; border: string; label: string }> = {
  info:     { bg: "bg-blue-50",  text: "text-blue-700",  border: "border-blue-200",  label: "מידע" },
  warning:  { bg: "bg-amber-50", text: "text-amber-700", border: "border-amber-200", label: "אזהרה" },
  critical: { bg: "bg-red-50",   text: "text-red-700",   border: "border-red-200",   label: "קריטי" },
};

export function getSeverityBadge(severity: string) {
  return SEVERITY_BADGE[severity.toLowerCase()] ?? SEVERITY_BADGE["info"];
}
