import { redirect } from "next/navigation";

// Root redirects to spaces selector — the space page auto-redirects
// to /schedule/today if the user only has one space
export default function RootPage() {
  redirect("/spaces");
}
