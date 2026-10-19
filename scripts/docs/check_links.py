#!/usr/bin/env python3
"""Check local Markdown links and required documentation translations.

This script uses the Python standard library only. Run from any directory:
    python3 scripts/docs/check_links.py
"""
from __future__ import annotations

from pathlib import Path
from urllib.parse import unquote
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
LOCALES = ("en", "hr", "sr-Cyrl")
GUIDES = ("INSTALL.md", "USER-GUIDE.md", "BUILD.md")
LINK = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
FENCE = re.compile(r"^\s*(`{3,}|~{3,})")
errors: list[str] = []

required = [ROOT / "README.md"]
required.extend(ROOT / "docs" / lang / name for lang in LOCALES for name in GUIDES)
required.extend(ROOT / "docs" / lang / "README.md" for lang in ("hr", "sr-Cyrl"))
for path in required:
    if not path.is_file():
        errors.append("Missing required document: " + str(path.relative_to(ROOT)))

checked_files = 0
checked_links = 0
for source in sorted(ROOT.rglob("*.md")):
    parts = source.relative_to(ROOT).parts
    if any(part.startswith(".") or part in {"dist", "node_modules"} for part in parts):
        continue
    checked_files += 1
    in_fence = False
    for line_no, line in enumerate(source.read_text(encoding="utf-8").splitlines(), 1):
        if FENCE.match(line):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        for match in LINK.finditer(line):
            destination = match.group(1).split("#", 1)[0].split("?", 1)[0].strip().strip("<>")
            if not destination or re.match(r"^[a-z][a-z\d+.-]*:", destination, re.I):
                continue
            checked_links += 1
            target = (source.parent / unquote(destination)).resolve()
            try:
                target.relative_to(ROOT)
            except ValueError:
                errors.append(f"{source.relative_to(ROOT)}:{line_no}: outside repository: {destination}")
                continue
            if not target.exists():
                errors.append(f"{source.relative_to(ROOT)}:{line_no}: broken link: {destination}")

for lang in LOCALES:
    for name in GUIDES:
        path = ROOT / "docs" / lang / name
        if path.exists() and "2.0.0.0" not in path.read_text(encoding="utf-8"):
            errors.append(f"Missing 2.0 release-status reference in docs/{lang}/{name}")

if errors:
    print("Documentation validation FAILED:")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print(f"Documentation validation PASS: {checked_files} Markdown files, "
      f"{checked_links} internal links, English/Croatian/Serbian guide coverage.")
