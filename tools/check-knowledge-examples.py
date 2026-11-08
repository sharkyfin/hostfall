#!/usr/bin/env python3
"""Run trusted, repository-authored teaching examples, never player input."""
import json, subprocess, sys, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
articles=json.loads((root/'apps/unity/Assets/_HOSTFALL/Content/Resources/FieldContent.json').read_text())['articles']
results=[]
with tempfile.TemporaryDirectory(prefix='hostfall-lessons-') as workspace:
    workspace=Path(workspace)
    for a in articles:
        if a.get('track') not in ('cpp','python'): continue
        for e in a['examples']:
            stem=workspace/a['id']
            if e['language']=='cpp':
                path=stem.with_suffix('.cpp'); path.write_text(e['code'])
                compiled=subprocess.run(['clang++','-std=c++20','-Wall','-Wextra','-Werror','-pedantic',str(path),'-o',str(stem)],capture_output=True,text=True,timeout=30)
                if compiled.returncode: raise RuntimeError(a['id']+': '+compiled.stderr)
                command=[str(stem)]
            else:
                path=stem.with_suffix('.py'); path.write_text(e['code']); command=[sys.executable,str(path)]
            run=subprocess.run(command,cwd=workspace,capture_output=True,text=True,timeout=5)
            expected=e['expected'].strip()
            if run.returncode or run.stdout.strip()!=expected:
                raise RuntimeError(f"{a['id']}: got {run.stdout!r}, expected {expected!r}; {run.stderr}")
            results.append({'id':a['id'],'language':e['language'],'result':'passed','stdout':run.stdout.strip()})
report=root/'.artifacts/iteration-6/examples.json';report.parent.mkdir(parents=True,exist_ok=True)
report.write_text(json.dumps({'python':sys.version,'compiler':subprocess.check_output(['clang++','--version'],text=True).splitlines()[0],'examples':results},ensure_ascii=False,indent=2)+'\n')
print(f'{len(results)}/48 authored code examples passed (24 C++20, 24 Python); report: {report}')
