-- Migration 034: Add required_qualification_names to tasks (group tasks)
-- Stores qualification names required for a shift as a text array.
-- Using names (not IDs) because qualifications are group-scoped and identified by name.
-- NULL = no qualification requirement (default).

ALTER TABLE tasks
  ADD COLUMN IF NOT EXISTS required_qualification_names text[] NOT NULL DEFAULT '{}';

COMMENT ON COLUMN tasks.required_qualification_names IS
  'Qualification names that at least one assignee per shift must hold. Empty = no requirement.';
