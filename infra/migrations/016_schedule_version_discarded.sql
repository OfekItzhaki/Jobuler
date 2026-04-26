-- Migration 016: Add 'discarded' to schedule_version_status enum
-- Also ensures the tasks table exists (idempotent)

ALTER TYPE schedule_version_status ADD VALUE IF NOT EXISTS 'discarded';

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('016') ON CONFLICT DO NOTHING;
