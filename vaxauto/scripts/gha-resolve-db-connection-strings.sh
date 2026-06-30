#!/usr/bin/env bash
# Resolve SQL connection strings for GHA (same pattern as NightlyBilling nb-docker-tests-*.yml).
# Requires DB_USERNAME_{ENV} / DB_PASSWORD_{ENV} (e.g. DB_USERNAME_STG) or legacy DB_USERNAME / DB_PASSWORD.
set -euo pipefail

ENV_NAME="${ENV:-STG}"
ENV_UPPER="$(echo "$ENV_NAME" | tr '[:lower:]' '[:upper:]')"
ENV_LOWER="$(echo "$ENV_UPPER" | tr '[:upper:]' '[:lower:]')"
APPSETTINGS="VaxCare.Tests/appsettings.${ENV_UPPER}.json"

REQUIRED_KEYS=(Sales DataEntry Risk HealthSystems Reporting RealMed)

USERNAME_VAR="DB_USERNAME_${ENV_UPPER}"
PASSWORD_VAR="DB_PASSWORD_${ENV_UPPER}"

# shellcheck disable=SC2153
DB_USERNAME="${!USERNAME_VAR:-${DB_USERNAME:-}}"
# shellcheck disable=SC2153
DB_PASSWORD="${!PASSWORD_VAR:-${DB_PASSWORD:-}}"

export DB_USERNAME DB_PASSWORD

if [ -z "${DB_USERNAME}" ]; then
  echo "::error::${USERNAME_VAR} is empty or not set. Add it as a secret (e.g. GitHub secret ${USERNAME_VAR}) for environment '${ENV_LOWER}'." >&2
  exit 1
fi
if [ -z "${DB_PASSWORD}" ]; then
  echo "::error::${PASSWORD_VAR} is empty or not set. Add it as a secret (e.g. GitHub secret ${PASSWORD_VAR}) for environment '${ENV_LOWER}'." >&2
  exit 1
fi

if [ ! -f "$APPSETTINGS" ]; then
  echo "::error::Missing $APPSETTINGS (committed appsettings for connection string bases)." >&2
  exit 1
fi

echo "::add-mask::${DB_USERNAME}"
echo "::add-mask::${DB_PASSWORD}"

secret_fingerprint() {
  printf '%s' "$1" | sha256sum | awk '{print substr($1, 1, 12)}'
}
echo "${USERNAME_VAR} fingerprint: $(secret_fingerprint "${DB_USERNAME}") (length ${#DB_USERNAME})"
echo "${PASSWORD_VAR} fingerprint: $(secret_fingerprint "${DB_PASSWORD}") (length ${#DB_PASSWORD})"

echo "Building ConnectionStrings__* from $APPSETTINGS for ENV=$ENV_UPPER"

BASE_LINES="$(python3 scripts/export-sql-connection-strings.py "$APPSETTINGS" --bases-only)" || {
  echo "::error::Could not read connection string bases from $APPSETTINGS. Ensure ConnectionStrings entries are non-empty." >&2
  exit 1
}

if [ -z "${BASE_LINES//[[:space:]]/}" ]; then
  echo "::error::No connection string bases found in $APPSETTINGS." >&2
  exit 1
fi

# Append SQL credentials in bash — same as NightlyBilling nb-docker-tests-*.yml (not Python --resolve).
while IFS=$'\t' read -r name base; do
  if [ -z "${name:-}" ] || [ -z "${base:-}" ]; then
    continue
  fi

  resolved="${base};User ID=${DB_USERNAME};Password=${DB_PASSWORD};Integrated Security=False"
  export "ConnectionStrings__${name}=${resolved}"
  echo "::add-mask::${resolved}"
  echo "Connection string ${name} resolved (credentials masked)."
done <<< "$BASE_LINES"

# NightlyBilling uses CONNECTIONSTRINGS__{ENV} for DataEntry — mirror for smoke tests / cross-repo parity.
if [ -n "${ConnectionStrings__DataEntry:-}" ]; then
  export "CONNECTIONSTRINGS__${ENV_UPPER}=${ConnectionStrings__DataEntry}"
  echo "::add-mask::${ConnectionStrings__DataEntry}"
  echo "CONNECTIONSTRINGS__${ENV_UPPER} set from DataEntry (NightlyBilling-compatible env key)."
fi

for key in "${REQUIRED_KEYS[@]}"; do
  var="ConnectionStrings__${key}"
  if [ -z "${!var:-}" ]; then
    echo "::error::Failed to set $var. Add a non-empty ConnectionStrings:${key} base in $APPSETTINGS." >&2
    exit 1
  fi
done

echo "SQL connection strings resolved (credentials masked in logs)."
