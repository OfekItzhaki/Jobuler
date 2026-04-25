-- Migration 010: Group messages (per-group admin alerts/announcements)

CREATE TABLE IF NOT EXISTS group_messages (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_id    UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    author_user_id UUID NOT NULL REFERENCES users(id),
    content     TEXT NOT NULL,
    is_pinned   BOOLEAN NOT NULL DEFAULT false,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_group_messages_group ON group_messages (group_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_group_messages_space ON group_messages (space_id);
