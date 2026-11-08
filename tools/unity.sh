#!/usr/bin/env bash
set -euo pipefail
# On macOS, if USERPROFILE is leaked from cross-platform shells without LOCALAPPDATA,
# Unity Bee Stevedore fails to infer the cache folder.
unset USERPROFILE || true
export XDG_CACHE_HOME="${XDG_CACHE_HOME:-$HOME/Library/Caches}"
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity}"
local_editor="$repo_root/.artifacts/unity-editor/Unity/Unity.app/Contents/MacOS/Unity"
if [[ ! -x "$unity_editor" && -x "$local_editor" ]]; then unity_editor="$local_editor"; fi
if [[ ! -x "$unity_editor" ]]; then
  echo 'Unity 6000.6.8f1 not found. Set UNITY_EDITOR to its executable.' >&2; exit 2
fi
mkdir -p "$repo_root/.artifacts/qa"
mode="${1:-open}"
project_path="$repo_root/apps/unity"
if [[ "$mode" == open ]]; then exec "$unity_editor" -projectPath "$project_path"; fi
case "$mode" in
  author) args=(-executeMethod Hostfall.Editor.FacilityBuilder.Rebuild -quit) ;;
  expand) args=(-executeMethod Hostfall.Editor.SectorExpansionBuilder.Apply -quit) ;;
  editmode|playmode) args=(-runTests -testPlatform "$mode" -testResults "$repo_root/.artifacts/qa/$mode.xml") ;;
  desktop) args=(-executeMethod Hostfall.Editor.FacilityBuilder.BuildDesktop -quit) ;;
  web) args=(-buildTarget WebGL -executeMethod Hostfall.Editor.FacilityBuilder.BuildWeb -quit) ;;
  *) echo 'Usage: tools/unity.sh open|author|expand|editmode|playmode|desktop|web' >&2; exit 2 ;;
esac
exec "$unity_editor" -batchmode -nographics -projectPath "$project_path" -logFile "$repo_root/.artifacts/qa/unity-$mode.log" "${args[@]}"
