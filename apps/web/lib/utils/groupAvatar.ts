const COLORS = [
  "#3B82F6", "#10B981", "#F59E0B", "#EF4444",
  "#8B5CF6", "#EC4899", "#06B6D4", "#84CC16"
];

export function getAvatarColor(name: string): string {
  if (!name) return "#94A3B8";
  const sum = name.split("").reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
  return COLORS[sum % COLORS.length];
}

export function getAvatarLetter(name: string): string {
  return name ? name[0].toUpperCase() : "?";
}
