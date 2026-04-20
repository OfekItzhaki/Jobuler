-- ─────────────────────────────────────────────────────────────────────────────
-- Migration 005: Logs and Exports
-- Tables: audit_logs, system_logs, exports
-- ─────────────────────────────────────────────────────────────────────────────

-- ─── Audit Logs ──────────────────────────────────────────────────────────────
-- Who did what, when, in which space, against which entity.
CREATE TABLE audit_logs (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID REFERENCES spaces(id) ON DELETE SET NULL,
    actor_user_id   UUID REFERENCES users(id) ON DELETE SET NULL,
    action          TEXT NOT NULL,      -- e.g. 'publish_schedule', 'grant_permission'
    entity_type     TEXT,               -- e.g. 'schedule_version', 'person'
    entity_id       UUID,
    before_json     JSONB,
    after_json      JSONB,
    ip_address      TEXT,
    correlation_id  UUID,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_logs_space      ON audit_logs (space_id);
CREATE INDEX idx_audit_logs_actor      ON audit_logs (actor_user_id);
CREATE INDEX idx_audit_logs_action     ON audit_logs (action);
CREATE INDEX idx_audit_logs_entity     ON audit_logs (entity_type, entity_id);
CREATE INDEX idx_audit_logs_created_at ON audit_logs (created_at DESC);

-- ─── System Logs ─────────────────────────────────────────────────────────────
-- Technical/operational events: solver runs, queue events, security events.
CREATE TYPE system_log_severity AS ENUM ('info', 'warning', 'error', 'critical');

CREATE TABLE system_logs (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID REFERENCES spaces(id) ON DELETE SET NULL,
    severity        system_log_severity NOT NULL DEFAULT 'info',
    event_type      TEXT NOT NULL,      -- e.g. 'solver_completed', 'publish_failed'
    message         TEXT NOT NULL,
    details_json    JSONB,
    actor_user_id   UUID REFERENCES users(id) ON DELETE SET NULL,
    correlation_id  UUID,
    is_sensitive    BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_system_logs_space      ON system_logs (space_id);
CREATE INDEX idx_system_logs_severity   ON system_logs (severity);
CREATE INDEX idx_system_logs_event_type ON system_logs (event_type);
CREATE INDEX idx_system_logs_created_at ON system_logs (created_at DESC);

-- ─── Exports ─────────────────────────────────────────────────────────────────
CREATE TYPE export_format AS ENUM ('csv', 'pdf');
CREATE TYPE export_status AS ENUM ('pending', 'processing', 'completed', 'failed');

CREATE TABLE exports (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    schedule_version_id UUID REFERENCES schedule_versions(id),
    requested_by_user_id UUID REFERENCES users(id),
    format              export_format NOT NULL DEFAULT 'csv',
    status              export_status NOT NULL DEFAULT 'pending',
    storage_key         TEXT,           -- S3 object key
    download_url        TEXT,
    expires_at          TIMESTAMPTZ,
    error_message       TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at        TIMESTAMPTZ
);

CREATE INDEX idx_exports_space   ON exports (space_id);
CREATE INDEX idx_exports_version ON exports (schedule_version_id);

-- ─── RLS ─────────────────────────────────────────────────────────────────────
ALTER TABLE audit_logs  ENABLE ROW LEVEL SECURITY;
ALTER TABLE system_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE exports     ENABLE ROW LEVEL SECURITY;

-- Audit logs: space-scoped; NULL space_id rows (global events) are excluded from tenant view
CREATE POLICY audit_logs_isolation ON audit_logs
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

-- System logs: same isolation; sensitive logs filtered in app layer
CREATE POLICY system_logs_isolation ON system_logs
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY exports_isolation ON exports
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);
