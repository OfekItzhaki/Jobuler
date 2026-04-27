-- Migration 021: Convert all remaining PostgreSQL enum columns to TEXT
-- Idempotent — each block checks the column type before acting.

-- ── system_logs.severity ─────────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'system_logs' AND column_name = 'severity'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE system_logs ADD COLUMN severity_text TEXT;
        UPDATE system_logs SET severity_text = severity::TEXT;
        ALTER TABLE system_logs DROP COLUMN severity;
        ALTER TABLE system_logs RENAME COLUMN severity_text TO severity;
        ALTER TABLE system_logs ALTER COLUMN severity SET NOT NULL;
    END IF;
END $$;

-- ── assignments.assignment_source ────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'assignments' AND column_name = 'assignment_source'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE assignments ADD COLUMN assignment_source_text TEXT;
        UPDATE assignments SET assignment_source_text = assignment_source::TEXT;
        ALTER TABLE assignments DROP COLUMN assignment_source;
        ALTER TABLE assignments RENAME COLUMN assignment_source_text TO assignment_source;
        ALTER TABLE assignments ALTER COLUMN assignment_source SET NOT NULL;
        ALTER TABLE assignments ALTER COLUMN assignment_source SET DEFAULT 'solver';
    END IF;
END $$;

-- ── presence_windows.state ───────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'presence_windows' AND column_name = 'state'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE presence_windows ADD COLUMN state_text TEXT;
        UPDATE presence_windows SET state_text = state::TEXT;
        ALTER TABLE presence_windows DROP COLUMN state;
        ALTER TABLE presence_windows RENAME COLUMN state_text TO state;
        ALTER TABLE presence_windows ALTER COLUMN state SET NOT NULL;
    END IF;
END $$;

-- ── task_slots.status ────────────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'task_slots' AND column_name = 'status'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE task_slots ADD COLUMN status_text TEXT;
        UPDATE task_slots SET status_text = status::TEXT;
        ALTER TABLE task_slots DROP COLUMN status;
        ALTER TABLE task_slots RENAME COLUMN status_text TO status;
        ALTER TABLE task_slots ALTER COLUMN status SET NOT NULL;
        ALTER TABLE task_slots ALTER COLUMN status SET DEFAULT 'active';
    END IF;
END $$;

-- ── task_types.burden_level ──────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'task_types' AND column_name = 'burden_level'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE task_types ADD COLUMN burden_level_text TEXT;
        UPDATE task_types SET burden_level_text = burden_level::TEXT;
        ALTER TABLE task_types DROP COLUMN burden_level;
        ALTER TABLE task_types RENAME COLUMN burden_level_text TO burden_level;
        ALTER TABLE task_types ALTER COLUMN burden_level SET NOT NULL;
        ALTER TABLE task_types ALTER COLUMN burden_level SET DEFAULT 'neutral';
    END IF;
END $$;

-- ── exports.status ───────────────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'exports' AND column_name = 'status'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE exports ADD COLUMN status_text TEXT;
        UPDATE exports SET status_text = status::TEXT;
        ALTER TABLE exports DROP COLUMN status;
        ALTER TABLE exports RENAME COLUMN status_text TO status;
        ALTER TABLE exports ALTER COLUMN status SET NOT NULL;
    END IF;
END $$;

-- ── exports.format ───────────────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'exports' AND column_name = 'format'
        AND udt_name != 'text'
    ) THEN
        ALTER TABLE exports ADD COLUMN format_text TEXT;
        UPDATE exports SET format_text = format::TEXT;
        ALTER TABLE exports DROP COLUMN format;
        ALTER TABLE exports RENAME COLUMN format_text TO format;
        ALTER TABLE exports ALTER COLUMN format SET NOT NULL;
    END IF;
END $$;

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('021') ON CONFLICT DO NOTHING;
