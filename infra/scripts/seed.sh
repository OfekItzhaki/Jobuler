#!/usr/bin/env bash
# Load demo seed data into the local postgres instance

set -e

PGHOST="${POSTGRES_HOST:-localhost}"
PGPORT="${POSTGRES_PORT:-5432}"
PGDB="${POSTGRES_DB:-jobuler}"
PGUSER="${POSTGRES_USER:-jobuler}"
PGPASSWORD="${POSTGRES_PASSWORD:-changeme_local}"

export PGPASSWORD

SEED_FILE="$(dirname "$0")/seed.sql"

echo "Loading seed data from $SEED_FILE"
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDB" -f "$SEED_FILE"
echo "Seed complete."
