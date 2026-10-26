CREATE TABLE IF NOT EXISTS password_reset_tokens(
    password_reset_token_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    token_hash              VARCHAR(255) NOT NULL,
    user_id                 INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    expires_at              TIMESTAMP NOT NULL,
    used_at                 TIMESTAMP NULL,
    created_at              TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_user ON password_reset_tokens(user_id);