-- Migration 024: Add daily time window to tasks table
-- Allows tasks to be restricted to a specific time-of-day window each day.
-- When NULL, the task runs 24/7 (no daily restriction).
-- When set, the solver only generates shifts within [daily_start_time, daily_end_time].

ALTER TABLE tasks
    ADD COLUMN IF NOT EXISTS daily_start_time TIME,
    ADD COLUMN IF NOT EXISTS daily_end_time   TIME;

-- Enforce: both must be set together, and end must be after start
ALTER TABLE tasks
    ADD CONSTRAINT chk_task_daily_window_both_or_neither
        CHECK (
            (daily_start_time IS NULL AND daily_end_time IS NULL)
            OR
            (daily_start_time IS NOT NULL AND daily_end_time IS NOT NULL AND daily_end_time > daily_start_time)
        );

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('024') ON CONFLICT DO NOTHING;
