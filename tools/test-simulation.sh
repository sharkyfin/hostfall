#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-$repo_root/.artifacts/unity-editor/Unity/Unity.app/Contents/MacOS/Unity}"
unity_resources="$(cd "$(dirname "$unity_editor")/../Resources" && pwd)"
dotnet_sdk="${DOTNET_EXECUTABLE:-$unity_resources/Scripting/DotNetSdk/dotnet}"
exec "$dotnet_sdk" run --project "$repo_root/tools/SimulationChecks/SimulationChecks.csproj" -p:UnityResourcesDir="$unity_resources"
