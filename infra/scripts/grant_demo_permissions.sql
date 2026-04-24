-- Grant permissions to demo users
-- ofek and yael get space.view + people.manage so they can create groups
-- dana gets space.view so she can log in and be found by add-by-email

INSERT INTO space_permission_grants (space_id, user_id, permission_key, granted_by_user_id)
VALUES
  -- ofek: space.view + people.manage
  ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'space.view',    '00000000-0000-0000-0000-000000000001'),
  ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'people.manage', '00000000-0000-0000-0000-000000000001'),
  -- yael: space.view + people.manage
  ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000003', 'space.view',    '00000000-0000-0000-0000-000000000001'),
  ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000003', 'people.manage', '00000000-0000-0000-0000-000000000001'),
  -- dana: space.view only
  ('10000000-0000-0000-0000-000000000001', 'f0a1b2c3-d4e5-4f6a-7b8c-9d0e1f2a3b4c', 'space.view',   '00000000-0000-0000-0000-000000000001')
ON CONFLICT DO NOTHING;

-- Also add space_memberships for dana (the seed re-run failed for her)
INSERT INTO space_memberships (space_id, user_id)
VALUES ('10000000-0000-0000-0000-000000000001', 'f0a1b2c3-d4e5-4f6a-7b8c-9d0e1f2a3b4c')
ON CONFLICT DO NOTHING;
