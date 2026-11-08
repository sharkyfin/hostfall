#!/usr/bin/env python3
"""Fetch the three selected CC0 props, preserving sources and deriving URP maps.

Run with Pillow installed. No scripts or scene files from the source are executed.
"""
import hashlib
import json
from pathlib import Path
import subprocess
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
DEST = ROOT / 'apps/unity/Assets/_HOSTFALL/Art/PolyHavenEngineering'
CACHE = ROOT / '.artifacts/research-2026-11-07'
IDS = ['office_notepads', 'modular_industrial_pipes_01', 'industrial_storage_cart']
AUTHORS = {'office_notepads': 'Ulan Cabanilla', 'modular_industrial_pipes_01': 'Jorge Camacho', 'industrial_storage_cart': 'Jule Bielitz'}


def fetch(url, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        subprocess.run(['curl', '-A', 'HOSTFALL-asset-preparation/1.0', '-L', '--fail', '--silent', '--show-error', url, '-o', str(path)], check=True)


for asset_id in IDS:
    manifest = CACHE / (asset_id + '-files.json')
    fetch('https://api.polyhaven.com/files/' + asset_id, manifest)
    data = json.loads(manifest.read_text())
    target = DEST / asset_id
    sources = []
    model = data['fbx']['1k']['fbx']
    resources = [(model, target / (asset_id + '.fbx'))]
    # glTF's JPEG maps avoid implicit EXR conversions; geometry remains original FBX.
    for name, entry in data['gltf']['1k']['gltf']['include'].items():
        if name.startswith('textures/'):
            resources.append((entry, target / 'Textures' / Path(name).name))
    for entry, path in resources:
        fetch(entry['url'], path)
        digest = hashlib.md5(path.read_bytes()).hexdigest()
        if digest != entry['md5']:
            raise SystemExit('Source checksum mismatch: ' + str(path))
        sources.append({'file': str(path.relative_to(target)), 'url': entry['url'], 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    derived = []
    for arm in (target / 'Textures').glob('*_arm_1k.jpg'):
        prefix = arm.name.replace('_arm_1k.jpg', '')
        channels = Image.open(arm).convert('RGB').split()
        smooth = channels[1].point(lambda v: 255-v)
        zero = Image.new('L', channels[0].size, 0)
        for name, image in [(prefix + '_MetalSmooth.png', Image.merge('RGBA', (channels[2], zero, zero, smooth))),
                            (prefix + '_Occlusion.png', channels[0])]:
            image.save(target / 'Textures' / name)
            derived.append({'file': 'Textures/' + name, 'source': str(arm.relative_to(target)), 'formula': 'ARM R=AO, G=roughness, B=metal; URP R=metal A=255-roughness; separate AO=R'})
    (target / 'PROVENANCE.json').write_text(json.dumps({'asset': asset_id, 'author': AUTHORS[asset_id], 'source': 'https://polyhaven.com/a/' + asset_id, 'license': 'CC0-1.0', 'license_url': 'https://polyhaven.com/license', 'checked': '2026-11-07', 'sources': sources, 'derived': derived}, indent=2))
    (target / 'LICENSE.txt').write_text('CC0 1.0 Universal\nAsset: ' + asset_id + '\nAuthor: ' + AUTHORS[asset_id] + '\nSource: https://polyhaven.com/a/' + asset_id + '\nAsset license: https://polyhaven.com/license\nLegal code: https://creativecommons.org/publicdomain/zero/1.0/legalcode\nOriginal FBX and maps preserved. Derived URP maps are documented in PROVENANCE.json.\n')
    print(asset_id, len(sources), 'source files checked')
