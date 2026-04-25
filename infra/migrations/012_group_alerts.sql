-- Migration 012: Group alerts (admin-only broadcast messages per group)

CREATE TABLE IF NOT EXISTS group_alerts (
    id                   UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id             UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_id             UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    title                VARCHAR(200) NOT NULL,
    body                 TEXT NOT NULL,
    severity             VARCHAR(20) NOT NULL,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_person_id UUID NOT NULL REFERENCES people(id)
);

CREATE INDEX IF NOT EXISTS idx_group_alerts_group
    ON group_alerts (space_id, group_id, created_at DESC);
