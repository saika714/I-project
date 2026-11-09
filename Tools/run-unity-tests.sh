#!/usr/bin/env bash
set -eu

task_root="$(cd "$(dirname "$0")/.." && pwd)"
task_project="${1:-$task_root}"
task_version="$(awk '$1 == "m_EditorVersion:" { print $2 }' "$task_project/ProjectSettings/ProjectVersion.txt")"
task_editor="${SOFTBALL_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$task_version/Unity.app/Contents/MacOS/Unity}"
task_results="$task_root/TestResults"

if [ ! -x "$task_editor" ]; then
    echo "Set SOFTBALL_UNITY_EDITOR to the Unity $task_version executable." >&2
    exit 1
fi
mkdir -p "$task_results"
for task_platform in EditMode PlayMode; do
    "$task_editor" -batchmode -projectPath "$task_project" \
        -runTests -testPlatform "$task_platform" -testFilter SoftballConnect.Tests -testCategory '!ReportCapture' \
        -testResults "$task_results/$task_platform.xml" \
        -logFile "$task_results/$task_platform.log"
done
echo "Unity results: $task_results"
