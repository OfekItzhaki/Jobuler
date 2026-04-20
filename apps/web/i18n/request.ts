import { getRequestConfig } from "next-intl/server";
import { cookies } from "next/headers";

export const locales = ["he", "en", "ru"] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = "he";

export const rtlLocales: Locale[] = ["he"];

export function isRtl(locale: Locale): boolean {
  return rtlLocales.includes(locale);
}

export default getRequestConfig(async () => {
  // Read locale from cookie set at login, fall back to Hebrew
  const cookieStore = await cookies();
  const locale = (cookieStore.get("locale")?.value ?? defaultLocale) as Locale;

  return {
    locale,
    messages: (await import(`../messages/${locale}.json`)).default,
  };
});
