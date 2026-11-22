#!/usr/bin/env python3
"""Local MCP client for this project; no credentials, screenshots saved rather than printed."""
import argparse,asyncio,base64,json,os
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.streamable_http import streamablehttp_client
from mcp.client.stdio import stdio_client

ROOT=Path(__file__).resolve().parents[1]
def output(result):
    data=result.model_dump(mode='json') if hasattr(result,'model_dump') else result
    def clean(v):
        if isinstance(v,dict):
            if v.get('type')=='image' and v.get('data'):
                d=ROOT/'.artifacts/tooling/images';d.mkdir(parents=True,exist_ok=True)
                p=d/(str(len(list(d.glob('*.png'))))+'.png');p.write_bytes(base64.b64decode(v['data']))
                return {'type':'image','file':str(p)}
            return {k:clean(x) for k,x in v.items()}
        if isinstance(v,list):return [clean(x) for x in v]
        if isinstance(v,str) and len(v)>50000:return f'<large output omitted: {len(v)} characters>'
        return v
    print(json.dumps(clean(data),ensure_ascii=False,indent=2))
def failed(result):
    if getattr(result,'isError',False):return True
    data=getattr(result,'structuredContent',None)
    if isinstance(data,dict):
        data=data.get('result',data)
        if isinstance(data,dict) and data.get('success') is False:return True
    for block in getattr(result,'content',[]):
        if getattr(block,'type','')=='text':
            try:
                data=json.loads(block.text)
                if isinstance(data,dict) and data.get('success') is False:return True
            except (ValueError,TypeError):pass
    return False
async def run(session,args):
    await session.initialize()
    if args.instance:
        selected=await session.call_tool('set_active_instance',{'instance':args.instance})
        if failed(selected):output(selected);raise SystemExit(1)
    if args.action=='resources':output(await session.list_resources());return
    if args.action=='tools':
        result=await session.list_tools();output([{'name':t.name,'description':t.description,'schema':t.inputSchema} for t in result.tools]);return
    if args.action=='read':output(await session.read_resource(args.name));return
    if args.group:await session.call_tool('manage_tools',{'action':'activate','group':args.group})
    result=await session.call_tool(args.name,json.loads(Path(args.params[1:]).read_text() if args.params.startswith('@') else args.params))
    output(result)
    if failed(result):raise SystemExit(1)
async def main(args):
    if args.blender:
        params=StdioServerParameters(command='/Users/mac/.local/bin/uvx',args=['--from','mcp-for-blender==2.2.9','mcp-for-blender'],env={**os.environ,'DISABLE_TELEMETRY':'true','BLENDER_HOST':'127.0.0.1'})
        async with stdio_client(params) as streams:
            async with ClientSession(*streams) as session:await run(session,args)
    else:
        async with streamablehttp_client('http://127.0.0.1:8080/mcp',timeout=600) as streams:
            async with ClientSession(streams[0],streams[1]) as session:await run(session,args)
p=argparse.ArgumentParser();p.add_argument('action',choices=['tools','resources','read','call']);p.add_argument('name',nargs='?',default='');p.add_argument('params',nargs='?',default='{}');p.add_argument('--instance');p.add_argument('--group');p.add_argument('--blender',action='store_true');asyncio.run(main(p.parse_args()))
