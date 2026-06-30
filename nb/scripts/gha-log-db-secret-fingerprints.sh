#!/usr/bin/env bash
# Log SHA256 fingerprints for DB_USERNAME / DB_PASSWORD (compare with QaAutomation; never log values).
# Requires DB_USERNAME and DB_PASSWORD in the environment. Source after empty checks.
set -euo pipefail

echo "::add-mask::${DB_USERNAME}"
echo "::add-mask::${DB_PASSWORD}"

secret_fingerprint() {
  printf '%s' "$1" | sha256sum | awk '{print substr($1, 1, 12)}'
}

echo "DB_USERNAME fingerprint: $(secret_fingerprint "${DB_USERNAME}") (length ${#DB_USERNAME})"
echo "DB_PASSWORD fingerprint: $(secret_fingerprint "${DB_PASSWORD}") (length ${#DB_PASSWORD})"
