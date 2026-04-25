-- Fix group ownership: assign admin person as owner of new-UUID groups that have no owner
-- Also add admin as member of old-UUID groups so they can manage them

BEGIN;

-- New-UUID Squad A (f2a3b4c5-...) — add admin as owner member
INSERT INTO group_memberships (id, space_id, group_id, person_id, is_owner, joined_at)
SELECT uuid_generate_v4(),
       'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9',
       'f2a3b4c5-d6e7-4f8a-9b0c-d1e2f3a4b5c6',
       'a0b1c2d3-e4f5-4a6b-7c8d-e9f0a1b2c3d4',
       true,
       NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM group_memberships
    WHERE group_id = 'f2a3b4c5-d6e7-4f8a-9b0c-d1e2f3a4b5c6'
      AND person_id = 'a0b1c2d3-e4f5-4a6b-7c8d-e9f0a1b2c3d4'
);

-- New-UUID Squad B (a3b4c5d6-...) — add admin as owner member
INSERT INTO group_memberships (id, space_id, group_id, person_id, is_owner, joined_at)
SELECT uuid_generate_v4(),
       'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9',
       'a3b4c5d6-e7f8-4a9b-0c1d-e2f3a4b5c6d7',
       'a0b1c2d3-e4f5-4a6b-7c8d-e9f0a1b2c3d4',
       true,
       NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM group_memberships
    WHERE group_id = 'a3b4c5d6-e7f8-4a9b-0c1d-e2f3a4b5c6d7'
      AND person_id = 'a0b1c2d3-e4f5-4a6b-7c8d-e9f0a1b2c3d4'
);

-- For any other groups with no owner at all, assign admin as owner
-- (covers user-created groups like f14c92e0-...)
INSERT INTO group_memberships (id, space_id, group_id, person_id, is_owner, joined_at)
SELECT uuid_generate_v4(),
       g.space_id,
       g.id,
       'a0b1c2d3-e4f5-4a6b-7c8d-e9f0a1b2c3d4',
       true,
       NOW()
FROM groups g
WHERE g.space_id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9'
  AND g.deleted_at IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM group_memberships gm
      WHERE gm.group_id = g.id AND gm.is_owner = true
  );

COMMIT;

SELECT g.name, p.full_name as owner_name, p.linked_user_id
FROM groups g
JOIN group_memberships gm ON gm.group_id = g.id AND gm.is_owner = true
JOIN people p ON p.id = gm.person_id
WHERE g.space_id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9'
  AND g.deleted_at IS NULL
ORDER BY g.name;
