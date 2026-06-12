#!/usr/bin/env bash
# Resolve SQL connection strings for GHA (same pattern as NightlyBilling nb-docker-tests-*.yml).
# Requires: DB_USERNAME, DB_PASSWORD, and ENV (QA or STG).
set -euo pipefail

ENV_NAME="${ENV:-STG}"
ENV_UPPER="$(echo "$ENV_NAME" | tr '[:lower:]' '[:upper:]')"
ENV_LOWER="$(echo "$ENV_UPPER" | tr '[:upper:]' '[:lower:]')"
APPSETTINGS="VaxCare.Tests/appsettings.${ENV_UPPER}.json"

REQUIRED_KEYS=(Sales DataEntry Risk HealthSystems Reporting RealMed)

if [ -z "${DB_USERNAME:-}" ]; then
  echo "::error::DB_USERNAME is empty or not set. Add it as a secret on GitHub Environment '${ENV_LOWER}' (or ensure the job can read environment secrets)." >&2
  exit 1
fi
if [ -z "${DB_PASSWORD:-}" ]; then
  echo "::error::DB_PASSWORD is empty or not set. Add it as a secret on GitHub Environment '${ENV_LOWER}' (or ensure the job can read environment secrets)." >&2
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

RESOLVED_LINES="$(python3 scripts/export-sql-connection-strings.py "$APPSETTINGS" --resolve)" || {
  echo "::error::Could not build connection strings from $APPSETTINGS. Ensure ConnectionStrings entries are non-empty (server/catalog only; Integrated Security is stripped in CI)." >&2
  exit 1
}

if [ -z "${RESOLVED_LINES//[[:space:]]/}" ]; then
  echo "::error::No connection strings resolved from $APPSETTINGS." >&2
  exit 1
fi

while IFS=$'\t' read -r name resolved; do
  if [ -z "${name:-}" ] || [ -z "${resolved:-}" ]; then
    continue
  fi

  export "ConnectionStrings__${name}=${resolved}"
  echo "::add-mask::${resolved}"
  echo "Connection string ${name} resolved (credentials masked)."
done <<< "$RESOLVED_LINES"

for key in "${REQUIRED_KEYS[@]}"; do
  var="ConnectionStrings__${key}"
  if [ -z "${!var:-}" ]; then
    echo "::error::Failed to set $var. Add a non-empty ConnectionStrings:${key} base in $APPSETTINGS." >&2
    exit 1
  fi
done

echo "SQL connection strings resolved (credentials masked in logs)."
