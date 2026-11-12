CREATE TABLE accounts (
 id text PRIMARY KEY, username text NOT NULL UNIQUE,
 salt bytea NOT NULL, password_hash bytea NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now()
);
-- One versioned aggregate keeps items, equipment, quests and currency atomic.
-- JSON is server-generated; clients never submit character documents.
CREATE TABLE characters (
 id text PRIMARY KEY, account_id text NOT NULL REFERENCES accounts(id),
 slot integer NOT NULL CHECK(slot BETWEEN 0 AND 2),
 document jsonb NOT NULL CHECK(jsonb_typeof(document)='object'),
 level integer NOT NULL CHECK(level BETWEEN 1 AND 100),
 coins integer NOT NULL CHECK(coins>=0), version bigint NOT NULL DEFAULT 0,
 updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,slot)
);
