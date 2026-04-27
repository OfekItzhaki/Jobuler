-- Migration 020: Fix schedule_run_status and schedule_run_trigger columns
-- Migration 019 may not have run cleanly. This migration is idempotent —
-- it checks the column type before attempting the conversion.

-- ── schedule_runs.status ─────────────────────────────────────────────────────
DO $$
BEGIN
    -- Only convert if the column is still an enum type
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'schedule_runs'
          AND column_name = 'status'
          AND udt_name = 'schedule_run_status'
    ) THEN
        ALTER TABLE schedule_runs ADD COLUMN status_text TEXT;
        UPDATE schedule_runs SET status_text = status::TEXT;
        ALTER TABLE schedule_runs DROP COLUMN status;
        ALTER TABLE schedule_runs RENAME COLUMN status_text TO status;
        ALTER TABLE schedule_runs ALTER COLUMN status SET NOT NULL;
        ALTER TABLE schedule_runs ALTER COLUMN status SET DEFAULT 'queued';
    END IF;
END $$;

-- Ensure check constraint exists (idempotent)
ALTER TABLE schedule_runs DROP CONSTRAINT IF EXISTS chk_schedule_run_status;
ALTER TABLE schedule_runs ADD CONSTRAINT chk_schedule_run_status
    CHECK (status IN ('queued', 'running', 'completed', 'failed', 'timed_out'));

-- ── schedule_runs.trigger_type ───────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'schedule_runs'
          AND column_name = 'trigger_type'
          AND udt_name = 'schedule_run_trigger'
    ) THEN
        ALTER TABLE schedule_runs ADD COLUMN trigger_type_text TEXT;
        UPDATE schedule_runs SET trigger_type_text = trigger_type::TEXT;
        ALTER TABLE schedule_runs DROP COLUMN trigger_type;
        ALTER TABLE schedule_runs RENAME COLUMN trigger_type_text TO trigger_type;
        ALTER TABLE schedule_runs ALTER COLUMN trigger_type SET NOT NULL;
        ALTER TABLE schedule_runs ALTER COLUMN trigger_type SET DEFAULT 'standard';
    END IF;
END $$;

-- Ensure check constraint exists (idempotent)
ALTER TABLE schedule_runs DROP CONSTRAINT IF EXISTS chk_schedule_run_trigger;
ALTER TABLE schedule_runs ADD CONSTRAINT chk_schedule_run_trigger
    CHECK (trigger_type IN ('standard', 'emergency', 'manual', 'rollback'));

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('020') ON CONFLICT DO NOTHING;
