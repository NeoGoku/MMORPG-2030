CREATE TABLE IF NOT EXISTS shared_world (
 id integer PRIMARY KEY CHECK(id=1),
 version bigint NOT NULL DEFAULT 0,
 document jsonb NOT NULL DEFAULT '{}'::jsonb,
 updated_at timestamptz NOT NULL DEFAULT now()
);
INSERT INTO shared_world(id,document) VALUES(1,'{}'::jsonb) ON CONFLICT DO NOTHING;
