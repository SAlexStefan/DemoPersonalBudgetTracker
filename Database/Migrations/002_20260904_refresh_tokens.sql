CREATE TABLE IF NOT EXISTS refresh_tokens(
    refresh_token_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id            UUID NOT NULL DEFAULT gen_random_uuid(),
    token_hash            VARCHAR(255) NOT NULL,
    user_id                INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    expires_at            TIMESTAMP NOT NULL,
    revoked_at            TIMESTAMP NULL,
    replaced_by_public_id UUID NULL,
    created_at            TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_public_id ON refresh_tokens(public_id);