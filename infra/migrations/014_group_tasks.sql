-- Migration 014: Group tasks (flat task entity replacing TaskType + TaskSlot for new functionality)
-- Legacy task_types and task_slots tables are RETAINED unchanged.

CREATE TABLE IF NOT EXISTS tasks (
    id                   UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id             UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_id             UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    name                 TEXT NOT NULL,
    starts_at            TIMESTAMPTZ NOT NULL,
    ends_at              TIMESTAMPTZ NOT NULL,
    duration_hours       DECIMAL(6,2) NOT NULL,
    required_headcount   INT NOT NULL DEFAULT 1,
    burden_level         VARCHAR(20) NOT NULL DEFAULT 'neutral',
    allows_double_shift  BOOLEAN NOT NULL DEFAULT FALSE,
    allows_overlap       BOOLEAN NOT NULL DEFAULT FALSE,
    is_active            BOOLEAN NOT NULL DEFAULT TRUE,
    created_by_user_id   UUID REFERENCES users(id),
    updated_by_user_id   UUID REFERENCES users(id),
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_task_ends_after_starts  CHECK (ends_at > starts_at),
    CONSTRAINT chk_task_duration_positive  CHECK (duration_hours > 0),
    CONSTRAINT chk_task_headcount_positive CHECK (required_headcount >= 1),
    CONSTRAINT chk_task_burden_level       CHECK (
        burden_level IN ('favorable', 'neutral', 'disliked', 'hated')
    )
);

-- Unique task name per group (within a space)
CREATE UNIQUE INDEX IF NOT EXISTS uq_tasks_space_group_name
    ON tasks (space_id, group_id, name)
    WHERE is_active = TRUE;

-- Fast lookup by group
CREATE INDEX IF NOT EXISTS idx_tasks_group
    ON tasks (space_id, group_id, starts_at ASC)
    WHERE is_active = TRUE;

-- Auto-update updated_at on row change
CREATE OR REPLACE TRIGGER set_tasks_updated_at
    BEFORE UPDATE ON tasks
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('014') ON CONFLICT DO NOTHING;
