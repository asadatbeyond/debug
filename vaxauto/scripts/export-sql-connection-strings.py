#!/usr/bin/env python3
"""Build ConnectionStrings__* exports from appsettings template + SQL login (GHA / Docker).

Mirrors NightlyBilling scripts/connection_string_base_from_testhost_config.py:
strip Integrated Security, append User ID / Password for Linux CI runners.
"""

from __future__ import annotations

import argparse
import json
import sys


def strip_windows_auth(connection_string: str) -> str:
    parts: list[str] = []
    for fragment in connection_string.split(";"):
        piece = fragment.strip()
        if not piece:
            continue
        key = piece.split("=", 1)[0].strip().lower().replace(" ", "_")
        if key in ("integrated_security", "trusted_connection"):
            continue
        if key == "authentication":
            value = piece.split("=", 1)[1].strip().lower() if "=" in piece else ""
            if any(token in value for token in ("active directory", "integrated", "windows")):
                continue
        parts.append(piece)
    return ";".join(parts)


def build_sql_connection_string(base: str, username: str, password: str) -> str:
    base = strip_windows_auth(base)
    return f"{base};User ID={username};Password={password};Integrated Security=False"


def shell_export(name: str, value: str) -> str:
  # bash-safe single-quoted export
    escaped = value.replace("'", "'\"'\"'")
    return f"export ConnectionStrings__{name}='{escaped}'"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("appsettings_path", help="Path to appsettings.{ENV}.json")
    parser.add_argument("username", help="SQL login user (from DB_USERNAME secret)")
    parser.add_argument("password", help="SQL login password (from DB_PASSWORD secret)")
    args = parser.parse_args()

    with open(args.appsettings_path, encoding="utf-8") as f:
        data = json.load(f)

    connection_strings = data.get("ConnectionStrings") or {}
    if not connection_strings:
        print("No ConnectionStrings section found.", file=sys.stderr)
        return 1

    for name, base_cs in connection_strings.items():
        if not isinstance(base_cs, str) or not base_cs.strip():
            continue
        resolved = build_sql_connection_string(base_cs, args.username, args.password)
        print(shell_export(name, resolved))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
