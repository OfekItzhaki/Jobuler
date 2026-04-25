# Requirements Document

## Introduction

This feature extends the Jobuler scheduling app with three coordinated additions and two housekeeping tasks:

1. **Phone Number in Member List** — The `GroupMemberDto` is extended to expose each member's phone number (already stored on the `Person` domain entity since migration 010). Phone numbers are visible to all group members so they can contact each other directly, eliminating the need for a separate WhatsApp group.

2. **Group Alerts Tab ("התראות")** — A new tab on the group detail page where group admins can broadcast short one-way messages (title, body, severity) to all group members. Members can read alerts but cannot reply, edit, or delete them. Admins can delete their own alerts. Alerts are scoped per group and stored in a new `group_alerts` table.

3. **Forgot Password** — A password reset flow backed by a `password_reset_tokens` table. The reset token is generated and stored server-side. Delivery is abstracted behind `INotificationSender` (no-op by default) so it can be wired to email or WhatsApp later without changing business logic. A `/reset-password` UI page lets users set a new password using the token.

4. **Close-out: group-ownership task 20** — Create `docs/steps/029-group-ownership.md` step documentation.

5. **Close-out: group-detail-page task 10** — Final checkpoint ensuring all tests pass.

---

## Glossary

- **System**: The Jobuler backend (ASP.NET Core API + Application + Domain + Infrastructure layers).
- **UI**: The Jobuler Next.js frontend.
- **GroupDetailPage**: The Next.js page at `/groups/[groupId]` introduced in the group-detail-page spec.
- **Group_Member**: Any user who belongs to a group, including the owner.
- **Group_Admin**: A user whose `adminGroupId` in AuthStore equals the current `groupId` (frontend) / a user with the `people.manage` permission scoped to the group's space (backend).
- **GroupMemberDto**: The DTO returned by `GET /spaces/{spaceId}/groups/{groupId}/members`, currently containing `personId`, `fullName`, `displayName`, `isOwner`.
- **Person**: The domain entity representing a person in a space. Has a `PhoneNumber` field added in migration 010.
- **Alert**: A short broadcast message posted by a Group_Admin to all members of a specific group. Has a title, body, severity, creation timestamp, and creator.
- **Alert_Severity**: An enumeration with three values: `info`, `warning`, `critical`.
- **Group_Alerts_Table**: The new `group_alerts` PostgreSQL table introduced by this feature.
- **IPermissionService**: The Application-layer interface used for all permission checks.
- **ITenantScoped**: The interface that all tenant-scoped domain entities must implement, requiring a `SpaceId` property.
- **AuthStore**: The Zustand store holding authentication state including `adminGroupId`.
- **SpaceStore**: The Zustand store holding `currentSpaceId`.
- **apiClient**: The authenticated HTTP client used by the Next.js frontend.
- **INotificationSender**: A new Application-layer interface abstracting message delivery (email, WhatsApp, SMS). Has a no-op default implementation. Used exclusively for password reset token delivery.
- **PasswordResetToken**: A domain entity storing a hashed reset token, the target user ID, an expiry timestamp, and a `used_at` timestamp. Stored in the `password_reset_tokens` table.
- **Reset_Token**: A cryptographically random 64-character hex string generated server-side, sent to the user, and never stored in plain text (only its SHA-256 hash is stored).

---

## Requirements

### Requirement 1: Expose Phone Number in GroupMemberDto

**User Story:** As a group member, I want to see the phone numbers of other members in the member list, so that I can contact them directly without needing a separate communication channel.

#### Acceptance Criteria

1. THE System SHALL include a `phoneNumber` field of type `string | null` in `GroupMemberDto`, populated from the `phone_number` column of the `people` table via the existing `GetGroupMembersQuery`.
2. WHEN a person has no phone number recorded, THE System SHALL return `null` for the `phoneNumber` field in `GroupMemberDto`.
3. WHEN `GET /spaces/{spaceId}/groups/{groupId}/members` is called by any authenticated group member, THE System SHALL return the `phoneNumber` field for every member in the response.
4. THE System SHALL NOT require any new database migration for this change — the `phone_number` column already exists on the `people` table from migration 010.
5. THE System SHALL include `phone_number` in the SQL projection of `GetGroupMembersQuery` by joining or selecting from the `people` table.

---

### Requirement 2: Display Phone Number in Member List UI

**User Story:** As a group member, I want to see each member's phone number in the member list on the group detail page, so that I can quickly find a contact number without leaving the app.

#### Acceptance Criteria

1. WHEN the "חברים" tab is active in read-only mode (non-admin), THE UI SHALL display each member's `phoneNumber` alongside their display name, or display nothing for that field if `phoneNumber` is `null`.
2. WHEN the "חברים" tab is active in admin-edit mode, THE UI SHALL display each member's `phoneNumber` alongside their display name and the remove button, or display nothing for that field if `phoneNumber` is `null`.
3. THE UI SHALL display phone numbers as plain text — no click-to-call formatting is required.
4. THE UI SHALL update the `GroupMemberDto` TypeScript interface in `lib/api/groups.ts` to include `phoneNumber: string | null`.
5. WHEN a member's `phoneNumber` is `null`, THE UI SHALL render an empty cell or omit the phone column value — it SHALL NOT display the string "null" or "undefined".

---

### Requirement 3: Group Alerts Domain Entity and Migration

**User Story:** As a developer, I want a `GroupAlert` domain entity and corresponding database table, so that alerts can be persisted and queried with proper tenant isolation.

#### Acceptance Criteria

1. THE System SHALL create a new database migration (migration 011) that adds a `group_alerts` table with columns: `id UUID PRIMARY KEY`, `space_id UUID NOT NULL`, `group_id UUID NOT NULL`, `title VARCHAR(200) NOT NULL`, `body TEXT NOT NULL`, `severity VARCHAR(20) NOT NULL`, `created_at TIMESTAMPTZ NOT NULL DEFAULT now()`, `created_by_person_id UUID NOT NULL`.
2. THE System SHALL add a foreign key from `group_alerts.group_id` to `groups.id`.
3. THE System SHALL add a foreign key from `group_alerts.created_by_person_id` to `people.id`.
4. THE System SHALL add an index on `group_alerts (space_id, group_id, created_at DESC)` to support efficient per-group listing ordered by recency.
5. THE System SHALL create a `GroupAlert` domain entity implementing `ITenantScoped` with properties: `Id`, `SpaceId`, `GroupId`, `Title`, `Body`, `Severity` (enum: `Info`, `Warning`, `Critical`), `CreatedAt`, `CreatedByPersonId`.
6. THE `GroupAlert` domain entity SHALL expose a static factory method `Create(spaceId, groupId, title, body, severity, createdByPersonId)` that sets `CreatedAt = DateTime.UtcNow`.
7. THE System SHALL register `GroupAlert` as a `DbSet` in `AppDbContext` with EF Fluent API configuration mapping to the `group_alerts` table.

---

### Requirement 4: Create Alert (Admin Only)

**User Story:** As a group admin, I want to post a broadcast alert to my group, so that I can communicate important information to all members at once.

#### Acceptance Criteria

1. WHEN an authenticated user with `people.manage` permission submits a create-alert request for a group, THE System SHALL persist a new `GroupAlert` record and return HTTP 201 with the created alert's `id` and `createdAt`.
2. IF the authenticated user does not have `people.manage` permission, THEN THE System SHALL return HTTP 403 when a create-alert request is received.
3. THE System SHALL validate that `title` is between 1 and 200 characters and is not blank; IF invalid, THEN THE System SHALL return HTTP 400.
4. THE System SHALL validate that `body` is between 1 and 2000 characters and is not blank; IF invalid, THEN THE System SHALL return HTTP 400.
5. THE System SHALL validate that `severity` is one of `info`, `warning`, or `critical` (case-insensitive); IF invalid, THEN THE System SHALL return HTTP 400.
6. THE System SHALL resolve the `createdByPersonId` from the authenticated user's linked person record within the group's space; IF no linked person is found, THEN THE System SHALL return HTTP 400.
7. THE System SHALL perform the permission check via `IPermissionService` in the Application layer before persisting the alert.
8. THE System SHALL expose the endpoint as `POST /spaces/{spaceId}/groups/{groupId}/alerts` requiring `[Authorize]`.

---

### Requirement 5: List Alerts (All Group Members)

**User Story:** As a group member, I want to read all alerts posted to my group, so that I stay informed about important announcements.

#### Acceptance Criteria

1. WHEN an authenticated group member calls `GET /spaces/{spaceId}/groups/{groupId}/alerts`, THE System SHALL return all alerts for that group ordered by `created_at` descending (newest first).
2. THE System SHALL include the following fields in each alert response: `id`, `title`, `body`, `severity`, `createdAt`, `createdByPersonId`, `createdByDisplayName`.
3. THE System SHALL verify that the requesting user is a member of the group before returning alerts; IF the user is not a member, THEN THE System SHALL return HTTP 403.
4. THE System SHALL filter the query by `space_id` to enforce tenant isolation.
5. IF the group has no alerts, THE System SHALL return an empty array with HTTP 200.
6. THE System SHALL resolve `createdByDisplayName` from the `people` table (falling back to `full_name` if `display_name` is null) in the same query.

---

### Requirement 6: Delete Alert (Admin, Own Alerts Only)

**User Story:** As a group admin, I want to delete alerts I have posted, so that I can remove outdated or incorrect information.

#### Acceptance Criteria

1. WHEN an authenticated user with `people.manage` permission calls `DELETE /spaces/{spaceId}/groups/{groupId}/alerts/{alertId}` and the alert's `created_by_person_id` matches the caller's linked person, THE System SHALL delete the alert and return HTTP 204.
2. IF the authenticated user does not have `people.manage` permission, THEN THE System SHALL return HTTP 403.
3. IF the alert's `created_by_person_id` does not match the caller's linked person (i.e., the admin is trying to delete another admin's alert), THEN THE System SHALL return HTTP 403.
4. IF the alert is not found or does not belong to the specified group and space, THEN THE System SHALL return HTTP 404.
5. THE System SHALL perform both the permission check and the ownership check in the Application layer via `IPermissionService` and direct entity comparison.

---

### Requirement 7: Alerts Tab in Group Detail Page

**User Story:** As a group member, I want a dedicated "התראות" tab on the group detail page, so that I can easily find and read all alerts for my group.

#### Acceptance Criteria

1. THE GroupDetailPage SHALL render a "התראות" tab visible to ALL group members regardless of admin mode state.
2. WHEN the "התראות" tab is active, THE UI SHALL fetch and display alerts from `GET /spaces/{spaceId}/groups/{groupId}/alerts`.
3. THE UI SHALL display each alert with its `title`, `body`, `severity` (as a visual badge or label), and `createdAt` (formatted as a human-readable date/time in Hebrew locale).
4. THE UI SHALL display the `createdByDisplayName` for each alert so members know who posted it.
5. THE UI SHALL render alerts ordered newest-first, matching the API response order.
6. IF the alerts list is empty, THE UI SHALL display the message "אין התראות לקבוצה זו".
7. IF the alerts fetch fails, THE UI SHALL display an error message in Hebrew.
8. THE UI SHALL apply a distinct visual style per severity: `info` — blue, `warning` — amber, `critical` — red.

---

### Requirement 8: Create Alert UI (Admin Only)

**User Story:** As a group admin, I want a form in the alerts tab to post a new alert, so that I can broadcast messages without leaving the group detail page.

#### Acceptance Criteria

1. WHEN `adminGroupId` equals `groupId`, THE UI SHALL display a create-alert form at the top of the "התראות" tab containing: a title input, a body textarea, a severity selector (info / warning / critical), and a submit button.
2. WHEN `adminGroupId` does not equal `groupId`, THE UI SHALL NOT render the create-alert form.
3. WHEN the admin submits the form with valid inputs, THE UI SHALL call `POST /spaces/{spaceId}/groups/{groupId}/alerts` and re-fetch the alerts list on success.
4. IF the create-alert API call returns an error, THEN THE UI SHALL display the error message below the form.
5. WHEN the alert is created successfully, THE UI SHALL clear the form fields and display the new alert at the top of the list without a full page reload.
6. THE UI SHALL disable the submit button while the create request is in flight to prevent duplicate submissions.

---

### Requirement 9: Delete Alert UI (Admin, Own Alerts Only)

**User Story:** As a group admin, I want to delete alerts I have posted directly from the alerts tab, so that I can manage the alert history without a separate admin panel.

#### Acceptance Criteria

1. WHEN `adminGroupId` equals `groupId`, THE UI SHALL display a delete button on each alert whose `createdByPersonId` matches the current user's linked person ID.
2. WHEN `adminGroupId` does not equal `groupId`, THE UI SHALL NOT render any delete buttons on alerts.
3. WHEN the admin clicks the delete button, THE UI SHALL call `DELETE /spaces/{spaceId}/groups/{groupId}/alerts/{alertId}` and remove the alert from the list on success.
4. IF the delete API call returns an error, THEN THE UI SHALL display the error message in Hebrew.
5. THE UI SHALL NOT display a confirmation dialog before deleting an alert — the delete is immediate.

---

### Requirement 10: Close-out — group-ownership Step Documentation

**User Story:** As a developer, I want step documentation for the group-ownership implementation, so that any developer can understand what was built and why by reading `docs/steps/` in order.

#### Acceptance Criteria

1. THE System SHALL include a file at `docs/steps/029-group-ownership.md` covering: title, phase, purpose, files created/modified, key decisions, how it connects to other parts of the system, how to verify, what comes next, and a git commit command.
2. THE documentation SHALL reference all major files introduced or modified by the group-ownership spec (migration 009, domain entities, commands, queries, controller endpoints, frontend components).
3. THE documentation SHALL note the ownership transfer email confirmation flow and the no-op `IEmailSender` default.

---

### Requirement 11: Close-out — group-detail-page Final Checkpoint

**User Story:** As a developer, I want confirmation that all tests pass after the group-detail-page implementation, so that the spec can be formally closed.

#### Acceptance Criteria

1. THE System SHALL have all backend unit and property tests in `apps/api/Jobuler.Tests` passing with no failures.
2. THE System SHALL have all frontend tests in `apps/web/__tests__` passing with no failures.
3. IF any test is failing, THE System SHALL surface the failure details so they can be resolved before this spec is considered complete.

---

### Requirement 12: Forgot Password — Token Generation and Storage

**User Story:** As a user who has forgotten their password, I want to request a password reset, so that I can regain access to my account.

#### Acceptance Criteria

1. THE System SHALL expose a `POST /auth/forgot-password` endpoint that accepts an `email` field and is `[AllowAnonymous]`.
2. WHEN the email matches a registered active user, THE System SHALL generate a cryptographically random 64-character hex reset token, store its SHA-256 hash in a new `password_reset_tokens` table alongside the `user_id`, `created_at`, `expires_at` (1 hour from now), and a nullable `used_at` timestamp.
3. WHEN the email does NOT match any registered user, THE System SHALL still return HTTP 200 — it SHALL NOT reveal whether the email exists (prevents user enumeration).
4. THE System SHALL allow at most one active (unused, non-expired) reset token per user at a time; IF one already exists, THE System SHALL invalidate it (set `used_at = now()`) before creating a new one.
5. THE System SHALL add a new database migration (migration 012) creating the `password_reset_tokens` table with columns: `id UUID PRIMARY KEY`, `user_id UUID NOT NULL REFERENCES users(id)`, `token_hash TEXT NOT NULL UNIQUE`, `created_at TIMESTAMPTZ NOT NULL DEFAULT now()`, `expires_at TIMESTAMPTZ NOT NULL`, `used_at TIMESTAMPTZ`.
6. THE System SHALL add an index on `password_reset_tokens (user_id)` and `password_reset_tokens (token_hash)`.

---

### Requirement 13: Forgot Password — Token Delivery via INotificationSender

**User Story:** As a developer, I want password reset tokens delivered through an abstracted notification interface, so that I can swap in email or WhatsApp delivery later without changing business logic.

#### Acceptance Criteria

1. THE System SHALL define an `INotificationSender` interface in the Application layer with a single method: `SendPasswordResetAsync(to: string, token: string, cancellationToken)` where `to` is either an email address or a phone number.
2. THE System SHALL register a `NoOpNotificationSender` implementation by default that logs the reset token at Warning level (so it is visible in dev logs) and returns immediately.
3. WHEN a reset token is generated for a user who has a `phone_number`, THE System SHALL call `INotificationSender.SendPasswordResetAsync` with the phone number as `to`.
4. WHEN a reset token is generated for a user who has no `phone_number` but has an `email`, THE System SHALL call `INotificationSender.SendPasswordResetAsync` with the email as `to`.
5. THE `NoOpNotificationSender` SHALL log the message: `"[NoOp] Password reset for {to}: token={token}"` at Warning level so developers can copy the token from logs during development.
6. ALL future notification delivery (WhatsApp, email, SMS) SHALL be implemented by registering a new `INotificationSender` in DI — no business logic changes required.

---

### Requirement 14: Forgot Password — Token Validation and Password Reset

**User Story:** As a user, I want to set a new password using the reset link I received, so that I can log in again.

#### Acceptance Criteria

1. THE System SHALL expose a `POST /auth/reset-password` endpoint that accepts `token` (the raw 64-char hex) and `newPassword` fields and is `[AllowAnonymous]`.
2. WHEN the endpoint is called, THE System SHALL hash the provided token with SHA-256 and look up the matching `password_reset_tokens` row.
3. IF no matching token is found, THE System SHALL return HTTP 400 with the message "Invalid or expired reset token."
4. IF the token's `expires_at` is in the past, THE System SHALL return HTTP 400 with the message "Invalid or expired reset token."
5. IF the token's `used_at` is not null (already used), THE System SHALL return HTTP 400 with the message "Invalid or expired reset token."
6. WHEN the token is valid, THE System SHALL hash `newPassword` with BCrypt (work factor 12), update the user's `password_hash`, set `used_at = now()` on the token record, and return HTTP 204 — all in a single transaction.
7. THE System SHALL validate that `newPassword` is at least 8 characters; IF invalid, THE System SHALL return HTTP 400.
8. AFTER a successful reset, THE System SHALL invalidate all existing refresh tokens for that user by setting their `revoked_at = now()`, forcing a fresh login.

---

### Requirement 15: Forgot Password — UI Flow

**User Story:** As a user, I want a "Forgot password?" link on the login page and a dedicated reset page, so that I can complete the password reset without contacting support.

#### Acceptance Criteria

1. THE login page SHALL display a "שכחת סיסמה?" link below the password field that navigates to `/forgot-password`.
2. THE `/forgot-password` page SHALL display a single email input and a submit button labelled "שלח קישור לאיפוס".
3. WHEN the user submits the form, THE UI SHALL call `POST /auth/forgot-password` and display a success message regardless of whether the email exists: "אם הכתובת רשומה במערכת, תקבל הודעה בקרוב."
4. THE `/reset-password` page SHALL read a `?token=` query parameter from the URL.
5. THE `/reset-password` page SHALL display a new-password input, a confirm-password input, and a submit button labelled "אפס סיסמה".
6. WHEN the user submits a valid new password, THE UI SHALL call `POST /auth/reset-password` with the token and new password, then redirect to `/login?reset=1` on success.
7. IF the reset API returns an error (invalid/expired token), THE UI SHALL display the error message in Hebrew.
8. THE login page SHALL display a success banner "הסיסמה אופסה בהצלחה! התחבר עם הסיסמה החדשה." when the URL contains `?reset=1`.
