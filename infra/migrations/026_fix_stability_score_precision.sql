-- Migration 026: Fix stability_score column precision
-- NUMERIC(5,2) overflows when the solver returns large penalty values (e.g. 2100+).
-- Widen to NUMERIC(18,2) to accommodate real-world solver outputs.

ALTER TABLE assignment_change_summaries
    ALTER COLUMN stability_score TYPE NUMERIC(18,2);
