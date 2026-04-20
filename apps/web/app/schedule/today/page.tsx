import AppShell from "@/components/shell/AppShell";
import { getTranslations } from "next-intl/server";

export default async function TodayPage() {
  const t = await getTranslations("schedule");

  return (
    <AppShell>
      <div className="space-y-4">
        <h1 className="text-xl font-semibold">{t("title")} — {/* today's date */}</h1>
        {/* Schedule table rendered here in Phase 5 */}
        <p className="text-gray-500 text-sm">{t("noAssignments")}</p>
      </div>
    </AppShell>
  );
}
