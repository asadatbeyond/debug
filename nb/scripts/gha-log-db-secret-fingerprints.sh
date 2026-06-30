#!/usr/bin/env bash
# Log DB_USERNAME / DB_PASSWORD diagnostics for GHA (5-char prefix + fingerprint; never full values).
# Requires DB_USERNAME and DB_PASSWORD in the environment. Source after empty checks.
set -euo pipefail

prefix_preview() {
  local value="$1"
  local len="${#value}"
  if [ "$len" -eq 0 ]; then
    echo "<empty>"
  elif [ "$len" -le 5 ]; then
    echo "${value}***"
  else
    echo "${value:0:5}***"
  fi
}

# Log prefix before ::add-mask:: so compare across repos/jobs (GitHub may still redact substrings).
echo "DB_USERNAME prefix (first 5): $(prefix_preview "${DB_USERNAME}")"
echo "DB_PASSWORD prefix (first 5): $(prefix_preview "${DB_PASSWORD}")"

echo "::add-mask::${DB_USERNAME}"
echo "::add-mask::${DB_PASSWORD}"

secret_fingerprint() {
  printf '%s' "$1" | sha256sum | awk '{print substr($1, 1, 12)}'
}

echo "DB_USERNAME fingerprint: $(secret_fingerprint "${DB_USERNAME}") (length ${#DB_USERNAME})"
echo "DB_PASSWORD fingerprint: $(secret_fingerprint "${DB_PASSWORD}") (length ${#DB_PASSWORD})"
