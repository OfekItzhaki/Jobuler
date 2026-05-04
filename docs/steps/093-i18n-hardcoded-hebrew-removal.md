# Step 093 — i18n: Remove All Hardcoded Hebrew Strings

## Phase
Phase 9 — Quality & Internationalisation

## Purpose
The web app had a large number of hardcoded Hebrew strings scattered across component files. These strings were invisible to the i18n system, meaning they always rendered in Hebrew regardless of the user's selected language (English or Russian). This step removes every hardcoded Hebrew string from TSX files and replaces them with `useTranslations()` calls backed by keys in all three message files (`en.json`, `he.json`, `ru.json`).

## What was built

### Files modified — components

| File | What changed |
|---|---|
| `apps/web/app/groups/[groupId]/tabs/MembersTab.tsx` | Added `useTranslations` to both `MembersTab` and `MemberProfileModal`. Replaced all Hebrew UI strings with `t()` / `tCommon()` / `tProfile()` calls. |
| `apps/web/app/groups/[groupId]/tabs/ScheduleTab.tsx` | Added `useTranslations`. Replaced draft banner, discard confirm, infeasible banner, export button, week navigation, loading text, and filter placeholder. Removed fragile `scheduleError.includes("אין חיבור")` string-sniff; replaced with a clean `scheduleIsOffline` boolean prop. |
| `apps/web/app/groups/[groupId]/tabs/SettingsTab.tsx` | Added `useTranslations`. Replaced all section titles, labels, buttons, and error messages throughout the entire component. |
| `apps/web/app/groups/[groupId]/tabs/MessagesTab.tsx` | Added `useTranslations` to the `MessageCard` sub-component (parent already had it). Replaced all Hebrew strings in the card (save/cancel/edit/delete/pin/unpin/confirm). |
| `apps/web/app/groups/[groupId]/tabs/ConstraintsTab.tsx` | Added `useTranslations` to `ConstraintRow` and `SectionCreateForm` sub-components and the main `ConstraintsTab`. Replaced all Hebrew labels, section titles, severity options, and modal strings. |
| `apps/web/app/groups/[groupId]/tabs/QualificationsTab.tsx` | Replaced Hebrew `title` tooltip attribute with `t("deactivate")` / `t("add")`. |
| `apps/web/app/admin/people/[personId]/page.tsx` | Added `useTranslations("personDetail")` and `useTranslations("common")`. Replaced every Hebrew string in the entire page (roles, groups, qualifications, availability windows, presence windows, restrictions). |
| `apps/web/app/groups/[groupId]/page.tsx` | Added `scheduleIsOffline` state; set it to `true` whenever an offline error is set. Passed it as a prop to `ScheduleTab`. |

### Files modified — i18n messages

| File | What was added |
|---|---|
| `apps/web/messages/en.json` | New `personDetail` namespace (40+ keys). Extended `groups.members_tab` with modal/availability keys. Extended `groups.settings_tab` with `errorCreateRole`, `errorUpdateRole`. Extended `groups.schedule_tab` with `draftBadge`, `discardConfirmText`, `yesDiscard`, `discarding`, `cancel`, `filterByName`, `exportCsv`, `thisWeek`, `infeasible`. |
| `apps/web/messages/he.json` | Same additions as `en.json`, all translated to Hebrew. Added `groups.settings_tab` and `groups.schedule_tab` sections (previously missing entirely). |
| `apps/web/messages/ru.json` | Same additions as `en.json`, all translated to Russian. Extended `groups.members_tab`, `groups.settings_tab`, `groups.schedule_tab`, and added `personDetail` namespace. |

## Key decisions

- **`AppShell.tsx` locale labels left as-is** — `"עב"` and `"עברית"` are intentional: they are the Hebrew language name displayed in Hebrew script in the language switcher. This is correct UX behaviour.
- **Code comment left as-is** — `// Week range label e.g. "12–18 ינואר"` is a developer note, not rendered UI.
- **`scheduleIsOffline` prop** — Rather than sniffing the translated error string for Hebrew/English/Russian substrings to determine error styling, a clean boolean prop is now passed from the parent. This is locale-agnostic and won't break if translations change.
- **Sub-component translations** — `ConstraintRow`, `SectionCreateForm`, and `MessageCard` each call `useTranslations` directly rather than receiving translated strings as props, keeping the translation boundary at the component level.

## How it connects

- All three locales (Hebrew, English, Russian) now render fully translated UI across every group detail tab, the admin person detail page, and the schedule tab.
- The `personDetail` namespace is new — it covers the admin-only person detail page which was previously 100% Hebrew.
- The `groups.schedule_tab` namespace in `he.json` and `ru.json` was previously missing; it is now complete.

## How to run / verify

1. Start the web app: `cd apps/web && npm run dev`
2. Switch language to English or Russian via the language switcher in the top bar
3. Navigate to any group → verify all tabs (Schedule, Members, Settings, Constraints, Messages, Qualifications) render in the selected language
4. Navigate to Admin → People → any person → verify the detail page renders in the selected language
5. No Hebrew text should appear anywhere except the "עב" / "עברית" label in the language switcher itself

## What comes next

- Any remaining admin pages that were not covered in this pass (e.g. `admin/tasks`, `admin/groups`) can be i18n'd in a follow-up step
- RTL layout polish for the Hebrew locale

## Git commit

```bash
git add -A && git commit -m "feat(i18n): remove all hardcoded Hebrew strings from web app components"
```
