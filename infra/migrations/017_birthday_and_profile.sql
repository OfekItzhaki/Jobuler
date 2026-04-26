-- Migration 017: Add birthday to users and people tables

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS birthday DATE;

ALTER TABLE people
    ADD COLUMN IF NOT EXISTS birthday DATE;

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('017') ON CONFLICT DO NOTHING;
