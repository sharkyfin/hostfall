#!/usr/bin/env python3
"""Read-only local readiness report. Never reads account configuration or process argv."""
import json,socket,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def port(n):
    try:
        with socket.create_connection(('127.0.0.1',n),2):return True
    except OSError:return False
report={'unity_version':(root/'apps/unity/ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0],
        'unity_http_local':port(8080),'blender_socket_local':port(9876),
        'blender_app':Path('/Applications/Blender.app').exists(),
        'asset_inventory':{},'unity_instances':None}
for folder in ['QuaterniusSciFi','QuaterniusRobots','PolyHavenEngineering','HostfallEngineering']:
    p=root/'apps/unity/Assets/_HOSTFALL/Art'/folder
    report['asset_inventory'][folder]={'models':len(list(p.rglob('*.fbx'))),'textures':len(list(p.rglob('*.png')))+len(list(p.rglob('*.jpg')))}
if report['unity_http_local']:
    result=subprocess.run([str(root/'tools/editor-mcp.sh'),'read','mcpforunity://instances'],capture_output=True,text=True,timeout=30)
    if result.returncode==0:
        report['unity_instances']=json.loads(json.loads(result.stdout)['contents'][0]['text'])
if report['blender_socket_local']:
    with socket.create_connection(('127.0.0.1',9876),2) as s:
        s.settimeout(5);s.sendall(b'{"type":"get_scene_info","params":{}}')
        report['blender_scene']=json.loads(s.recv(60000))
print(json.dumps(report,ensure_ascii=False,indent=2))
