-- Create the categories table
-- Run this against your PostgreSQL database before using the Category APIs.

CREATE TABLE IF NOT EXISTS categories (
    id          SERIAL PRIMARY KEY,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    code        VARCHAR     NOT NULL,
    name        VARCHAR     NOT NULL,
    CONSTRAINT uq_categories_code UNIQUE (code),
    CONSTRAINT uq_categories_name UNIQUE (name)
);
