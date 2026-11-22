#!/usr/bin/env python3
"""Compile against the installed Editor API without starting the licensed Editor.

This checks C# API compatibility only, not Unity import, serialization, or Play Mode.
Requires the Editor's bundled .NET SDK and template assembly cache.
"""
from pathlib import Path
import hashlib
import json
import os
import subprocess
import urllib.request
import xml.etree.ElementTree as E

ROOT = Path(__file__).resolve().parent.parent
editor = Path(os.environ.get('UNITY_EDITOR', str(ROOT / '.artifacts/unity-editor/Unity/Unity.app/Contents/MacOS/Unity')))
resources = editor.parent.parent / 'Resources'
sdk = resources / 'Scripting/DotNetSdk/dotnet'
if not sdk.exists():
    raise SystemExit('Set UNITY_EDITOR to the installed 6000.6.8f1 Mac editor executable')
out = ROOT / '.artifacts/api-check'
out.mkdir(exist_ok=True)
manifest = json.loads((ROOT / 'apps/unity/Packages/manifest.json').read_text())['dependencies']
version = manifest['com.unity.splines']
splines = out / ('splines-' + version)
if not (splines / 'package/Runtime').exists():
    with urllib.request.urlopen('https://packages.unity.com/com.unity.splines', timeout=30) as response:
        release = json.load(response)['versions'][version]
    archive = out / ('splines-' + version + '.tgz')
    with urllib.request.urlopen(release['dist']['tarball'], timeout=30) as response:
        archive.write_bytes(response.read())
    if hashlib.sha1(archive.read_bytes()).hexdigest() != release['dist']['shasum']:
        raise SystemExit('Spline package integrity mismatch')
    splines.mkdir(exist_ok=True)
    subprocess.run(['tar', 'xzf', str(archive), '-C', str(splines)], check=True)
refs = {}
engine = resources / 'Scripting/Managed/UnityEngine'
for path in engine.glob('*.dll'):
    refs[path.name] = path
platform_extension = resources.parents[2] / 'PlaybackEngines/MacStandaloneSupport/UnityEditor.OSXStandalone.Extensions.dll'
if platform_extension.exists():
    refs[platform_extension.name] = platform_extension
cache = resources / 'PackageManager/ProjectTemplates/libcache'
# Prefer the matching URP template; fallback provides Mathematics for Cinemachine/Splines.
for template in sorted(cache.iterdir(), key=lambda p: ('urp' not in p.name, p.name)):
    for path in (template / 'ScriptAssemblies').glob('Unity.*.dll'):
        refs.setdefault(path.name, path)
versions = ';'.join('UNITY_6000_' + str(i) + '_OR_NEWER' for i in range(7)) + ';UNITY_2022_3_OR_NEWER;UNITY_2023_2_OR_NEWER'

def project(name, files, dependencies=(), defines='', language=None):
    folder = out / name
    folder.mkdir(exist_ok=True)
    root = E.Element('Project', Sdk='Microsoft.NET.Sdk')
    props = E.SubElement(root, 'PropertyGroup')
    for key, value in dict(TargetFramework='net8.0', EnableDefaultCompileItems='false', AssemblyName=name,
                           DefineConstants=versions + ';' + defines, AllowUnsafeBlocks='true',
                           NoWarn='0618;0649;0169;SYSLIB0050;SYSLIB0051').items():
        E.SubElement(props, key).text = value
    if language:
        E.SubElement(props, 'LangVersion').text = language
    items = E.SubElement(root, 'ItemGroup')
    for name_, path in refs.items():
        E.SubElement(E.SubElement(items, 'Reference', Include=name_), 'HintPath').text = str(path)
    for path in files:
        E.SubElement(items, 'Compile', Include=str(path))
    for dependency in dependencies:
        E.SubElement(items, 'ProjectReference', Include='../' + dependency + '/' + dependency + '.csproj')
    E.ElementTree(root).write(folder / (name + '.csproj'))

packages = resources / 'PackageManager/BuiltInPackages'
project('Unity.Splines', (splines / 'package/Runtime').rglob('*.cs'), defines='UNITY_PHYSICS_MODULE')
project('Unity.Cinemachine', (packages / 'com.unity.cinemachine/Runtime').rglob('*.cs'), ['Unity.Splines'],
        'CINEMACHINE_PHYSICS;CINEMACHINE_SPLINES_2_4;CINEMACHINE_URP;CINEMACHINE_UNITY_INPUTSYSTEM;CINEMACHINE_UIELEMENTS;CINEMACHINE_UNITY_6000_0_11F1_OR_NEWER')
source = ROOT / 'apps/unity/Assets/_HOSTFALL'
project('Hostfall.Simulation', (source / 'Simulation').rglob('*.cs'), language='9.0')
files = [path for path in source.rglob('*.cs') if not {'Editor', 'Tests', 'Simulation'}.intersection(path.relative_to(source).parts)]
project('Hostfall.Runtime', files, ['Hostfall.Simulation', 'Unity.Cinemachine'], language='9.0')
project('Hostfall.Editor', (source / 'Editor').glob('*.cs'), ['Hostfall.Runtime'], 'UNITY_EDITOR;UNITY_EDITOR_OSX', language='9.0')
subprocess.run([str(sdk), 'build', str(out / 'Hostfall.Editor/Hostfall.Editor.csproj'), '--nologo'], check=True)
print('API compilation passed. Unity import, scene serialization, and Play Mode remain separate checks.')
