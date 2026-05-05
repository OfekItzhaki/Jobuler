-- Migration 027: Group-scoped roles
-- Adds group_id to space_roles and person_role_assignments so each group
-- can have its own independent role set.
-- Backward compatible: existing rows keep group_id = NULL (space-level roles).

-- ─── space_roles: add group_id ───────────────────────────────────────────────
ALTER TABLE space_roles
    ADD COLUMN group_id UUID REFERENCES groups(id) ON DELETE CASCADE;

-- Drop old unique constraint (space_id, name) — same name is now allowed in
-- different groups within the same space.
ALTER TABLE space_roles
    DROP CONSTRAINT space_roles_space_id_name_key;

-- New unique constraint: (space_id, group_id, name)
-- NULLS NOT DISTINCT ensures two rows with the same space_id, NULL group_id,
-- and same name are still considered duplicates (space-level roles).
CREATE UNIQUE INDEX idx_space_roles_space_group_name
    ON space_roles (space_id, group_id, name)
    NULLS NOT DISTINCT;

-- ─── person_role_assignments: add group_id ───────────────────────────────────
ALTER TABLE person_role_assignments
    ADD COLUMN group_id UUID REFERENCES groups(id) ON DELETE CASCADE;

-- Drop old unique constraint (person_id, role_id)
ALTER TABLE person_role_assignments
    DROP CONSTRAINT person_role_assignments_person_id_role_id_key;

-- New unique constraint: (person_id, role_id, group_id)
CREATE UNIQUE INDEX idx_person_role_assignments_person_role_group
    ON person_role_assignments (person_id, role_id, group_id)
    NULLS NOT DISTINCT;

-- Index for group-scoped lookups
CREATE INDEX idx_space_roles_group ON space_roles (group_id) WHERE group_id IS NOT NULL;
CREATE INDEX idx_person_role_assignments_group ON person_role_assignments (group_id) WHERE group_id IS NOT NULL;
