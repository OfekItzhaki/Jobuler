# Requirements Document

## Introduction

This feature introduces group ownership to Jobuler. Currently, creating a group does not add the creator as a member, there is no concept of ownership, and any space admin can remove any member including the creator. This feature fixes the creator auto-membership bug, establishes a formal ownership model, adds owner-only group management actions (rename, soft-delete with recovery, ownership transfer), and protects the owner from being removed. A 5th demo user (Dana) is also added to seed data to support testing the "add by email" flow.

## Glossary

- **Group**: A container for people and schedules within a space.
- **Group_Owner**: The single user who holds ownership of a group. Initially the creator. Has exclusive access to destructive and transfer actions.
- **Group_Member**: A user who belongs to a group, including the owner.
- **Ownership_Transfer**: The process by which the current owner designates a new owner, requiring email confirmation from the recipient before taking effect.
- **Pending_Transfer**: An ownership transfer that has been initiated but not yet confirmed by the recipient.
- **Soft_Delete**: Marking a group as deleted by setting a `deleted_at` timestamp without physically removing the row.
- **Recovery_Window**: The 30-day period after soft-deletion during which the owner can restore the group.
- **Group_Avatar**: A colored circle displaying the first letter of the group name, shown in the group UI.
- **Space_Admin**: A user with the `people.manage` permission in a space.
- **Creator**: The authenticated user who issues the create-group request.
- **Confirmation_Email**: An email sent to the prospective new owner containing a link to accept the ownership transfer.
- **System**: The Jobuler backend (ASP.NET Core API + domain layer).
- **UI**: The Jobuler Next.js frontend.

---

## Requirements

### Requirement 1: Creator Auto-Membership and Initial Ownership

**User Story:** As a space admin, I want the group I create to automatically include me as a member and mark me as the owner, so that I am always part of the group I manage.

#### Acceptance Criteria

1. WHEN a group is created, THE System SHALL add the creator as a group member in the same transaction as the group creation.
2. WHEN a group is created, THE System SHALL mark the creator's membership record with `is_owner = true`.
3. THE System SHALL pass `CurrentUserId` from the controller to `CreateGroupCommand` so the handler can perform auto-membership.
4. WHEN a group is created, THE System SHALL store `created_by_user_id` on the group record equal to the creator's user ID.
5. IF the creator's user ID cannot be resolved at group creation time, THEN THE System SHALL return a 400 error and not persist the group.

---

### Requirement 2: Group Ownership Model

**User Story:** As a group owner, I want my ownership status to be formally tracked, so that the system can enforce owner-only protections and actions.

#### Acceptance Criteria

1. THE System SHALL ensure each group has exactly one member with `is_owner = true` at all times.
2. THE System SHALL add an `is_owner` boolean column to the `group_memberships` table via a new database migration (migration 009).
3. WHEN the `GetGroupMembersQuery` is executed, THE System SHALL include an `isOwner` field in each `GroupMemberDto` response.
4. WHEN a group is retrieved, THE System SHALL include an `ownerPersonId` field in the `GroupWithMemberCountDto` response so the UI can identify the owner.
5. THE System SHALL enforce that no two memberships for the same group have `is_owner = true` simultaneously at the database constraint level.

---

### Requirement 3: Owner Removal Protection

**User Story:** As a group owner, I want to be protected from being removed from my own group, so that groups always have an accountable owner.

#### Acceptance Criteria

1. WHEN a request is made to remove a member who has `is_owner = true`, THE System SHALL return HTTP 400 with the error message "Cannot remove the group owner. Transfer ownership first."
2. THE UI SHALL hide the "הסר" (remove) button for the member row whose `isOwner` is `true` in the members-edit tab.
3. WHEN a space admin attempts to remove the owner via the API directly, THE System SHALL reject the request with HTTP 400 regardless of the caller's permission level.
4. THE System SHALL perform the ownership check in `RemovePersonFromGroupCommand` before executing the removal.

---

### Requirement 4: Group Rename (Owner Only)

**User Story:** As a group owner, I want to rename my group from the settings tab, so that I can keep the group name accurate without needing a developer.

#### Acceptance Criteria

1. WHEN the authenticated user is the group owner and submits a rename request, THE System SHALL update the group's `name` field and return HTTP 204.
2. IF the authenticated user is not the group owner, THEN THE System SHALL return HTTP 403 when a rename request is received.
3. THE System SHALL validate that the new group name is between 1 and 100 characters and is not blank; IF the name is invalid, THEN THE System SHALL return HTTP 400.
4. THE UI SHALL display an inline editable text field for the group name in the settings tab, visible only when the current user is the group owner.
5. WHEN the rename is saved successfully, THE UI SHALL update the displayed group name without a full page reload.

---

### Requirement 5: Group Avatar

**User Story:** As a user, I want to see a colored circle with the first letter of the group name as the group's avatar, so that groups are visually distinguishable at a glance.

#### Acceptance Criteria

1. THE UI SHALL display a colored circle containing the first letter of the group name wherever a group is shown (group list, group detail header).
2. THE UI SHALL derive the avatar color deterministically from the group name so the same group always shows the same color.
3. THE UI SHALL render the avatar letter in uppercase.
4. WHERE a group name is empty or unavailable, THE UI SHALL display a fallback placeholder character ("?") in the avatar circle.

---

### Requirement 6: Soft-Delete Group (Owner Only)

**User Story:** As a group owner, I want to delete my group with a confirmation step and a 30-day recovery window, so that accidental deletions can be undone.

#### Acceptance Criteria

1. WHEN the group owner clicks "מחק קבוצה" in the settings tab, THE UI SHALL display a confirmation dialog with the text "האם אתה בטוח? ניתן לשחזר תוך 30 יום".
2. WHEN the owner confirms deletion, THE System SHALL set `deleted_at` to the current UTC timestamp on the group record and return HTTP 204.
3. IF the authenticated user is not the group owner, THEN THE System SHALL return HTTP 403 when a delete request is received.
4. WHEN a group has a non-null `deleted_at`, THE System SHALL exclude it from the `GetGroupsQuery` results.
5. THE System SHALL add a `deleted_at` nullable timestamp column to the `groups` table in migration 009.
6. WHEN a group is soft-deleted, THE System SHALL NOT remove any membership, task, or schedule data associated with the group. All `group_memberships` rows SHALL remain intact so that restoring the group immediately reinstates all members without re-invitation.
7. WHEN a group is soft-deleted, THE System SHALL exclude it from all member-facing queries (group list, schedule, assignments) so members lose access immediately.
8. WHEN a soft-deleted group is restored, THE System SHALL make it visible again to all existing members automatically, since their membership rows were never removed.

---

### Requirement 7: Deleted Group Recovery (Owner Only)

**User Story:** As a group owner, I want to restore a group I deleted within 30 days, so that I can recover from accidental deletions.

#### Acceptance Criteria

1. THE UI SHALL display a "קבוצות מחוקות" (Deleted Groups) section in the settings tab, visible only to the group owner.
2. WHEN the settings tab is loaded by the group owner, THE UI SHALL fetch and display all groups soft-deleted within the last 30 days that the user owns.
3. WHEN the owner clicks restore on a deleted group, THE System SHALL set `deleted_at` back to `null` and return HTTP 204.
4. IF the authenticated user is not the group owner, THEN THE System SHALL return HTTP 403 when a restore request is received.
5. WHEN a group's `deleted_at` is older than 30 days, THE System SHALL treat the group as permanently deleted and exclude it from restore results.
6. THE System SHALL filter permanently deleted groups on read (on-read filter approach) rather than running a scheduled deletion job.
7. WHEN a group is restored, THE System SHALL re-send the "added to group" notification (with opt-out token) to every existing member who has a linked user account, using the same notification flow as `AddPersonByEmailCommand`.
8. WHEN a group is restored, THE System SHALL send these notifications via the `IEmailSender` interface. IF no email provider is configured, THE System SHALL log the intent and continue without error (no-op implementation).

---

### Requirement 7a: Email Infrastructure

**User Story:** As a developer, I want an `IEmailSender` interface with a no-op default implementation, so that email sending can be wired to a real provider later without changing business logic.

#### Acceptance Criteria

1. THE System SHALL define an `IEmailSender` interface in the Application layer with a single method: `SendAsync(to, subject, htmlBody, cancellationToken)`.
2. THE System SHALL register a `NoOpEmailSender` implementation by default that logs the email intent at Debug level and returns immediately.
3. ALL email-sending in the system (ownership transfer confirmation, group restore notifications) SHALL go through `IEmailSender` exclusively — never directly through an SMTP client or third-party SDK.
4. WHEN a real email provider is added in the future, it SHALL only require registering a new `IEmailSender` implementation in DI — no business logic changes.

---

### Requirement 8: Ownership Transfer — Initiation

**User Story:** As a group owner, I want to transfer ownership to another member by selecting them from a dropdown, so that I can hand off responsibility without losing group continuity.

#### Acceptance Criteria

1. THE UI SHALL display an ownership transfer section in the settings tab, visible only to the group owner.
2. THE UI SHALL populate a dropdown with the current group members excluding the owner.
3. WHEN the owner selects a member and confirms the transfer, THE System SHALL create a `pending_ownership_transfer` record containing the group ID, the current owner's person ID, the proposed new owner's person ID, a unique confirmation token, and a `created_at` timestamp.
4. WHEN a pending transfer is created, THE System SHALL send a confirmation email to the proposed new owner's registered email address containing a confirmation link with the token.
5. THE System SHALL allow at most one pending transfer per group at a time; IF a pending transfer already exists, THEN THE System SHALL return HTTP 409 when a new transfer is initiated.
6. WHILE a pending transfer exists, THE UI SHALL display the transfer status as "ממתין לאישור" (Pending confirmation) in the settings tab.
7. THE System SHALL record an audit log entry when an ownership transfer is initiated, including actor_user_id, group_id, proposed_new_owner_person_id, and timestamp.

---

### Requirement 9: Ownership Transfer — Confirmation

**User Story:** As a prospective group owner, I want to confirm an ownership transfer via email link, so that I can accept ownership intentionally.

#### Acceptance Criteria

1. WHEN the proposed new owner clicks the confirmation link, THE System SHALL validate the token and verify it has not expired (tokens expire after 48 hours).
2. WHEN the token is valid, THE System SHALL set `is_owner = false` on the previous owner's membership, set `is_owner = true` on the new owner's membership, and delete the `pending_ownership_transfer` record — all in a single transaction.
3. IF the token is invalid or expired, THEN THE System SHALL return HTTP 400 with a descriptive error message.
4. WHEN ownership transfer is confirmed, THE System SHALL record an audit log entry including actor_user_id (new owner), group_id, previous_owner_person_id, and timestamp.
5. WHEN ownership transfer is confirmed, THE System SHALL ensure the previous owner remains a regular group member (is not removed).

---

### Requirement 10: Ownership Transfer — Cancellation

**User Story:** As a group owner, I want to cancel a pending ownership transfer, so that I can retract a transfer I initiated by mistake.

#### Acceptance Criteria

1. WHEN the current owner clicks "בטל העברה" (Cancel transfer) in the settings tab, THE System SHALL delete the `pending_ownership_transfer` record and return HTTP 204.
2. IF the authenticated user is not the current group owner, THEN THE System SHALL return HTTP 403 when a cancellation request is received.
3. IF no pending transfer exists for the group, THEN THE System SHALL return HTTP 404 when a cancellation request is received.
4. WHEN a transfer is cancelled, THE UI SHALL remove the "ממתין לאישור" status indicator and restore the transfer initiation form.

---

### Requirement 11: Seed Data — Ungrouped Demo User

**User Story:** As a developer, I want a demo user who is not a member of any group, so that I can test the "add by email" flow end-to-end without manual setup.

#### Acceptance Criteria

1. THE System SHALL include a 5th demo user in `seed.sql` with email `dana@demo.local`, display name "Dana", and the same BCrypt password hash as the other demo users.
2. THE System SHALL NOT add Dana to any `group_memberships` row in `seed.sql`.
3. THE System SHALL add Dana to the `space_memberships` table for the "Unit Alpha" demo space in `seed.sql`.
4. THE System SHALL assign Dana the `space.view` permission in `seed.sql` so she can log in and be found by the add-by-email flow.
