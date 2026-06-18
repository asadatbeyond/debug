#!/usr/bin/env python3
"""Read connection string bases from appsettings JSON (strip Windows auth for CI SQL login).

Mirrors NightlyBilling scripts/connection_string_base_from_testhost_config.py.
Credentials are appended in bash (gha-resolve-db-connection-strings.sh), not here.
"""

from __future__ import annotations

import argparse
import json
import os
import sys


def quote_connection_string_value(value: str, *, always_quote: bool = False) -> str:
    """Quote values so ';' inside passwords does not break SqlClient parsing."""
    if not value:
        return value
    if always_quote or any(char in value for char in (";", "=", '"')):
        return '"' + value.replace('"', '""') + '"'
    return value


def build_sql_connection_string(base: str, username: str, password: str) -> str:
    user = quote_connection_string_value(username)
    # Always quote password — common CI passwords contain ';' or get truncated otherwise.
    pwd = quote_connection_string_value(password, always_quote=True)
    return f"{base};User ID={user};Password={pwd};Integrated Security=False"


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


def load_bases(appsettings_path: str) -> dict[str, str]:
    raw = open(appsettings_path, encoding="utf-8").read().strip()
    if not raw:
        print(f"{appsettings_path} is empty.", file=sys.stderr)
        return {}

    data = json.loads(raw)

    connection_strings = data.get("ConnectionStrings") or {}
    bases: dict[str, str] = {}
    for name, base_cs in connection_strings.items():
        if not isinstance(base_cs, str) or not base_cs.strip():
            continue
        bases[name] = strip_windows_auth(base_cs)
    return bases


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("appsettings_path", help="Path to appsettings.{ENV}.json")
    parser.add_argument(
        "--bases-only",
        action="store_true",
        help="Print name<TAB>stripped_base per line (no credentials; for GHA bash assembly).",
    )
    parser.add_argument(
        "--resolve",
        action="store_true",
        help="Print name<TAB>full_connection_string using DB_USERNAME/DB_PASSWORD env vars.",
    )
    parser.add_argument("username", nargs="?", help="SQL login (local only; not used with --bases-only)")
    parser.add_argument("password", nargs="?", help="SQL password (local only; not used with --bases-only)")
    args = parser.parse_args()

    bases = load_bases(args.appsettings_path)
    if not bases:
        print(
            f"No non-empty ConnectionStrings found in {args.appsettings_path}.",
            file=sys.stderr,
        )
        return 1

    if args.bases_only:
        for name, base in bases.items():
            print(f"{name}\t{base}")
        return 0

    if args.resolve:
        username = os.environ.get("DB_USERNAME", "")
        password = os.environ.get("DB_PASSWORD", "")
        if not username or not password:
            print("DB_USERNAME and DB_PASSWORD must be set for --resolve.", file=sys.stderr)
            return 1
        for name, base in bases.items():
            print(f"{name}\t{build_sql_connection_string(base, username, password)}")
        return 0

    if not args.username or not args.password:
        print("username and password are required unless --bases-only is set.", file=sys.stderr)
        return 1

    for name, base in bases.items():
        resolved = build_sql_connection_string(base, args.username, args.password)
        escaped = resolved.replace("'", "'\"'\"'")
        print(f"export ConnectionStrings__{name}='{escaped}'")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
