-- Enable required PostgreSQL extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";   -- trigram search for people/task names
CREATE EXTENSION IF NOT EXISTS "btree_gin"; -- composite GIN indexes
