-- Fix seed data: link existing people to users, insert new-UUID records
-- Run as: psql -U postgres -d jobuler -f infra/scripts/fix_seed_data.sql

BEGIN;

-- ── Step 1: Link old-UUID people to their old-UUID users ──────────────────────
-- The old people rows (50000000-...) need linked_user_id pointing to old users (00000000-...)
UPDATE people SET linked_user_id = '00000000-0000-0000-0000-000000000001'
  WHERE id = '50000000-0000-0000-0000-000000000001' AND linked_user_id IS NULL;
UPDATE people SET linked_user_id = '00000000-0000-0000-0000-000000000002'
  WHERE id = '50000000-0000-0000-0000-000000000002' AND linked_user_id IS NULL;
UPDATE people SET linked_user_id = '00000000-0000-0000-0000-000000000003'
  WHERE id = '50000000-0000-0000-0000-000000000003' AND linked_user_id IS NULL;
UPDATE people SET linked_user_id = '00000000-0000-0000-0000-000000000004'
  WHERE id = '50000000-0000-0000-0000-000000000004' AND linked_user_id IS NULL;

-- ── Step 2: Insert new-UUID users (skip if email already exists) ──────────────
-- We can't use ON CONFLICT (id) because the emails already exist with old UUIDs.
-- Instead, insert only if the email doesn't exist yet.
INSERT INTO users (id, email, display_name, password_hash, preferred_locale)
SELECT 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'admin2@demo.local', 'Admin (new)', '$2a$12$WqeSlsFmXzSru4YK23qfeuMYIUd/4ZkHLLwx0NAehm.Vbmq1MYEEa', 'he'
WHERE NOT EXISTS (SELECT 1 FROM users WHERE id = 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5');

INSERT INTO users (id, email, display_name, password_hash, preferred_locale)
SELECT 'b2c3d4e5-f6a7-4b8c-9d0e-f1a2b3c4d5e6', 'ofek2@demo.local', 'Ofek (new)', '$2a$12$WqeSlsFmXzSru4YK23qfeuMYIUd/4ZkHLLwx0NAehm.Vbmq1MYEEa', 'he'
WHERE NOT EXISTS (SELECT 1 FROM users WHERE id = 'b2c3d4e5-f6a7-4b8c-9d0e-f1a2b3c4d5e6');

INSERT INTO users (id, email, display_name, password_hash, preferred_locale)
SELECT 'c3d4e5f6-a7b8-4c9d-0e1f-a2b3c4d5e6f7', 'yael2@demo.local', 'Yael (new)', '$2a$12$WqeSlsFmXzSru4YK23qfeuMYIUd/4ZkHLLwx0NAehm.Vbmq1MYEEa', 'he'
WHERE NOT EXISTS (SELECT 1 FROM users WHERE id = 'c3d4e5f6-a7b8-4c9d-0e1f-a2b3c4d5e6f7');

INSERT INTO users (id, email, display_name, password_hash, preferred_locale)
SELECT 'd4e5f6a7-b8c9-4d0e-1f2a-b3c4d5e6f7a8', 'viewer2@demo.local', 'Viewer (new)', '$2a$12$WqeSlsFmXzSru4YK23qfeuMYIUd/4ZkHLLwx0NAehm.Vbmq1MYEEa', 'he'
WHERE NOT EXISTS (SELECT 1 FROM users WHERE id = 'd4e5f6a7-b8c9-4d0e-1f2a-b3c4d5e6f7a8');

-- ── Step 3: Insert new-UUID space ─────────────────────────────────────────────
INSERT INTO spaces (id, name, description, owner_user_id, locale)
SELECT 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Unit Alpha', 'Demo space for local development', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'he'
WHERE NOT EXISTS (SELECT 1 FROM spaces WHERE id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9');

-- ── Step 4: Insert new-UUID people with linked_user_id ────────────────────────
INSERT INTO people (id, space_id, full_name, display_name, linked_user_id)
SELECT 'b4c5d6e7-f8a9-4b0c-1d2e-f3a4b5c6d7e8', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Ofek Israeli', 'Ofek', 'b2c3d4e5-f6a7-4b8c-9d0e-f1a2b3c4d5e6'
WHERE NOT EXISTS (SELECT 1 FROM people WHERE id = 'b4c5d6e7-f8a9-4b0c-1d2e-f3a4b5c6d7e8');

INSERT INTO people (id, space_id, full_name, display_name, linked_user_id)
SELECT 'c5d6e7f8-a9b0-4c1d-2e3f-a4b5c6d7e8f9', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Yael Cohen', 'Yael', 'c3d4e5f6-a7b8-4c9d-0e1f-a2b3c4d5e6f7'
WHERE NOT EXISTS (SELECT 1 FROM people WHERE id = 'c5d6e7f8-a9b0-4c1d-2e3f-a4b5c6d7e8f9');

INSERT INTO people (id, space_id, full_name, display_name, linked_user_id)
SELECT 'a0b1c2d3-e4f5-4a6b-7c8d-e9f0a1b2c3d4', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Admin User', 'Admin', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM people WHERE id = 'a0b1c2d3-e4f5-4a6b-7c8d-e9f0a1b2c3d4');

-- ── Step 5: Space memberships for new users ───────────────────────────────────
INSERT INTO space_memberships (space_id, user_id)
SELECT 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM space_memberships WHERE space_id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9' AND user_id = 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5');

INSERT INTO space_memberships (space_id, user_id)
SELECT 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'b2c3d4e5-f6a7-4b8c-9d0e-f1a2b3c4d5e6'
WHERE NOT EXISTS (SELECT 1 FROM space_memberships WHERE space_id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9' AND user_id = 'b2c3d4e5-f6a7-4b8c-9d0e-f1a2b3c4d5e6');

INSERT INTO space_memberships (space_id, user_id)
SELECT 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'c3d4e5f6-a7b8-4c9d-0e1f-a2b3c4d5e6f7'
WHERE NOT EXISTS (SELECT 1 FROM space_memberships WHERE space_id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9' AND user_id = 'c3d4e5f6-a7b8-4c9d-0e1f-a2b3c4d5e6f7');

INSERT INTO space_memberships (space_id, user_id)
SELECT 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'd4e5f6a7-b8c9-4d0e-1f2a-b3c4d5e6f7a8'
WHERE NOT EXISTS (SELECT 1 FROM space_memberships WHERE space_id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9' AND user_id = 'd4e5f6a7-b8c9-4d0e-1f2a-b3c4d5e6f7a8');

-- ── Step 6: Permissions for new admin user ────────────────────────────────────
INSERT INTO space_permission_grants (space_id, user_id, permission_key, granted_by_user_id)
VALUES
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'space.view',                    'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'space.admin_mode',              'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'people.manage',                 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'tasks.manage',                  'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'schedule.publish',              'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'schedule.rollback',             'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'schedule.recalculate',          'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'constraints.manage',            'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'restrictions.manage_sensitive', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'permissions.manage',            'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'),
  ('e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5', 'logs.view_sensitive',           'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5')
ON CONFLICT DO NOTHING;

-- ── Step 7: Group types with new UUIDs ───────────────────────────────────────
INSERT INTO group_types (id, space_id, name)
SELECT 'd0e1f2a3-b4c5-4d6e-7f8a-b9c0d1e2f3a4', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Squad'
WHERE NOT EXISTS (SELECT 1 FROM group_types WHERE id = 'd0e1f2a3-b4c5-4d6e-7f8a-b9c0d1e2f3a4');

INSERT INTO group_types (id, space_id, name)
SELECT 'e1f2a3b4-c5d6-4e7f-8a9b-c0d1e2f3a4b5', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Platoon'
WHERE NOT EXISTS (SELECT 1 FROM group_types WHERE id = 'e1f2a3b4-c5d6-4e7f-8a9b-c0d1e2f3a4b5');

-- ── Step 8: Groups with new UUIDs ─────────────────────────────────────────────
INSERT INTO groups (id, space_id, group_type_id, name)
SELECT 'f2a3b4c5-d6e7-4f8a-9b0c-d1e2f3a4b5c6', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'd0e1f2a3-b4c5-4d6e-7f8a-b9c0d1e2f3a4', 'Squad A'
WHERE NOT EXISTS (SELECT 1 FROM groups WHERE id = 'f2a3b4c5-d6e7-4f8a-9b0c-d1e2f3a4b5c6');

INSERT INTO groups (id, space_id, group_type_id, name)
SELECT 'a3b4c5d6-e7f8-4a9b-0c1d-e2f3a4b5c6d7', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'd0e1f2a3-b4c5-4d6e-7f8a-b9c0d1e2f3a4', 'Squad B'
WHERE NOT EXISTS (SELECT 1 FROM groups WHERE id = 'a3b4c5d6-e7f8-4a9b-0c1d-e2f3a4b5c6d7');

-- ── Step 8: Task types with new UUIDs ─────────────────────────────────────────
INSERT INTO task_types (id, space_id, name, burden_level, allows_overlap, created_by_user_id)
SELECT 'b0c1d2e3-f4a5-4b6c-7d8e-f9a0b1c2d3e4', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Post 1', 'neutral', false, 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM task_types WHERE id = 'b0c1d2e3-f4a5-4b6c-7d8e-f9a0b1c2d3e4');

INSERT INTO task_types (id, space_id, name, burden_level, allows_overlap, created_by_user_id)
SELECT 'c1d2e3f4-a5b6-4c7d-8e9f-a0b1c2d3e4f5', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Post 2', 'neutral', false, 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM task_types WHERE id = 'c1d2e3f4-a5b6-4c7d-8e9f-a0b1c2d3e4f5');

INSERT INTO task_types (id, space_id, name, burden_level, allows_overlap, created_by_user_id)
SELECT 'd2e3f4a5-b6c7-4d8e-9f0a-b1c2d3e4f5a6', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Kitchen', 'disliked', false, 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM task_types WHERE id = 'd2e3f4a5-b6c7-4d8e-9f0a-b1c2d3e4f5a6');

INSERT INTO task_types (id, space_id, name, burden_level, allows_overlap, created_by_user_id)
SELECT 'e3f4a5b6-c7d8-4e9f-0a1b-c2d3e4f5a6b7', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'War Room', 'hated', false, 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM task_types WHERE id = 'e3f4a5b6-c7d8-4e9f-0a1b-c2d3e4f5a6b7');

INSERT INTO task_types (id, space_id, name, burden_level, allows_overlap, created_by_user_id)
SELECT 'f4a5b6c7-d8e9-4f0a-1b2c-d3e4f5a6b7c8', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Patrol', 'disliked', false, 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM task_types WHERE id = 'f4a5b6c7-d8e9-4f0a-1b2c-d3e4f5a6b7c8');

INSERT INTO task_types (id, space_id, name, burden_level, allows_overlap, created_by_user_id)
SELECT 'a5b6c7d8-e9f0-4a1b-2c3d-e4f5a6b7c8d9', 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9', 'Reserve', 'favorable', false, 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5'
WHERE NOT EXISTS (SELECT 1 FROM task_types WHERE id = 'a5b6c7d8-e9f0-4a1b-2c3d-e4f5a6b7c8d9');

COMMIT;

SELECT 'Seed data fix complete.' AS result;
