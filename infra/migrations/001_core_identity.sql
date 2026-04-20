-- ─────────────────────────────────────────────────────────────────────────────
-- Migration 001: Core Identity and Access
-- Tables: users, spaces, space_memberships, space_permission_grants,
--         space_roles, ownership_transfer_history
-- ─────────────────────────────────────────────────────────────────────────────

-- ─── Users ───────────────────────────────────────────────────────────────────
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email           TEXT NOT NULL UNIQUE,
    display_name    TEXT NOT NULL,
    password_hash   TEXT NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    preferred_locale TEXT NOT NULL DEFAULT 'he',  -- he | en | ru
    profile_image_url TEXT,
    last_login_at   TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_email ON users (email);

-- ─── Refresh Tokens ──────────────────────────────────────────────────────────
CREATE TABLE refresh_tokens (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash      TEXT NOT NULL UNIQUE,
    expires_at      TIMESTAMPTZ NOT NULL,
    revoked_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens (user_id);
CREATE INDEX idx_refresh_tokens_token_hash ON refresh_tokens (token_hash);

-- ─── Spaces ──────────────────────────────────────────────────────────────────
-- A space is the core multi-tenant workspace (platoon, company, site, etc.)
CREATE TABLE spaces (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            TEXT NOT NULL,
    description     TEXT,
    owner_user_id   UUID NOT NULL REFERENCES users(id),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    locale          TEXT NOT NULL DEFAULT 'he',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_spaces_owner ON spaces (owner_user_id);

-- ─── Space Memberships ───────────────────────────────────────────────────────
-- Links users to spaces. A user may belong to multiple spaces.
CREATE TABLE space_memberships (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    joined_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (space_id, user_id)
);

CREATE INDEX idx_space_memberships_space ON space_memberships (space_id);
CREATE INDEX idx_space_memberships_user  ON space_memberships (user_id);

-- ─── Space Permission Grants ─────────────────────────────────────────────────
-- Fine-grained permission grants per user per space.
-- permission_key examples: space.view, space.admin_mode, schedule.publish, etc.
CREATE TABLE space_permission_grants (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    permission_key  TEXT NOT NULL,
    granted_by_user_id UUID REFERENCES users(id),
    granted_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at      TIMESTAMPTZ,
    UNIQUE (space_id, user_id, permission_key)
);

CREATE INDEX idx_permission_grants_space_user ON space_permission_grants (space_id, user_id);

-- ─── Space Roles (Dynamic Operational Roles) ─────────────────────────────────
-- Roles are data, not hardcoded enums. Created per space by admins.
-- Examples: Soldier, Squad Commander, Medic, War Room Operator, etc.
CREATE TABLE space_roles (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id        UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    name            TEXT NOT NULL,
    description     TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_by_user_id UUID REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (space_id, name)
);

CREATE INDEX idx_space_roles_space ON space_roles (space_id);

-- ─── Ownership Transfer History ──────────────────────────────────────────────
-- Every ownership change is permanently logged here.
CREATE TABLE ownership_transfer_history (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    space_id            UUID NOT NULL REFERENCES spaces(id) ON DELETE CASCADE,
    previous_owner_id   UUID NOT NULL REFERENCES users(id),
    new_owner_id        UUID NOT NULL REFERENCES users(id),
    transferred_by_user_id UUID NOT NULL REFERENCES users(id),
    reason              TEXT,
    transferred_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_ownership_history_space ON ownership_transfer_history (space_id);

-- ─── Row-Level Security ──────────────────────────────────────────────────────
-- RLS is enabled on all tenant-scoped tables.
-- The application sets the session variable app.current_space_id before queries.
-- The app also sets app.current_user_id for user-scoped policies.

ALTER TABLE spaces                    ENABLE ROW LEVEL SECURITY;
ALTER TABLE space_memberships         ENABLE ROW LEVEL SECURITY;
ALTER TABLE space_permission_grants   ENABLE ROW LEVEL SECURITY;
ALTER TABLE space_roles               ENABLE ROW LEVEL SECURITY;
ALTER TABLE ownership_transfer_history ENABLE ROW LEVEL SECURITY;

-- Policy: users can only see spaces they are members of (or own)
CREATE POLICY spaces_isolation ON spaces
    USING (
        id = current_setting('app.current_space_id', TRUE)::UUID
        OR owner_user_id = current_setting('app.current_user_id', TRUE)::UUID
    );

-- Policy: memberships are visible only within the current space context
CREATE POLICY memberships_isolation ON space_memberships
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY permission_grants_isolation ON space_permission_grants
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY roles_isolation ON space_roles
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

CREATE POLICY ownership_history_isolation ON ownership_transfer_history
    USING (space_id = current_setting('app.current_space_id', TRUE)::UUID);

-- ─── Updated-at trigger ──────────────────────────────────────────────────────
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_spaces_updated_at
    BEFORE UPDATE ON spaces
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_space_roles_updated_at
    BEFORE UPDATE ON space_roles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
