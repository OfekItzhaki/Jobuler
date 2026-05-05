-- Migration 030: Add is_default to space_roles
-- Marks the system-created "Member" role per group that cannot be deleted.
-- Auto-created when a group is created. Can be renamed but not removed.

ALTER TABLE space_roles
    ADD COLUMN IF NOT EXISTS is_default BOOLEAN NOT NULL DEFAULT FALSE;

COMMENT ON COLUMN space_roles.is_default IS
    'TRUE for the system-created default role per group. Cannot be deleted, only renamed.';

-- Enforce at most one default role per group
CREATE UNIQUE INDEX IF NOT EXISTS idx_space_roles_one_default_per_group
    ON space_roles (space_id, group_id)
    WHERE is_default = TRUE;

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('030') ON CONFLICT DO NOTHING;
