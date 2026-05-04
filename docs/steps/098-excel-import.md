# Step 098 — Excel / CSV Import

## Phase
Phase 9 — Quality & Polish

## Purpose
Admins needed a way to bulk-add members and tasks from a spreadsheet instead of entering them one by one. This step adds an Excel/CSV import flow accessible from both the Members tab and the Tasks tab.

## What was built

### `ImportModal.tsx` (new)
A self-contained modal with three steps:

**Step 1 — Instructions**
- Two tabs: Members / Tasks
- Shows a table of required and optional column names with examples
- "Download template" button generates a pre-formatted `.xlsx` file the admin can fill in
- Drag-and-drop or file picker for `.xlsx`, `.xls`, `.csv`

**Step 2 — Preview**
- Parses the file in the browser using SheetJS (`xlsx@0.18.5`)
- Shows a table of all parsed rows before importing
- Admin can go back and re-upload if something looks wrong

**Step 3 — Import / Done**
- Imports rows one by one, showing live status (✅ / ❌ / ⏭ already exists)
- Members: calls `createPerson` then `addGroupMemberById` per row
- Tasks: calls `createGroupTask` per row with sensible defaults (starts now, ends 90 days ahead)
- 409 conflicts (person already exists) are shown as "skip" not error
- Done screen shows count of imported records and errors

### Column specs

**Members:**
| Column | Required |
|---|---|
| Full Name | ✅ |
| Display Name | optional |
| Phone | optional |
| Email | optional |

**Tasks:**
| Column | Required |
|---|---|
| Name | ✅ |
| Shift Duration (hours) | ✅ |
| Headcount | ✅ |
| Burden Level | optional (neutral/disliked/hated/favorable) |
| Daily Start | optional (HH:mm) |
| Daily End | optional (HH:mm) |

Hebrew column names are also accepted (שם מלא, טלפון, etc.).

### `MembersTab.tsx` (updated)
- Added optional `onOpenImport` prop
- Import 📥 button appears next to "Add Member" when admin

### `TasksTab.tsx` (updated)
- Added optional `onOpenImport` prop
- Import 📥 button appears next to "New Task" when admin

### `page.tsx` (updated)
- Added `showImportModal` and `importMode` state
- Renders `ImportModal` when open
- After import: reloads members or tasks depending on mode

### `package.json` (updated)
- Added `xlsx@0.18.5` (SheetJS) — browser-side Excel/CSV parsing, no server needed

## Key decisions
- All parsing happens in the browser — no new API endpoints needed
- SheetJS is the industry standard for Excel in JS, well-maintained, MIT licensed
- Template download uses the same library so the format is guaranteed to match the parser
- 409 conflicts are treated as "skip" not "error" — safe to re-run the import

## How to verify
1. Open a group → Members tab → click 📥
2. Download the template, fill in some names, upload it
3. Preview shows the rows, click Import
4. Members appear in the group
5. Repeat for Tasks tab

## Git commit

```bash
git add -A && git commit -m "feat(import): Excel/CSV import for members and tasks"
```
