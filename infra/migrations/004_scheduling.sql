-- ─────────────────────────────────────────────────────────────────────────────
-- Migration 004: Scheduling Domain
-- Tables: schedule_runs, schedule_versions, assignments,
--         assignment_change_summaries, fairness_counters
-- ─────────────────────────────────────────────────────────────────────────────

-- ─── Schedule Runs ───────────────────────────────────────────────────────────
CREATE TYPE schedule_run_trigger AS ENUM ('standard', 'emergency', 'manual', 'rollback');
CREATE TYPE schedule_run_status  AS ENUM ('queued', 'running', 'completed', 'failed', 'timed_out');

CREATE TABLE schedule_runs (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id                UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    trigger_type            schedule_run_trigger NOT NULL DEFAULT 'standard',
    baseline_version_id     UUID,   -- FK added after schedule_versions is created
    requested_by_user_id    UUID REFERENCES users(id),
    status                  schedule_run_status NOT NULL DEFAULT 'queued',
    solver_input_hash       TEXT,   -- SHA-256 of normalized input for deduplication
    started_at              TIMESTAMPTZ,
    finished_at             TIMESTAMPTZ,
    duration_ms             INT,
    result_summary_json     JSONB,
    error_summary           TEXT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_schedule_runs_space  ON schedule_runs (space_id);
CREATE INDEX idx_schedule_runs_status ON schedule_runs (status);

-- ─── Schedule Versions ───────────────────────────────────────────────────────
-- Immutable snapshots. Published versions are never edited in place.
CREATE TYPE schedule_version_status AS ENUM ('draft', 'published', 'rolled_back', 'archived');

CREATE TABLE schedule_versions (
    id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id                    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    version_number              INT NOT NULL,
    status                      schedule_version_status NOT NULL DEFAULT 'draft',
    baseline_version_id         UUID REFERENCES schedule_versions(id),
    source_run_id               UUID REFERENCES schedule_runs(id),
    rollback_source_version_id  UUID REFERENCES schedule_versions(id),
    created_by_user_id          UUID REFERENCES users(id),
    published_by_user_id        UUID REFERENCES users(id),
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    published_at                TIMESTAMPTZ,
    summary_json                JSONB,
    UNIQUE (space_id, version_number)
);

CREATE INDEX idx_schedule_versions_space  ON schedule_versions (space_id);
CREATE INDEX idx_schedule_versions_status ON schedule_versions (space_id, status);

-- Add FK from schedule_runs back to schedule_versions (circular, added after)
ALTER TABLE schedule_runs
    ADD CONSTRAINT fk_runs_baseline_version
    FOREIGN KEY (baseline_version_id) REFERENCES schedule_versions(id);

-- ─── Assignments ─────────────────────────────────────────────────────────────
-- Assignment rows are owned by a schedule version and are immutable once
-- the version is published.
CREATE TYPE assignment_source AS ENUM ('solver', 'override');

CREATE TABLE assignments (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    schedule_version_id     UUID NOT NULL REFERENCES schedule_versions(id) ON DELETE CASCADE,
    space_id                UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    task_slot_id            UUID NOT NULL REFERENCES task_slots(id),
    person_id               UUID NOT NULL REFERENCES people(id),
    assignment_source       assignment_source NOT NULL DEFAULT 'solver',
    change_reason_summary   TEXT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (schedule_version_id, task_slot_id, person_id)
);

CREATE INDEX idx_assignments_version ON assignments (schedule_version_id);
CREATE INDEX idx_assignments_person  ON assignments (person_id);
CREATE INDEX idx_assignments_slot    ON assignments (task_slot_id);
CREATE INDEX idx_assignments_space   ON assignments (space_id);

-- ─── Assignment Change Summaries ─────────────────────────────────────────────
-- Diff between a version and its baseline, stored for fast UI display.
CREATE TABLE assignment_change_summaries (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    version_id          UUID NOT NULL REFERENCES schedule_versions(id) ON DELETE CASCADE,
    baseline_version_id UUID REFERENCES schedule_versions(id),
    added_count         INT NOT NULL DEFAULT 0,
    removed_count       INT NOT NULL DEFAULT 0,
    changed_count       INT NOT NULL DEFAULT 0,
    stability_score     NUMERIC(18,2),
    diff_json           JSONB,  -- full diff payload for UI rendering
    computed_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_change_summaries_version ON assignment_change_summaries (version_id);
CREATE INDEX idx_change_summaries_space   ON assignment_change_summaries (space_id);

-- ─── Fairness Counters ───────────────────────────────────────────────────────
-- Rolling fairness ledger per person per space.
-- Updated after each solver run and used as input to the next run.
CREATE TABLE fairness_counters (
    id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id                    UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    person_id                   UUID NOT NULL REFERENCES people(id) ON DELETE CASCADE,
    as_of_date                  DATE NOT NULL,
    total_assignments_7d        INT NOT NULL DEFAULT 0,
    total_assignments_14d       INT NOT NULL DEFAULT 0,
    total_assignments_30d       INT NOT NULL DEFAULT 0,
    hated_tasks_7d              INT NOT NULL DEFAULT 0,
    hated_tasks_14d             INT NOT NULL DEFAULT 0,
    disliked_hated_score_7d     INT NOT NULL DEFAULT 0,
    kitchen_count_7d            INT NOT NULL DEFAULT 0,
    night_missions_7d           INT NOT NULL DEFAULT 0,
    consecutive_burden_count    INT NOT NULL DEFAULT 0,
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (space_id, person_id, as_of_date)
);

CREATE INDEX idx_fairness_space_person ON fairness_counters (space_id, person_id);
CREATE INDEX idx_fairness_date         ON fairness_counters (as_of_date);

-- ─── RLS ─────────────────────────────────────────────────────────────────────
ALTER TABLE schedule_runs               ENABLE ROW LEVEL SECURITY;
ALTER TABLE schedule_versions           ENABLE ROW LEVEL SECURITY;
ALTER TABLE assignments                 ENABLE ROW LEVEL SECURITY;
ALTER TABLE assignment_change_summaries ENABLE ROW LEVEL SECURITY;
ALTER TABLE fairness_counters           ENABLE ROW LEVEL SECURITY;

CREATE POLICY schedule_runs_isolation ON schedule_runs
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY schedule_versions_isolation ON schedule_versions
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY assignments_isolation ON assignments
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY change_summaries_isolation ON assignment_change_summaries
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY fairness_isolation ON fairness_counters
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);
