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
if [[ "$mode" == web-candidate ]]; then
  web_project="$repo_root/.artifacts/hostfall2-web-candidate"
  for directory in Assets Packages ProjectSettings; do
    mkdir -p "$web_project/$directory"
    rsync -a --delete "$project_path/$directory/" "$web_project/$directory/"
  done
  python3 - "$web_project" <<'PY'
import json
import sys
from pathlib import Path

web_project = Path(sys.argv[1])
# The Editor tooling adds an incompatible native TextMate library to Web players.
manifest = web_project / "Packages/manifest.json"
if manifest.exists():
    data = json.loads(manifest.read_text())
    for package in ("com.unity.ai.assistant", "com.unity.ai.inference"):
        data["dependencies"].pop(package, None)
    manifest.write_text(json.dumps(data, indent=2) + "\n")

# Optimize WebGL rendering settings (disable heavy SSAO, 4x cascades, 2048 shadows, HDR, MSAA, switch Forward+ to Forward)
renderer = web_project / "Assets/_HOSTFALL/Content/Settings/FacilityRenderer.asset"
if renderer.exists():
    txt = renderer.read_text()
    txt = txt.replace("m_Name: ScreenSpaceAmbientOcclusion\n  m_EditorClassIdentifier: \n  m_Active: 1",
                      "m_Name: ScreenSpaceAmbientOcclusion\n  m_EditorClassIdentifier: \n  m_Active: 0")
    txt = txt.replace("m_RenderingMode: 2", "m_RenderingMode: 0")
    renderer.write_text(txt)

urp = web_project / "Assets/_HOSTFALL/Content/Settings/FacilityURP.asset"
if urp.exists():
    txt = urp.read_text()
    txt = txt.replace("m_SupportsHDR: 1", "m_SupportsHDR: 0")
    txt = txt.replace("m_MSAA: 2", "m_MSAA: 1")
    txt = txt.replace("m_MainLightShadowmapResolution: 2048", "m_MainLightShadowmapResolution: 1024")
    txt = txt.replace("m_AdditionalLightShadowsSupported: 1", "m_AdditionalLightShadowsSupported: 0")
    txt = txt.replace("m_AdditionalLightsShadowmapResolution: 2048", "m_AdditionalLightsShadowmapResolution: 512")
    txt = txt.replace("m_ShadowCascadeCount: 4", "m_ShadowCascadeCount: 2")
    txt = txt.replace("m_SoftShadowQuality: 3", "m_SoftShadowQuality: 1")
    txt = txt.replace("m_SoftShadowsSupported: 1", "m_SoftShadowsSupported: 0")
    txt = txt.replace("m_ShadowDistance: 24", "m_ShadowDistance: 16")
    txt = txt.replace("m_OpaqueDownsampling: 1", "m_OpaqueDownsampling: 2")
    txt = txt.replace("m_UpscalingFilter: 0", "m_UpscalingFilter: 2")
    urp.write_text(txt)
PY
  "$unity_editor" -batchmode -nographics -noaudio -projectPath "$web_project" -buildTarget WebGL \
    -executeMethod Hostfall.Editor.Hostfall2Workbench.BuildWebCandidate -quit \
    -logFile "$repo_root/.artifacts/qa/unity-web-candidate.log"
  web_destination="$project_path/Builds/Web/HOSTFALL-2-Candidate"
  mkdir -p "$web_destination"
  rsync -a --delete "$web_project/Builds/Web/HOSTFALL-2-Candidate/" "$web_destination/"

  # Package itch.io archive
  itch_dir="$repo_root/.artifacts/hostfall-itch-preview"
  mkdir -p "$itch_dir"
  zip_file="$itch_dir/HOSTFALL-WebGL-v2-itch.zip"
  (cd "$web_destination" && zip -r -FS "$zip_file" .)
  cp -f "$zip_file" "$HOME/Desktop/HOSTFALL-WebGL-v2-itch.zip"

  # Package WebSim archive (with .websim.json and .websim-manifest.json)
  websim_dir="$repo_root/.artifacts/hostfall-websim-preview"
  mkdir -p "$websim_dir"
  rsync -a --delete "$web_destination/" "$websim_dir/"
  python3 - "$websim_dir" <<'PY'
import hashlib
import json
import sys
from pathlib import Path

target = Path(sys.argv[1])
(target / ".websim.json").write_text(json.dumps({"projectId": "xli3ad926zpkxg4eqvag", "baseVersion": 2}, indent=2) + "\n")
files = []
for p in sorted(target.rglob("*")):
    if p.is_file() and not p.name.startswith(".websim") and not p.name.endswith(".zip") and p.name != ".DS_Store":
        rel = str(p.relative_to(target))
        files.append({"path": rel, "hash": hashlib.sha256(p.read_bytes()).hexdigest()})
(target / ".websim-manifest.json").write_text(json.dumps({"files": files}, indent=2) + "\n")
PY
  websim_zip="$itch_dir/HOSTFALL-WebSim.zip"
  (cd "$websim_dir" && zip -r -FS "$websim_zip" .)
  cp -f "$websim_zip" "$HOME/Desktop/HOSTFALL-WebSim.zip"

  printf 'Itch package: %s\n' "$HOME/Desktop/HOSTFALL-WebGL-v2-itch.zip"
  printf 'WebSim package: %s\n' "$HOME/Desktop/HOSTFALL-WebSim.zip"
  printf '%s\n' "$web_destination/index.html"
  exit 0
fi
case "$mode" in
  author) args=(-executeMethod Hostfall.Editor.FacilityBuilder.Rebuild -quit) ;;
  expand) args=(-executeMethod Hostfall.Editor.SectorExpansionBuilder.Apply -quit) ;;
  audit) args=(-executeMethod HostfallSceneAudit.AnalyzeActiveScene -quit) ;;
  editmode|playmode) args=(-runTests -testPlatform "$mode" -testResults "$repo_root/.artifacts/qa/$mode.xml") ;;
  desktop) args=(-executeMethod Hostfall.Editor.FacilityBuilder.BuildDesktop -quit) ;;
  web) args=(-buildTarget WebGL -executeMethod Hostfall.Editor.FacilityBuilder.BuildWeb -quit) ;;
  *) echo 'Usage: tools/unity.sh open|author|expand|editmode|playmode|desktop|web|web-candidate' >&2; exit 2 ;;
esac
exec "$unity_editor" -batchmode -nographics -projectPath "$project_path" -logFile "$repo_root/.artifacts/qa/unity-$mode.log" "${args[@]}"
