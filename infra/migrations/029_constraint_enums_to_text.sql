-- Migration 029: Convert constraint_rules enum columns to TEXT
-- constraint_scope_type and constraint_severity were missed in migrations 019/021.
-- EF Core ValueConverter requires TEXT columns, not native PG enum types.

-- ── constraint_rules.scope_type ──────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'constraint_rules' AND column_name = 'scope_type'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE constraint_rules ADD COLUMN scope_type_text TEXT;
        UPDATE constraint_rules SET scope_type_text = scope_type::TEXT;
        ALTER TABLE constraint_rules DROP COLUMN scope_type;
        ALTER TABLE constraint_rules RENAME COLUMN scope_type_text TO scope_type;
        ALTER TABLE constraint_rules ALTER COLUMN scope_type SET NOT NULL;
        ALTER TABLE constraint_rules ADD CONSTRAINT chk_constraint_scope_type
            CHECK (scope_type IN ('person', 'role', 'group', 'task_type', 'space'));
    END IF;
END $$;

-- ── constraint_rules.severity ────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'constraint_rules' AND column_name = 'severity'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE constraint_rules ADD COLUMN severity_text TEXT;
        UPDATE constraint_rules SET severity_text = severity::TEXT;
        ALTER TABLE constraint_rules DROP COLUMN severity;
        ALTER TABLE constraint_rules RENAME COLUMN severity_text TO severity;
        ALTER TABLE constraint_rules ALTER COLUMN severity SET NOT NULL;
        -- Drop old enum-based check if it exists, then add text-based one
        ALTER TABLE constraint_rules DROP CONSTRAINT IF EXISTS chk_constraint_severity;
        ALTER TABLE constraint_rules ADD CONSTRAINT chk_constraint_severity
            CHECK (severity IN ('hard', 'soft', 'emergency'));
    END IF;
END $$;

-- Drop the now-unused enum types (safe — no other tables use them)
DROP TYPE IF EXISTS constraint_scope_type;
DROP TYPE IF EXISTS constraint_severity;

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('029') ON CONFLICT DO NOTHING;
