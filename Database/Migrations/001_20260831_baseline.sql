--Run against database on creation

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE IF NOT EXISTS users(
    user_id         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id       UUID NOT NULL DEFAULT gen_random_uuid(),
    email           CITEXT UNIQUE NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,
    created_at      TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);
CREATE TABLE IF NOT EXISTS categories(
    category_id     INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id       UUID NOT NULL DEFAULT gen_random_uuid(),
    name            VARCHAR(255) NOT NULL,
    type            VARCHAR(50) NOT NULL CHECK (type IN ('income', 'expense')),
    is_default      BOOLEAN NOT NULL DEFAULT FALSE,
    user_id         INT NULL REFERENCES users(user_id) ON DELETE CASCADE, -- null for the default values
    created_at      TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);
CREATE TABLE IF NOT EXISTS transactions(
    transaction_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id           UUID NOT NULL DEFAULT gen_random_uuid(),
    amount              DECIMAL(18, 2) NOT NULL CHECK(amount> 0),
    transaction_date    TIMESTAMP NOT NULL,
    note                VARCHAR(500) NULL,
    category_id         INT NOT NULL REFERENCES categories(category_id) ON DELETE RESTRICT,
    user_id             INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    created_at          TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);
CREATE INDEX IF NOT EXISTS idx_transactions_user_date ON transactions(user_id, created_at);
CREATE INDEX IF NOT EXISTS idx_categories_user ON categories(user_id);

--insert default values
INSERT  INTO categories(name, type, is_default, user_id)
SELECT  *
FROM    (VALUES
        ('Salary', 'income', true, NULL::INT),
        ('Other income', 'income', true, NULL::INT),
        ('Groceries', 'expense', true, NULL::INT),
        ('Rent', 'expense', true, NULL::INT),
        ('Utilities', 'expense', true, NULL::INT),
        ('Transport', 'expense', true, NULL::INT),
        ('Entertainment', 'expense', true, NULL::INT),
        ('Other Expense', 'expense', true, NULL::INT)
        ) AS s(name, type, is_default, user_id)
WHERE   NOT EXISTS (SELECT 1 FROM categories c WHERE c.name = s.name AND c.user_id IS NULL);

