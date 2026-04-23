-- Migration 007: Make group_type_id nullable
-- Groups no longer require a type — they are free-form containers.

ALTER TABLE groups
    ALTER COLUMN group_type_id DROP NOT NULL;

-- Drop the index that referenced group_type_id (it's still useful but optional)
-- Keep it — partial index would be better but this is fine for now.
