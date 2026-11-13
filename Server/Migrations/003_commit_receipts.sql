-- Receipt is inserted in the SAME transaction as character/shared-world changes.
-- No TTL: a retained receipt is a durable recovery fact, not an action to replay.
CREATE TABLE IF NOT EXISTS commit_receipts (
 operation_id text PRIMARY KEY CHECK(length(operation_id)=64),
 account_ids text[] NOT NULL,
 committed_at timestamptz NOT NULL DEFAULT now()
);
