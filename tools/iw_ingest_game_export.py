#!/usr/bin/env python3
"""
Ingest an Idle Wizard extracted/decompiled game folder directly.

This replaces the temporary renamed-ZIP workflow. Point it at the game export root and it will:
- discover files from a manifest of wildcard patterns,
- copy only relevant files into a stable workspace,
- generate verified_source_index.json,
- generate missing_expected_patterns.json,
- generate source_bundle.txt for model review if needed.

No manual searching. No renamed zips. No guessing about object-member paths.
"""
from __future__ import annotations
import argparse, json, re, shutil, hashlib
from pathlib import Path, PurePath

CLASS_RE = re.compile(r"\b(?:public|private|internal|protected)?\s*(?:sealed\s+|static\s+|abstract\s+|partial\s+)*class\s+(\w+)")
STRUCT_RE = re.compile(r"\b(?:public|private|internal|protected)?\s*(?:readonly\s+|partial\s+)*struct\s+(\w+)")
INTERFACE_RE = re.compile(r"\b(?:public|private|internal|protected)?\s*interface\s+(\w+)")
ENUM_RE = re.compile(r"\b(?:public|private|internal|protected)?\s*enum\s+(\w+)\s*\{(?P<body>.*?)\}", re.S)
METHOD_RE = re.compile(r"\b(?:public|private|internal|protected)\s+(?:static\s+)?(?:[\w<>\[\],]+)\s+(\w+)\s*\([^;{}]*\)\s*\{")
RESOURCE_STRING_RE = re.compile(r'"([A-Za-z0-9_.:/\- ]{3,100})"')


def load_manifest(path: Path) -> dict:
    return json.loads(path.read_text(encoding='utf-8'))


def rel_match(path: Path, pattern: str, root: Path) -> bool:
    try:
        rel = path.relative_to(root).as_posix()
    except ValueError:
        return False
    return PurePath(rel).match(pattern.replace('\\','/'))


def ignored(path: Path, root: Path, ignore_patterns: list[str]) -> bool:
    return any(rel_match(path, pat, root) for pat in ignore_patterns)


def discover(root: Path, patterns: list[str], ignore_patterns: list[str]) -> tuple[list[Path], list[str]]:
    matches=[]
    missing=[]
    for pat in patterns:
        found=[p for p in root.glob(pat) if p.is_file() and not ignored(p, root, ignore_patterns)]
        if not found:
            missing.append(pat)
        matches.extend(found)
    # de-dupe preserving order
    seen=set(); out=[]
    for p in matches:
        rp=str(p.resolve()).lower()
        if rp not in seen:
            seen.add(rp); out.append(p)
    return sorted(out), missing


def enum_values(body: str) -> list[dict]:
    vals=[]
    for raw in body.split(','):
        item=re.sub(r"//.*", "", raw).strip()
        if not item: continue
        if '=' in item:
            name, val = [x.strip() for x in item.split('=',1)]
        else:
            name, val = item, None
        if re.match(r"^[A-Za-z_]\w*$", name): vals.append({'name': name, 'value': val})
    return vals


def sha256(path: Path) -> str:
    h=hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1024*1024), b''):
            h.update(chunk)
    return h.hexdigest()


def scan_source_file(src: Path, root: Path) -> dict:
    text=src.read_text(errors='ignore')
    enums=[]
    for m in ENUM_RE.finditer(text):
        enums.append({'name':m.group(1), 'values':enum_values(m.group('body'))})
    return {
        'path': src.relative_to(root).as_posix(),
        'size': src.stat().st_size,
        'sha256': sha256(src),
        'classes': sorted(set(CLASS_RE.findall(text))),
        'structs': sorted(set(STRUCT_RE.findall(text))),
        'interfaces': sorted(set(INTERFACE_RE.findall(text))),
        'enums': enums,
        'methods': sorted(set(METHOD_RE.findall(text))),
        'resource_like_strings': sorted(set(s for s in RESOURCE_STRING_RE.findall(text) if any(c in s for c in ['.',':','/'])))[:1000]
    }


def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--game-root', required=True, type=Path, help='Root of extracted/decompiled game files')
    ap.add_argument('--manifest', type=Path, default=Path('manifests/idlewizard_required_files.json'))
    ap.add_argument('--out', type=Path, default=Path('iw_workspace'))
    ap.add_argument('--make-review-bundle', action='store_true', help='Also creates one big source_bundle.txt')
    ns=ap.parse_args()
    root=ns.game_root.resolve()
    manifest=load_manifest(ns.manifest)
    if ns.out.exists(): shutil.rmtree(ns.out)
    raw=ns.out/'raw_files'; raw.mkdir(parents=True)

    logic, missing_logic = discover(root, manifest['logic_patterns'], manifest.get('ignore_patterns', []))
    data, missing_data = discover(root, manifest['data_patterns'], manifest.get('ignore_patterns', []))
    files=logic+data

    copied=[]
    for p in files:
        rel=p.relative_to(root)
        dest=raw/rel
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, dest)
        copied.append(dest)

    scanned=[]
    class_index={}; enum_index={}; interface_index={}; struct_index={}
    for p in copied:
        if p.suffix == '.cs':
            entry=scan_source_file(p, raw)
        else:
            entry={'path': p.relative_to(raw).as_posix(), 'data_file': True, 'size':p.stat().st_size, 'sha256':sha256(p)}
        scanned.append(entry)
        for c in entry.get('classes',[]): class_index.setdefault(c,[]).append(entry['path'])
        for c in entry.get('interfaces',[]): interface_index.setdefault(c,[]).append(entry['path'])
        for c in entry.get('structs',[]): struct_index.setdefault(c,[]).append(entry['path'])
        for e in entry.get('enums',[]): enum_index.setdefault(e['name'],[]).append({'file':entry['path'], 'values':e['values']})

    report={
        'game_root': str(root),
        'file_count': len(scanned),
        'logic_file_count': len(logic),
        'data_file_count': len(data),
        'classes': class_index,
        'structs': struct_index,
        'interfaces': interface_index,
        'enums': enum_index,
        'files': scanned,
    }
    ns.out.mkdir(parents=True, exist_ok=True)
    (ns.out/'verified_source_index.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    (ns.out/'missing_expected_patterns.json').write_text(json.dumps({'missing_logic_patterns':missing_logic,'missing_data_patterns':missing_data}, indent=2), encoding='utf-8')

    if ns.make_review_bundle:
        with (ns.out/'source_bundle.txt').open('w', encoding='utf-8', errors='ignore') as out:
            for p in copied:
                if p.suffix == '.cs' or (p.name.endswith('.bytes') or p.name.endswith('.bytes.txt')) or p.suffix == '.json':
                    out.write(f"\n\n===== FILE: {p.relative_to(raw).as_posix()} =====\n")
                    out.write(p.read_text(errors='ignore'))

    print(f"Workspace: {ns.out}")
    print(f"Copied relevant files: {len(copied)}")
    print(f"Logic files: {len(logic)} | Data files: {len(data)}")
    print(f"Index: {ns.out/'verified_source_index.json'}")
    print(f"Missing patterns: {ns.out/'missing_expected_patterns.json'}")
    for important in ['BigNumber','Variable','VariableComplex','EffectFactory','SimpleEffect','GameContext','GameManager','ConditionFactory','SaveData','SpellBook','Item','Spell']:
        locations = class_index.get(important) or struct_index.get(important) or interface_index.get(important) or []
        print(f"{important}: {locations}")

if __name__ == '__main__':
    main()

