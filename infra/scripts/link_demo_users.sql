-- Link demo users to their person records
-- This enables CreateGroupCommand to find the creator's person

-- admin@demo.local → Ofek Israeli (person 50000000-...0001)
-- Note: admin user is the space owner, linked to first person
UPDATE people SET linked_user_id = '00000000-0000-0000-0000-000000000001'
WHERE id = '50000000-0000-0000-0000-000000000001' AND linked_user_id IS NULL;

-- ofek@demo.local → Ofek Israeli (same person — or create a separate one)
-- For demo purposes, link ofek user to person 50000000-...0001 as well
-- Actually each user should have their own person — let's create persons for each
INSERT INTO people (id, space_id, full_name, display_name, linked_user_id)
VALUES
  (gen_random_uuid(), '10000000-0000-0000-0000-000000000001', 'Ofek Demo', 'Ofek',
   '00000000-0000-0000-0000-000000000002'),
  (gen_random_uuid(), '10000000-0000-0000-0000-000000000001', 'Yael Demo', 'Yael',
   '00000000-0000-0000-0000-000000000003'),
  (gen_random_uuid(), '10000000-0000-0000-0000-000000000001', 'Viewer Demo', 'Viewer',
   '00000000-0000-0000-0000-000000000004')
ON CONFLICT DO NOTHING;
