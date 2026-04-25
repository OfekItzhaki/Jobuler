-- Enable required PostgreSQL extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";   -- trigram search for people/task names
CREATE EXTENSION IF NOT EXISTS "btree_gin"; -- composite GIN indexes

-- Migration tracking table
-- Records every migration that has been successfully applied.
-- The runner checks this table before executing each file and skips already-applied ones.
CREATE TABLE IF NOT EXISTS schema_migrations (
    version    TEXT        PRIMARY KEY,  -- filename without path, e.g. "001_core_identity.sql"
    applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
