-- ─────────────────────────────────────────────────────────────────────────────
-- Migration 003: Tasks, Task Slots, and Constraint Rules
-- ─────────────────────────────────────────────────────────────────────────────

-- ─── Task Burden Level ───────────────────────────────────────────────────────
CREATE TYPE task_burden_level AS ENUM ('favorable', 'neutral', 'disliked', 'hated');

-- ─── Task Types ──────────────────────────────────────────────────────────────
CREATE TABLE task_types (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    name                TEXT NOT NULL,
    description         TEXT,
    burden_level        task_burden_level NOT NULL DEFAULT 'neutral',
    default_priority    INT NOT NULL DEFAULT 5,
    allows_overlap      BOOLEAN NOT NULL DEFAULT FALSE,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    created_by_user_id  UUID REFERENCES users(id),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (space_id, name)
);

CREATE INDEX idx_task_types_space ON task_types (space_id);

-- ─── Task Type Overlap Rules ─────────────────────────────────────────────────
-- Explicit compatibility rules between task type pairs.
-- If no rule exists and allows_overlap is false, overlap is forbidden.
CREATE TABLE task_type_overlap_rules (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    task_type_a_id  UUID NOT NULL REFERENCES task_types(id) ON DELETE CASCADE,
    task_type_b_id  UUID NOT NULL REFERENCES task_types(id) ON DELETE CASCADE,
    overlap_allowed BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (task_type_a_id, task_type_b_id)
);

CREATE INDEX idx_overlap_rules_space ON task_type_overlap_rules (space_id);

-- ─── Task Slots ──────────────────────────────────────────────────────────────
CREATE TYPE task_slot_status AS ENUM ('active', 'cancelled', 'completed');

CREATE TABLE task_slots (
    id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id                    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    task_type_id                UUID NOT NULL REFERENCES task_types(id),
    starts_at                   TIMESTAMPTZ NOT NULL,
    ends_at                     TIMESTAMPTZ NOT NULL,
    required_headcount          INT NOT NULL DEFAULT 1,
    priority                    INT NOT NULL DEFAULT 5,
    required_role_ids_json      JSONB NOT NULL DEFAULT '[]',
    required_qualification_ids_json JSONB NOT NULL DEFAULT '[]',
    status                      task_slot_status NOT NULL DEFAULT 'active',
    location                    TEXT,
    created_by_user_id          UUID REFERENCES users(id),
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_slot_order CHECK (ends_at > starts_at)
);

CREATE INDEX idx_task_slots_space    ON task_slots (space_id);
CREATE INDEX idx_task_slots_type     ON task_slots (task_type_id);
CREATE INDEX idx_task_slots_time     ON task_slots (starts_at, ends_at);

-- ─── Constraint Rules ────────────────────────────────────────────────────────
-- Flexible constraint model supporting all scope levels and severities.
CREATE TYPE constraint_scope_type AS ENUM ('person', 'role', 'group', 'task_type', 'space');
CREATE TYPE constraint_severity   AS ENUM ('hard', 'soft');

CREATE TABLE constraint_rules (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    scope_type          constraint_scope_type NOT NULL,
    scope_id            UUID,           -- NULL when scope_type = 'space'
    severity            constraint_severity NOT NULL,
    rule_type           TEXT NOT NULL,  -- e.g. 'min_rest_hours', 'no_overlap', 'max_kitchen_per_week'
    rule_payload_json   JSONB NOT NULL DEFAULT '{}',
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    effective_from      DATE,
    effective_until     DATE,
    created_by_user_id  UUID REFERENCES users(id),
    updated_by_user_id  UUID REFERENCES users(id),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_constraints_space      ON constraint_rules (space_id);
CREATE INDEX idx_constraints_scope      ON constraint_rules (scope_type, scope_id);
CREATE INDEX idx_constraints_rule_type  ON constraint_rules (rule_type);

-- ─── RLS ─────────────────────────────────────────────────────────────────────
ALTER TABLE task_types              ENABLE ROW LEVEL SECURITY;
ALTER TABLE task_type_overlap_rules ENABLE ROW LEVEL SECURITY;
ALTER TABLE task_slots              ENABLE ROW LEVEL SECURITY;
ALTER TABLE constraint_rules        ENABLE ROW LEVEL SECURITY;

CREATE POLICY task_types_isolation ON task_types
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY overlap_rules_isolation ON task_type_overlap_rules
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY task_slots_isolation ON task_slots
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY constraints_isolation ON constraint_rules
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

-- ─── Updated-at triggers ─────────────────────────────────────────────────────
CREATE TRIGGER trg_task_types_updated_at
    BEFORE UPDATE ON task_types
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_task_slots_updated_at
    BEFORE UPDATE ON task_slots
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_constraints_updated_at
    BEFORE UPDATE ON constraint_rules
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
