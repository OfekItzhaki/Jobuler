#!/usr/bin/env bash
# Run all pending migrations in order against the local postgres instance.
# Already-applied migrations are skipped using the schema_migrations tracking table.

set -e

PGHOST="${POSTGRES_HOST:-localhost}"
PGPORT="${POSTGRES_PORT:-5432}"
PGDB="${POSTGRES_DB:-jobuler}"
PGUSER="${POSTGRES_USER:-jobuler}"
PGPASSWORD="${POSTGRES_PASSWORD:-changeme_local}"

export PGPASSWORD

MIGRATIONS_DIR="$(dirname "$0")/../migrations"

echo "Running migrations from $MIGRATIONS_DIR"
echo "Connecting to $PGHOST:$PGPORT/$PGDB as $PGUSER"
echo ""

# Ensure the tracking table exists (idempotent — safe to run every time)
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDB" -c "
CREATE TABLE IF NOT EXISTS schema_migrations (
    version    TEXT        PRIMARY KEY,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
);" > /dev/null

applied=0
skipped=0

for file in "$MIGRATIONS_DIR"/*.sql; do
  version="$(basename "$file")"

  # Check if this migration has already been applied
  already_applied=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDB" -tAc \
    "SELECT COUNT(*) FROM schema_migrations WHERE version = '$version';")

  if [ "$already_applied" -gt "0" ]; then
    echo "  ✓ $version (already applied, skipping)"
    skipped=$((skipped + 1))
    continue
  fi

  echo "  → $version"
  psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDB" -f "$file"

  # Record successful application
  psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDB" -c \
    "INSERT INTO schema_migrations (version) VALUES ('$version') ON CONFLICT DO NOTHING;" > /dev/null

  applied=$((applied + 1))
done

echo ""
echo "Migrations complete. Applied: $applied, Skipped: $skipped"
