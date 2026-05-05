-- Migration 032: Group-scoped qualifications
-- Admins define qualification types per group (driver, sniper, commander, etc.)
-- Members are then assigned qualifications from this list.

-- ── Qualification definitions (group-scoped, like space_roles) ───────────────
CREATE TABLE IF NOT EXISTS group_qualifications (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_id        UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    name            TEXT NOT NULL,
    description     TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_by_user_id UUID REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Unique name per group
CREATE UNIQUE INDEX IF NOT EXISTS idx_group_qualifications_name
    ON group_qualifications (space_id, group_id, name)
    WHERE is_active = TRUE;

CREATE INDEX IF NOT EXISTS idx_group_qualifications_group
    ON group_qualifications (group_id);

-- ── Member qualification assignments ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS member_qualifications (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_id            UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    person_id           UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    qualification_id    UUID NOT NULL REFERENCES group_qualifications(id) ON DELETE CASCADE,
    assigned_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    assigned_by_user_id UUID REFERENCES users(id),
    UNIQUE (person_id, qualification_id)
);

CREATE INDEX IF NOT EXISTS idx_member_qualifications_group
    ON member_qualifications (group_id, person_id);

-- ── RLS ───────────────────────────────────────────────────────────────────────
ALTER TABLE group_qualifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE member_qualifications ENABLE ROW LEVEL SECURITY;

CREATE POLICY group_qualifications_isolation ON group_qualifications
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY member_qualifications_isolation ON member_qualifications
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

-- ── Updated-at trigger ────────────────────────────────────────────────────────
CREATE TRIGGER trg_group_qualifications_updated_at
    BEFORE UPDATE ON group_qualifications
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Track migration
INSERT INTO schema_migrations (version) VALUES ('032') ON CONFLICT DO NOTHING;
