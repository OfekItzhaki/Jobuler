-- Migration 033: Kitchen headcount — intentionally 1 person per shift
-- The kitchen task (מטבח) requires 1 person per shift by design.
-- This migration is a no-op / documentation record.
-- The two-shift display seen previously was caused by the 23h55m solver time
-- window limit, not a headcount misconfiguration.
-- No data changes needed.
SELECT 1; -- no-op
