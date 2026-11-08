#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Run only the authored, repository-owned Python/JS teaching examples.
This is developer verification, not a sandbox for player programs.
"""
import json
import shutil
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
content = root / 'apps/unity/Assets/_HOSTFALL/Content/Resources/FieldContent.json'
data = json.loads(content.read_text(encoding='utf-8'))
checked = 0
for article in data['articles']:
    for example in article['examples']:
        if example['language'] not in ('python', 'javascript'):
            continue
        executable = sys.executable if example['language'] == 'python' else shutil.which('node')
        if executable is None:
            raise SystemExit('Node.js required to verify the JavaScript example')
        option = '-c' if example['language'] == 'python' else '-e'
        result = subprocess.run([executable, option, example['code']], capture_output=True, text=True, timeout=3, check=True)
        assert result.stdout.strip() == example['expected'], article['id']
        version = subprocess.check_output([executable, '--version'], text=True).strip()
        print('PASS', article['id'], version, repr(result.stdout.strip()))
        example['verification'] = 'Выполнено: ' + version + '; результат проверен 2026-11-06'
        checked += 1
assert checked == 2
content.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Verified 2 authored examples. Docker/Kubernetes/HTTP were not executed.')
