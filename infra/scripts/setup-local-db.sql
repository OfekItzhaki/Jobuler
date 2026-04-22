-- Run this as the postgres superuser to set up the local dev database.
-- In your terminal: psql -U postgres -f infra/scripts/setup-local-db.sql

-- Create the app user
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'jobuler') THEN
    CREATE USER jobuler WITH PASSWORD 'changeme_local';
  END IF;
END
$$;

-- Create the database
SELECT 'CREATE DATABASE jobuler OWNER jobuler'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'jobuler')\gexec

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE jobuler TO jobuler;

\echo 'Database setup complete. Now run the migrations.'
