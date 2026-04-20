#!/usr/bin/env bash
# Run all migrations in order against the local postgres container

set -e

PGHOST="${POSTGRES_HOST:-localhost}"
PGPORT="${POSTGRES_PORT:-5432}"
PGDB="${POSTGRES_DB:-jobuler}"
PGUSER="${POSTGRES_USER:-jobuler}"
PGPASSWORD="${POSTGRES_PASSWORD:-changeme_local}"

export PGPASSWORD

MIGRATIONS_DIR="$(dirname "$0")/../migrations"

echo "Running migrations from $MIGRATIONS_DIR"

for file in "$MIGRATIONS_DIR"/*.sql; do
  echo "  → $file"
  psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDB" -f "$file"
done

echo "Migrations complete."
