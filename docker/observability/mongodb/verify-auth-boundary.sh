#!/usr/bin/env bash
set -Eeuo pipefail

uri="$(tr -d '\r\n' < /run/secrets/graylog_mongodb_uri)"
authenticated_user="$(mongosh --quiet --norc "$uri" --eval 'db.runCommand({ connectionStatus: 1 }).authInfo.authenticatedUsers[0].user')"

if [[ "$authenticated_user" != "graylog" ]]; then
    echo "Unexpected authenticated MongoDB user." >&2
    exit 1
fi

if mongosh --quiet --norc 'mongodb://127.0.0.1:27017/graylog' --eval 'db.getCollectionNames()' >/dev/null 2>&1; then
    echo "Anonymous MongoDB data access unexpectedly succeeded." >&2
    exit 1
fi

echo 'graylog-authenticated;anonymous-denied'
