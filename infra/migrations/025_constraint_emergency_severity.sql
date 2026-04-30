-- Migration 025: Add 'emergency' severity to constraint_rules
-- Emergency constraints bypass all hard/soft constraints in the solver.

-- Drop the existing check constraint and recreate with 'emergency' included
ALTER TABLE constraint_rules
    DROP CONSTRAINT IF EXISTS chk_constraint_severity;

ALTER TABLE constraint_rules
    ADD CONSTRAINT chk_constraint_severity
        CHECK (severity IN ('hard', 'soft', 'emergency'));

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('025') ON CONFLICT DO NOTHING;
