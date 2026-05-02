-- Migration 031: Backfill default "Member" role for existing groups
-- Creates one default role per group that doesn't already have one,
-- then assigns it to all current members who have no group-scoped role.

-- ─── Step 1: Insert default "Member" role for groups that don't have one ─────
INSERT INTO space_roles (id, space_id, group_id, name, description, is_default, permission_level, is_active, created_at, updated_at)
SELECT
    uuid_generate_v4(),
    g.space_id,
    g.id          AS group_id,
    'Member'      AS name,
    'Default role with no permissions' AS description,
    TRUE          AS is_default,
    'view'        AS permission_level,
    TRUE          AS is_active,
    NOW()         AS created_at,
    NOW()         AS updated_at
FROM groups g
WHERE g.deleted_at IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM space_roles sr
      WHERE sr.group_id = g.id
        AND sr.is_default = TRUE
  );

-- ─── Step 2: Assign the default role to members who have no group-scoped role ─
-- Uses a CTE to resolve the newly inserted (or pre-existing) default role per group.
WITH default_roles AS (
    SELECT sr.id AS role_id, sr.space_id, sr.group_id
    FROM space_roles sr
    WHERE sr.is_default = TRUE
      AND sr.group_id IS NOT NULL
      AND sr.is_active = TRUE
),
members_without_role AS (
    SELECT
        gm.space_id,
        gm.group_id,
        gm.person_id,
        dr.role_id
    FROM group_memberships gm
    JOIN default_roles dr
        ON dr.group_id = gm.group_id
       AND dr.space_id = gm.space_id
    WHERE NOT EXISTS (
        SELECT 1 FROM person_role_assignments pra
        WHERE pra.person_id = gm.person_id
          AND pra.group_id  = gm.group_id
    )
)
INSERT INTO person_role_assignments (id, space_id, person_id, role_id, group_id, assigned_at)
SELECT
    uuid_generate_v4(),
    mwr.space_id,
    mwr.person_id,
    mwr.role_id,
    mwr.group_id,
    NOW()
FROM members_without_role mwr
ON CONFLICT DO NOTHING;

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('031') ON CONFLICT DO NOTHING;
