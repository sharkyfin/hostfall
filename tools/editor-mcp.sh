#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
exec /Users/mac/.local/bin/uv run --python 3.12 --with 'mcp==1.26.0' python tools/editor-mcp.py "$@"
