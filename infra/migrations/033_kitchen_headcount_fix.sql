-- Migration 033: Fix kitchen task required_headcount to 2
-- The kitchen task (מטבח) should require 2 people per shift, not 1.
-- Uses byte-safe matching to avoid encoding issues on Windows terminals.
-- Targets only the seed space kitchen task by matching the Hebrew name bytes.

UPDATE tasks
SET    required_headcount = 2
WHERE  encode(name::bytea, 'escape') = '\327\236\327\230\327\221\327\227'
AND    required_headcount = 1;

-- Fallback: also match ASCII 'kitchen' for non-Hebrew seed data
UPDATE tasks
SET    required_headcount = 2
WHERE  LOWER(name) = 'kitchen'
AND    required_headcount = 1;
