#!/usr/bin/env bash
set -Eeuo pipefail

secret_file=/run/secrets/graylog_mongodb_password
if [[ ! -s "$secret_file" ]]; then
    echo "Required Graylog MongoDB password secret is missing or empty." >&2
    exit 1
fi

export GRAYLOG_MONGODB_PASSWORD
GRAYLOG_MONGODB_PASSWORD="$(tr -d '\r\n' < "$secret_file")"

mongosh --quiet --norc --eval '
const graylog = db.getSiblingDB("graylog");
if (graylog.getUser("graylog") === null) {
    graylog.createUser({
        user: "graylog",
        pwd: process.env.GRAYLOG_MONGODB_PASSWORD,
        roles: [
            { role: "dbOwner", db: "graylog" },
            { role: "clusterMonitor", db: "admin" }
        ]
    });
}
'

unset GRAYLOG_MONGODB_PASSWORD
