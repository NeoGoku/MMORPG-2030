#!/usr/bin/env bash
set -euo pipefail
umask 077
archive=${1:?Usage: bash ops/backup.sh /safe/path/world.dump}
if [ -e "$archive" ]; then echo 'Refusing to overwrite an existing backup' >&2; exit 1; fi
partial="$archive.partial"
if [ -e "$partial" ]; then echo "Partial backup already exists" >&2; exit 1; fi
trap 'rm -f -- "$partial"' EXIT
# pg_dump provides a consistent database snapshot; includes characters and shared-world row.
docker compose exec -T db pg_dump -U mmorpg -d mmorpg --format=custom > "$partial"
pg_restore --list "$partial" > /dev/null
mv -- "$partial" "$archive"
printf 'Created backup: %s\n' "$archive"
