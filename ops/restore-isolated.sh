#!/usr/bin/env bash
set -euo pipefail
# Operates only on a NEW isolated database. Never restores over the configured game DB.
archive=${1:?Usage: bash ops/restore-isolated.sh archive.dump mmorpg_restore_name}
target=${2:?New isolated database name required}
[[ "$target" =~ ^mmorpg_restore_[a-z0-9_]+$ ]] || { echo 'Use a mmorpg_restore_ prefixed database name' >&2; exit 1; }
[ -s "$archive" ] || { echo 'Archive missing/empty' >&2; exit 1; }
pg_restore --list "$archive" >/dev/null
# createdb fails if target exists; no DROP or --clean is used.
createdb -- "$target"
pg_restore --exit-on-error --no-owner --no-acl --dbname="$target" "$archive"
psql --dbname="$target" --set=ON_ERROR_STOP=1 -c 'SELECT version FROM schema_migrations ORDER BY version; SELECT count(*) AS characters FROM characters; SELECT version FROM shared_world WHERE id=1;'
printf 'Restored isolated DB %s. Validate before any connection switch.\n' "$target"
