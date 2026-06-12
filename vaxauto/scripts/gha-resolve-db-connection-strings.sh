#!/usr/bin/env bash
# Resolve SQL connection strings for GHA (same pattern as NightlyBilling nb-docker-tests-*.yml).
# Requires: DB_USERNAME, DB_PASSWORD, and ENV (QA or STG).
set -euo pipefail

ENV_NAME="${ENV:-STG}"
ENV_UPPER="$(echo "$ENV_NAME" | tr '[:lower:]' '[:upper:]')"
APPSETTINGS="VaxCare.Tests/appsettings.${ENV_UPPER}.json"

if [ -z "${DB_USERNAME:-}" ]; then
  echo "::error::DB_USERNAME is empty. Add it as a repository secret." >&2
  exit 1
fi
if [ -z "${DB_PASSWORD:-}" ]; then
  echo "::error::DB_PASSWORD is empty. Add it as a repository secret." >&2
  exit 1
fi

if [ ! -f "$APPSETTINGS" ]; then
  echo "::error::Missing $APPSETTINGS (committed appsettings for connection string bases)." >&2
  exit 1
fi

USER_PREVIEW="${DB_USERNAME:0:8}***"
PASS_PREVIEW="${DB_PASSWORD:0:8}***"
echo "DB user (masked): $USER_PREVIEW"
echo "DB password (masked): $PASS_PREVIEW"
echo "Building ConnectionStrings__* from $APPSETTINGS for ENV=$ENV_UPPER"

eval "$(python3 scripts/export-sql-connection-strings.py "$APPSETTINGS" "$DB_USERNAME" "$DB_PASSWORD")"

for key in Sales DataEntry Risk HealthSystems Reporting RealMed; do
  var="ConnectionStrings__${key}"
  if [ -z "${!var:-}" ]; then
    echo "::error::Failed to set $var" >&2
    exit 1
  fi
  echo "::add-mask::${!var}"
done

echo "SQL connection strings resolved (credentials masked in logs)."
