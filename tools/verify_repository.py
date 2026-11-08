#!/usr/bin/env python3
"""Verify authored Unity references and preservation of the terminal baseline."""
from pathlib import Path
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parent.parent
UNITY = ROOT / 'apps/unity'
assets = UNITY / 'Assets'
errors = []

def check(value, message):
    if not value:
        errors.append(message)

version = (UNITY / 'ProjectSettings/ProjectVersion.txt').read_text()
check('m_EditorVersion: 6000.6.8f1' in version, 'Unexpected Unity editor version')
json.loads((UNITY / 'Packages/manifest.json').read_text())
known = {}
for path in assets.rglob('*.meta'):
    match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(), re.M)
    check(match, f'Missing GUID in {path.relative_to(ROOT)}')
    if match:
        guid = match[1]
        check(guid not in known, f'Duplicate GUID: {guid}')
        known[guid] = path
for path in assets.rglob('*'):
    if path.suffix != '.meta':
        check(Path(str(path) + '.meta').exists(), f'Missing meta: {path.relative_to(ROOT)}')
for path in assets.rglob('*.asmdef'):
    json.loads(path.read_text())
scene = assets / '_HOSTFALL/Content/Scenes/NorthlineFacility.unity'
check(scene.exists(), 'Authored NorthlineFacility scene is absent; run tools/unity.sh author')
if scene.exists():
    check(re.search(r'm_Name: [\"]?Systems[\"]?\n', scene.read_text()), 'Scene has no Systems root')
    referenced = set(re.findall(r'm_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})', scene.read_text()))
    for guid in referenced:
        # Package scripts live in the resolved Library, not source Assets.
        if guid in known:
            check(known[guid].with_suffix('').exists(), f'Missing script behind GUID {guid}')
    check(bool(referenced), 'Scene has no script references')
for path in (ROOT / 'legacy/terminal-v3/src/horror').glob('*'):
    original = subprocess.check_output(['git', 'show', 'e0f8ad1:src/horror/' + path.name], cwd=ROOT)
    check(original == path.read_bytes(), f'Legacy reference changed: {path.name}')
package_cache = UNITY / 'Library/PackageCache'
if package_cache.exists():
    resolved_guids = set(known)
    for path in package_cache.rglob('*.meta'):
        try:
            match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(), re.M)
            if match:
                resolved_guids.add(match[1])
        except UnicodeError:
            continue
    for tree in [assets, UNITY / 'ProjectSettings']:
        for path in tree.rglob('*'):
            if path.suffix not in ['.unity', '.prefab', '.asset', '.mat']:
                continue
            for guid in re.findall(r'guid: ([0-9a-f]{32})', path.read_text()):
                check(guid in resolved_guids or guid.startswith('0000000000000000'),
                      f'Unresolved serialized GUID {guid}: {path.relative_to(ROOT)}')
if errors:
    raise SystemExit('\n'.join(errors))
print(f'Repository checks passed; {len(known)} asset GUIDs; legacy reference matches baseline.')
print('This checks source integrity, not runtime behavior or package compatibility.')
