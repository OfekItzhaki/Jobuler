-- Migration 010: Add phone_number to users and people

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS phone_number TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS uq_users_phone_number
    ON users (phone_number)
    WHERE phone_number IS NOT NULL;

ALTER TABLE people
    ADD COLUMN IF NOT EXISTS phone_number TEXT;

CREATE INDEX IF NOT EXISTS idx_people_phone_number
    ON people (phone_number)
    WHERE phone_number IS NOT NULL;
