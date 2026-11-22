#!/usr/bin/env python3
"""Check the ground-level clearance grid exported by Hostfall.Editor.TraversalAudit.

This checks geometric connectivity with doors/threats excluded, not campaign completion.
"""
import collections
import json
import pathlib
import sys

path = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else '.artifacts/map-repair-20261113/geometry.json')
rows = json.loads(path.read_text())['rows']
labels = {}
components = []
for z, row in enumerate(rows):
    for x, value in enumerate(row):
        if value != '.' or (x, z) in labels:
            continue
        label = len(components)
        queue = collections.deque([(x, z)])
        labels[x, z] = label
        count = 0
        while queue:
            a, b = queue.popleft()
            count += 1
            for p, t in ((a-1,b), (a+1,b), (a,b-1), (a,b+1)):
                if 0 <= t < len(rows) and 0 <= p < len(row) and rows[t][p] == '.' and (p,t) not in labels:
                    labels[p,t] = label
                    queue.append((p,t))
        components.append(count)

probes = [('spawn',-2.5,-1.5), ('HOME',0,-14), ('S01',0,3),
          ('S02',0,17), ('S03',-16,20), ('S04',0,42), ('S05',0,65),
          ('S06',20,77), ('S07',30,60), ('S08',62,67), ('S08 office',62,55),
          ('S08 vault',71,22), ('S09',30,100), ('S09 witness',59,112),
          ('S10',10,132), ('evacuation',10,200), ('SOC shortcut',8.5,9)]
start = labels.get((55,41))
failed = []
lines = []
for name, x, z in probes:
    a, b = round((x+30)*2), round((z+22)*2)
    # Permit a neighbouring cell within 0.5 m when a probe meets a prop.
    candidates = [(abs(p-a)+abs(t-b),n) for (p,t),n in labels.items() if abs(p-a)+abs(t-b) <= 1]
    accessible = any(n == start for _, n in candidates)
    lines.append(f'{"PASS" if accessible else "FAIL"} {name} ({x}, {z})')
    if not accessible:
        failed.append(name)
print('\n'.join(lines))
path.with_name('connectivity.txt').write_text('\n'.join(lines)+'\n')
sys.exit(bool(failed))
