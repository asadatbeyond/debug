#!/usr/bin/env python3
"""Read connection string bases from appsettings JSON (strip Windows auth for CI SQL login).

Mirrors NightlyBilling scripts/connection_string_base_from_testhost_config.py.
Credentials are appended in bash (gha-resolve-db-connection-strings.sh), not here.
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


def load_bases(appsettings_path: str) -> dict[str, str]:
    with open(appsettings_path, encoding="utf-8") as f:
        data = json.load(f)

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

    if not args.username or not args.password:
        print("username and password are required unless --bases-only is set.", file=sys.stderr)
        return 1

    for name, base in bases.items():
        resolved = f"{base};User ID={args.username};Password={args.password};Integrated Security=False"
        escaped = resolved.replace("'", "'\"'\"'")
        print(f"export ConnectionStrings__{name}='{escaped}'")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
