-- Migration 009: Group ownership model
-- Adds is_owner flag to group_memberships, soft-delete to groups,
-- and pending_ownership_transfers table.

-- ── group_memberships: add is_owner flag ─────────────────────────────────────
ALTER TABLE group_memberships
    ADD COLUMN IF NOT EXISTS is_owner BOOLEAN NOT NULL DEFAULT false;

-- Enforce at most one owner per group at the DB level
CREATE UNIQUE INDEX IF NOT EXISTS uq_group_memberships_one_owner
    ON group_memberships (group_id)
    WHERE is_owner = true;

-- ── groups: add soft-delete support ──────────────────────────────────────────
ALTER TABLE groups
    ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;

CREATE INDEX IF NOT EXISTS idx_groups_deleted_at
    ON groups (deleted_at)
    WHERE deleted_at IS NOT NULL;

-- ── pending_ownership_transfers ───────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pending_ownership_transfers (
    id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id                    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_id                    UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    current_owner_person_id     UUID NOT NULL REFERENCES people(id),
    proposed_owner_person_id    UUID NOT NULL REFERENCES people(id),
    confirmation_token          TEXT NOT NULL UNIQUE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at                  TIMESTAMPTZ NOT NULL
);

-- One pending transfer per group at a time
CREATE UNIQUE INDEX IF NOT EXISTS uq_pending_transfers_group
    ON pending_ownership_transfers (group_id);

CREATE INDEX IF NOT EXISTS idx_pending_transfers_token
    ON pending_ownership_transfers (confirmation_token);

CREATE INDEX IF NOT EXISTS idx_pending_transfers_space
    ON pending_ownership_transfers (space_id);
