#!/usr/bin/env python3
"""Serve a local Unity Web build with compression and isolation headers."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('--port', type=int, default=4174)
parser.add_argument('--directory', type=Path, default=Path(__file__).resolve().parent.parent / 'apps/unity/Builds/Web')
args = parser.parse_args()
if not (args.directory / 'index.html').exists():
    raise SystemExit('Build index.html missing. Run tools/unity.sh web first.')

class UnityHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith(('.br', '.gz')):
            path = path.rsplit('.', 1)[0]
        if path.endswith('.wasm'):
            return 'application/wasm'
        if path.endswith('.js'):
            return 'text/javascript'
        return super().guess_type(path)

    def end_headers(self):
        if self.path.split('?', 1)[0].endswith('.br'):
            self.send_header('Content-Encoding', 'br')
        elif self.path.split('?', 1)[0].endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()

server = ThreadingHTTPServer(('127.0.0.1', args.port), partial(UnityHandler, directory=str(args.directory.resolve())))
print(f'HOSTFALL Web: http://127.0.0.1:{args.port}', flush=True)
try:
    server.serve_forever()
except KeyboardInterrupt:
    pass
finally:
    server.server_close()
