-- Create the sub_categories table
-- Run this against your PostgreSQL database before using the SubCategory APIs.

CREATE TABLE IF NOT EXISTS sub_categories (
    id          SERIAL PRIMARY KEY,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    code        VARCHAR     NOT NULL,
    name        VARCHAR     NOT NULL,
    category_id INT         NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    CONSTRAINT uq_sub_categories_code UNIQUE (code),
    CONSTRAINT uq_sub_categories_name UNIQUE (name)
);

CREATE INDEX IF NOT EXISTS ix_sub_categories_category_id ON sub_categories(category_id);
