-- ─────────────────────────────────────────────────────────────────────────────
-- Migration 002: Operational Data
-- Tables: group_types, groups, group_memberships, people,
--         person_role_assignments, person_qualifications,
--         availability_windows, presence_windows,
--         person_restrictions, sensitive_restriction_reasons
-- ─────────────────────────────────────────────────────────────────────────────

-- ─── Group Types ─────────────────────────────────────────────────────────────
-- Dynamic group type definitions per space (squad, unit, platoon, company, etc.)
CREATE TABLE group_types (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    name        TEXT NOT NULL,
    description TEXT,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (space_id, name)
);

CREATE INDEX idx_group_types_space ON group_types (space_id);

-- ─── Groups ──────────────────────────────────────────────────────────────────
CREATE TABLE groups (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_type_id   UUID NOT NULL REFERENCES group_types(id),
    name            TEXT NOT NULL,
    description     TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (space_id, name)
);

CREATE INDEX idx_groups_space ON groups (space_id);
CREATE INDEX idx_groups_type  ON groups (group_type_id);

-- ─── People ──────────────────────────────────────────────────────────────────
-- People are operational records within a space, separate from auth users.
-- A person may optionally be linked to a user account.
CREATE TABLE people (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    linked_user_id      UUID REFERENCES users(id),  -- optional link to auth user
    full_name           TEXT NOT NULL,
    display_name        TEXT,
    profile_image_url   TEXT,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_people_space ON people (space_id);
CREATE INDEX idx_people_name  ON people USING gin (full_name gin_trgm_ops);

-- ─── Person Role Assignments ─────────────────────────────────────────────────
CREATE TABLE person_role_assignments (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    person_id   UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    role_id     UUID NOT NULL REFERENCES space_roles(id),
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (person_id, role_id)
);

CREATE INDEX idx_person_roles_person ON person_role_assignments (person_id);
CREATE INDEX idx_person_roles_space  ON person_role_assignments (space_id);

-- ─── Group Memberships ───────────────────────────────────────────────────────
CREATE TABLE group_memberships (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    group_id    UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    person_id   UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    joined_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (group_id, person_id)
);

CREATE INDEX idx_group_memberships_group  ON group_memberships (group_id);
CREATE INDEX idx_group_memberships_person ON group_memberships (person_id);
CREATE INDEX idx_group_memberships_space  ON group_memberships (space_id);

-- ─── Person Qualifications ───────────────────────────────────────────────────
CREATE TABLE person_qualifications (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    person_id       UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    qualification   TEXT NOT NULL,
    issued_at       DATE,
    expires_at      DATE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_qualifications_person ON person_qualifications (person_id);
CREATE INDEX idx_qualifications_space  ON person_qualifications (space_id);

-- ─── Availability Windows ────────────────────────────────────────────────────
-- Explicit availability periods for a person (when they CAN be scheduled).
CREATE TABLE availability_windows (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    person_id   UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    starts_at   TIMESTAMPTZ NOT NULL,
    ends_at     TIMESTAMPTZ NOT NULL,
    note        TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_availability_order CHECK (ends_at > starts_at)
);

CREATE INDEX idx_availability_person ON availability_windows (person_id);
CREATE INDEX idx_availability_time   ON availability_windows (starts_at, ends_at);

-- ─── Presence Windows ────────────────────────────────────────────────────────
-- Manually-set presence state for a person over a time window.
-- on_mission is auto-derived from assignments; free_in_base and at_home are manual.
CREATE TYPE presence_state AS ENUM ('free_in_base', 'at_home', 'on_mission');

CREATE TABLE presence_windows (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    person_id   UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    state       presence_state NOT NULL,
    starts_at   TIMESTAMPTZ NOT NULL,
    ends_at     TIMESTAMPTZ NOT NULL,
    note        TEXT,
    is_derived  BOOLEAN NOT NULL DEFAULT FALSE,  -- TRUE = auto-derived from assignment
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_presence_order CHECK (ends_at > starts_at)
);

CREATE INDEX idx_presence_person ON presence_windows (person_id);
CREATE INDEX idx_presence_time   ON presence_windows (starts_at, ends_at);
CREATE INDEX idx_presence_space  ON presence_windows (space_id);

-- ─── Person Restrictions ─────────────────────────────────────────────────────
-- Operational restrictions visible to admins with people.manage permission.
-- Sensitive reasons are stored separately and require restrictions.manage_sensitive.
CREATE TABLE person_restrictions (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    person_id           UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    restriction_type    TEXT NOT NULL,  -- e.g. 'no_kitchen', 'no_night', 'no_task_type'
    task_type_id        UUID,           -- optional: restrict to specific task type
    effective_from      DATE NOT NULL,
    effective_until     DATE,
    operational_note    TEXT,           -- visible to normal admins
    created_by_user_id  UUID REFERENCES users(id),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_restrictions_person ON person_restrictions (person_id);
CREATE INDEX idx_restrictions_space  ON person_restrictions (space_id);

-- ─── Sensitive Restriction Reasons ───────────────────────────────────────────
-- Sensitive reasons are permission-separated from operational restrictions.
-- Requires restrictions.manage_sensitive permission to read or write.
CREATE TABLE sensitive_restriction_reasons (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    restriction_id  UUID NOT NULL REFERENCES person_restrictions(id) ON DELETE CASCADE,
    reason          TEXT NOT NULL,
    created_by_user_id UUID REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_sensitive_reasons_restriction ON sensitive_restriction_reasons (restriction_id);
CREATE INDEX idx_sensitive_reasons_space       ON sensitive_restriction_reasons (space_id);

-- ─── RLS for operational data ────────────────────────────────────────────────
ALTER TABLE group_types                   ENABLE ROW LEVEL SECURITY;
ALTER TABLE groups                        ENABLE ROW LEVEL SECURITY;
ALTER TABLE group_memberships             ENABLE ROW LEVEL SECURITY;
ALTER TABLE people                        ENABLE ROW LEVEL SECURITY;
ALTER TABLE person_role_assignments       ENABLE ROW LEVEL SECURITY;
ALTER TABLE person_qualifications         ENABLE ROW LEVEL SECURITY;
ALTER TABLE availability_windows          ENABLE ROW LEVEL SECURITY;
ALTER TABLE presence_windows              ENABLE ROW LEVEL SECURITY;
ALTER TABLE person_restrictions           ENABLE ROW LEVEL SECURITY;
ALTER TABLE sensitive_restriction_reasons ENABLE ROW LEVEL SECURITY;

CREATE POLICY group_types_isolation ON group_types
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY groups_isolation ON groups
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY group_memberships_isolation ON group_memberships
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY people_isolation ON people
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY person_roles_isolation ON person_role_assignments
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY qualifications_isolation ON person_qualifications
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY availability_isolation ON availability_windows
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY presence_isolation ON presence_windows
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY restrictions_isolation ON person_restrictions
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

-- Sensitive reasons: RLS enforces space isolation; permission check is in app layer
CREATE POLICY sensitive_reasons_isolation ON sensitive_restriction_reasons
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

-- ─── Updated-at triggers ─────────────────────────────────────────────────────
CREATE TRIGGER trg_groups_updated_at
    BEFORE UPDATE ON groups
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_people_updated_at
    BEFORE UPDATE ON people
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_restrictions_updated_at
    BEFORE UPDATE ON person_restrictions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_sensitive_reasons_updated_at
    BEFORE UPDATE ON sensitive_restriction_reasons
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
