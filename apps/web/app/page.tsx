import { redirect } from "next/navigation";

// Root redirects to today's schedule (or login if not authenticated — handled by middleware)
export default function RootPage() {
  redirect("/schedule/today");
}
