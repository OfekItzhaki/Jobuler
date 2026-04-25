-- Swap emails: rename old-UUID users to legacy emails, give canonical emails to new-UUID users
-- This makes admin@demo.local / ofek@demo.local / yael@demo.local / viewer@demo.local
-- point to the new-UUID users that have proper linked_user_id on their people records.

BEGIN;

-- Step 1: Rename old-UUID users to legacy emails (free up canonical emails)
UPDATE users SET email = 'admin_legacy@demo.local'  WHERE id = '00000000-0000-0000-0000-000000000001';
UPDATE users SET email = 'ofek_legacy@demo.local'   WHERE id = '00000000-0000-0000-0000-000000000002';
UPDATE users SET email = 'yael_legacy@demo.local'   WHERE id = '00000000-0000-0000-0000-000000000003';
UPDATE users SET email = 'viewer_legacy@demo.local' WHERE id = '00000000-0000-0000-0000-000000000004';

-- Step 2: Give canonical emails to new-UUID users
UPDATE users SET email = 'admin@demo.local'  WHERE id = 'a1b2c3d4-e5f6-4a7b-8c9d-e0f1a2b3c4d5';
UPDATE users SET email = 'ofek@demo.local'   WHERE id = 'b2c3d4e5-f6a7-4b8c-9d0e-f1a2b3c4d5e6';
UPDATE users SET email = 'yael@demo.local'   WHERE id = 'c3d4e5f6-a7b8-4c9d-0e1f-a2b3c4d5e6f7';
UPDATE users SET email = 'viewer@demo.local' WHERE id = 'd4e5f6a7-b8c9-4d0e-1f2a-b3c4d5e6f7a8';

COMMIT;

SELECT id, email, display_name FROM users ORDER BY email;
