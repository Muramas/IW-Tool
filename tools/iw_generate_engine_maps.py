#!/usr/bin/env python3
"""
Generate verified Idle Wizard engine maps from an ingested workspace.

Run after iw_ingest_game_export.py succeeds.

Input:
  iw_workspace_vNext/raw_files/...
  iw_workspace_vNext/verified_source_index.json

Output:
  iw_workspace_vNext/engine_maps/
    effect_names.json
    condition_names.json
    condition_factory_enums.json
    variable_classes.json
    effect_factory_calls.json
    game_context_resources.json
    data_files.json
    unresolved_report.json

Rules:
- Extract only what is present in source/data files.
- Do not infer missing formulas.
- Mark unresolved instead of guessing.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ENUM_RE = re.compile(
    r"\b(?:public|private|internal|protected)?\s*enum\s+(\w+)\s*\{(?P<body>.*?)\}",
    re.S,
)

CLASS_RE = re.compile(
    r"\b(?:public|private|internal|protected)?\s*"
    r"(?:sealed\s+|static\s+|abstract\s+|partial\s+)*"
    r"class\s+(\w+)(?:\s*:\s*([^\n{]+))?"
)

METHOD_SIG_RE = re.compile(
    r"\b(public|private|internal|protected)\s+"
    r"(static\s+)?"
    r"([\w<>\[\],\.]+)\s+"
    r"(\w+)\s*\(([^)]*)\)"
)

PROPERTY_RE = re.compile(
    r"\b(public|private|internal|protected)\s+"
    r"([\w<>\[\],\.]+)\s+"
    r"(\w+)\s*\{[^{};]*get;?[^{};]*(?:set;?)?[^{};]*\}"
)

STRING_RE = re.compile(r'"([^"\\]*(?:\\.[^"\\]*)*)"')
NEW_EFFECT_RE = re.compile(r"new\s+([A-Za-z_]\w*)\s*\(")

RESOURCE_CALL_RE = re.compile(
    r"(?:ContextAddResource|AddResource|RegisterResource|AddStatistic|SetResource|CreateResource)\s*\((.*?)\)",
    re.S,
)

CASE_RE = re.compile(r"case\s+([^:]+):")


def read(path: Path) -> str:
    return path.read_text(errors="ignore")


def find_one(raw: Path, name: str) -> Path | None:
    matches = list(raw.rglob(name))
    return matches[0] if matches else None


def parse_enum_file(path: Path | None, enum_name: str | None = None) -> dict:
    if not path or not path.exists():
        return {
            "found": False,
            "file": str(path) if path else None,
            "enums": [],
        }

    text = read(path)
    enums = []

    for match in ENUM_RE.finditer(text):
        name = match.group(1)

        if enum_name and name != enum_name:
            continue

        values = []
        next_auto = 0

        for raw_item in match.group("body").split(","):
            item = re.sub(r"//.*", "", raw_item).strip()

            if not item:
                continue

            if "=" in item:
                enum_value_name, enum_value = [
                    x.strip() for x in item.split("=", 1)
                ]

                try:
                    next_auto = int(enum_value, 0) + 1
                except Exception:
                    pass
            else:
                enum_value_name = item.strip()
                enum_value = str(next_auto)
                next_auto += 1

            if re.match(r"^[A-Za-z_]\w*$", enum_value_name):
                values.append(
                    {
                        "name": enum_value_name,
                        "value": enum_value,
                    }
                )

        enums.append(
            {
                "name": name,
                "values": values,
            }
        )

    return {
        "found": bool(enums),
        "file": str(path),
        "enums": enums,
    }


def parse_variable_classes(raw: Path) -> dict:
    result = []

    paths = []
    paths.extend(sorted(raw.rglob("Variable*.cs")))
    paths.extend(sorted(raw.rglob("ProfitVariable.cs")))

    seen = set()

    for path in paths:
        normalized = str(path).lower()

        if normalized in seen:
            continue

        seen.add(normalized)

        text = read(path)

        classes = [
            {
                "name": match.group(1),
                "inherits": (match.group(2) or "").strip(),
            }
            for match in CLASS_RE.finditer(text)
        ]

        methods = [
            {
                "visibility": match.group(1),
                "static": bool(match.group(2)),
                "return": match.group(3),
                "name": match.group(4),
                "params": match.group(5).strip(),
            }
            for match in METHOD_SIG_RE.finditer(text)
        ]

        properties = [
            {
                "visibility": match.group(1),
                "type": match.group(2),
                "name": match.group(3),
            }
            for match in PROPERTY_RE.finditer(text)
        ]

        result.append(
            {
                "file": str(path.relative_to(raw)),
                "classes": classes,
                "properties": properties,
                "methods": methods,
            }
        )

    return {
        "files": result,
    }


def parse_effect_factory(raw: Path) -> dict:
    files = []

    candidate_names = [
        "EffectFactory.cs",
        "SimpleEffect.cs",
        "CombineEffect.cs",
        "ActionEffect.cs",
    ]

    candidates = []

    for name in candidate_names:
        path = find_one(raw, name)

        if path:
            candidates.append(path)

    for path in sorted(raw.rglob("*Effect.cs")):
        if path not in candidates:
            candidates.append(path)

    for path in candidates:
        text = read(path)

        classes = [
            {
                "name": match.group(1),
                "inherits": (match.group(2) or "").strip(),
            }
            for match in CLASS_RE.finditer(text)
        ]

        methods = [
            {
                "visibility": match.group(1),
                "static": bool(match.group(2)),
                "return": match.group(3),
                "name": match.group(4),
                "params": match.group(5).strip(),
            }
            for match in METHOD_SIG_RE.finditer(text)
        ]

        constructed_effect_classes = sorted(set(NEW_EFFECT_RE.findall(text)))
        switch_cases = sorted(set(x.strip() for x in CASE_RE.findall(text)))

        files.append(
            {
                "file": str(path.relative_to(raw)),
                "classes": classes,
                "methods": methods,
                "constructs_effect_classes": constructed_effect_classes,
                "switch_cases": switch_cases,
            }
        )

    return {
        "files": files,
    }


def split_args(arg_text: str) -> list[str]:
    args = []
    current = []
    depth = 0
    in_string = False
    escaped = False

    for char in arg_text:
        if in_string:
            current.append(char)

            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False

            continue

        if char == '"':
            in_string = True
            current.append(char)
            continue

        if char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1

        if char == "," and depth == 0:
            args.append("".join(current).strip())
            current = []
        else:
            current.append(char)

    if current:
        args.append("".join(current).strip())

    return args


def parse_game_context_resources(raw: Path) -> dict:
    hits = []
    relevant = []

    for name in [
        "GameContext.cs",
        "GameManager.cs",
        "Statistic.cs",
    ]:
        path = find_one(raw, name)

        if path:
            relevant.append(path)

    relevant.extend(sorted(raw.rglob("*Manager.cs")))

    seen = set()

    for path in relevant:
        if path in seen:
            continue

        seen.add(path)

        text = read(path)

        for match in RESOURCE_CALL_RE.finditer(text):
            call = match.group(0)
            args = split_args(match.group(1))
            string_literals = STRING_RE.findall(call)
            line = text.count("\n", 0, match.start()) + 1

            hits.append(
                {
                    "file": str(path.relative_to(raw)),
                    "line": line,
                    "call": re.sub(r"\s+", " ", call).strip()[:1000],
                    "args": args,
                    "string_literals": string_literals,
                }
            )

    return {
        "resource_registration_like_calls": hits,
    }


def parse_data_files(raw: Path) -> dict:
    data = []

    paths = []
    paths.extend(sorted(list(raw.rglob("*.bytes")) + list(raw.rglob("*.bytes.txt"))))
    paths.extend(sorted(raw.rglob("*.json")))

    for path in paths:
        text = read(path)
        preview = text[:500]

        data.append(
            {
                "file": str(path.relative_to(raw)),
                "size": path.stat().st_size,
                "starts_with": preview[:120],
                "looks_json": preview.lstrip().startswith(("{", "[")),
            }
        )

    return {
        "files": data,
    }


def unresolved(effect_factory: dict, resources: dict, data_files: dict) -> dict:
    issues = []

    if not effect_factory["files"]:
        issues.append("No effect factory/effect class files found.")

    if not resources["resource_registration_like_calls"]:
        issues.append(
            "No resource registration-like calls found; resource map cannot be generated yet."
        )

    if not data_files["files"]:
        issues.append("No data files found.")

    return {
        "issues": issues,
    }


def main() -> None:
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--workspace",
        required=True,
        type=Path,
        help="iw_workspace folder produced by iw_ingest_game_export.py",
    )

    parser.add_argument(
        "--out",
        type=Path,
        default=None,
    )

    args = parser.parse_args()

    workspace = args.workspace
    raw = workspace / "raw_files"

    if not raw.exists():
        raise SystemExit(f"Missing raw_files folder: {raw}")

    output = args.out or (workspace / "engine_maps")
    output.mkdir(parents=True, exist_ok=True)

    effect_names = parse_enum_file(
        find_one(raw, "EffectNames.cs"),
        "EffectNames",
    )

    condition_names = parse_enum_file(
        find_one(raw, "ConditionNames.cs"),
        "ConditionNames",
    )

    condition_factory_path = find_one(raw, "ConditionFactory.cs")

    condition_factory_enums = (
        parse_enum_file(condition_factory_path)
        if condition_factory_path
        else {
            "found": False,
            "file": None,
            "enums": [],
        }
    )

    variables = parse_variable_classes(raw)
    effect_factory = parse_effect_factory(raw)
    resources = parse_game_context_resources(raw)
    data_files = parse_data_files(raw)
    unresolved_report = unresolved(effect_factory, resources, data_files)

    outputs = {
        "effect_names.json": effect_names,
        "condition_names.json": condition_names,
        "condition_factory_enums.json": condition_factory_enums,
        "variable_classes.json": variables,
        "effect_factory_calls.json": effect_factory,
        "game_context_resources.json": resources,
        "data_files.json": data_files,
        "unresolved_report.json": unresolved_report,
    }

    for filename, obj in outputs.items():
        (output / filename).write_text(
            json.dumps(obj, indent=2),
            encoding="utf-8",
        )

    print(f"Engine maps written to: {output}")
    print(f"EffectNames found: {effect_names.get('found')}")

    if effect_names.get("enums"):
        print(
            f"EffectNames values: "
            f"{len(effect_names['enums'][0]['values'])}"
        )

    print(f"Variable files mapped: {len(variables['files'])}")
    print(
        f"Effect/effect-factory files mapped: "
        f"{len(effect_factory['files'])}"
    )
    print(
        f"Resource registration-like calls: "
        f"{len(resources['resource_registration_like_calls'])}"
    )
    print(f"Data files mapped: {len(data_files['files'])}")

    if unresolved_report["issues"]:
        print("Issues:")

        for issue in unresolved_report["issues"]:
            print(f" - {issue}")


if __name__ == "__main__":
    main()
