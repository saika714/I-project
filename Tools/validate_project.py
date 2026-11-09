#!/usr/bin/env python3
"""Read-only repository checks; does not replace Unity compilation or Test Runner."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
MVP = ASSETS / "_SoftballConnect"


def require(condition, message):
    if not condition:
        raise SystemExit("FAIL: " + message)


guids = {}
for meta in ASSETS.rglob("*.meta"):
    match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.M)
    require(match is not None, "Missing GUID in " + str(meta))
    guid = match.group(1)
    require(meta.with_suffix("").exists(), "Orphan metadata: " + str(meta))
    require(guid not in guids, "Duplicate GUID: " + guid)
    guids[guid] = meta.with_suffix("")

for path in [MVP] + list(MVP.rglob("*")):
    if path.suffix != ".meta":
        require(path.with_name(path.name + ".meta").is_file(), "Missing metadata: " + str(path))

for layer in ["Domain", "Application"]:
    directory = MVP / "Scripts" / layer
    asm = json.loads((directory / ("SoftballConnect." + layer + ".asmdef")).read_text())
    require(asm.get("noEngineReferences"), layer + " must not reference Unity engines")
    for source in directory.glob("*.cs"):
        require("using Unity" not in source.read_text(), "Reverse dependency: " + str(source))

actions = json.loads((MVP / "Input/SoftballDesktop.inputactions").read_text())
names = {a["name"] for a in actions["maps"][0]["actions"]}
require(names == {"Swing", "PointerSwing", "Continue", "Restart", "Pause", "Menu", "Language", "Assist", "Debug"}, "Unexpected action names")
catalog = json.loads((MVP / "Localization/ui.json").read_text())["entries"]
keys = {e["key"] for e in catalog}
require(len(keys) == len(catalog), "Duplicate localization keys")
require(all(e.get("en") and e.get("ja") for e in catalog), "Missing translations")

# Documentation lives outside Assets. Validate relative links after moving it.
documents = [ROOT / "README.md"] + list((ROOT / "Docs").glob("*.md")) + list((ROOT / ".github").rglob("*.md")) + list((ROOT / "Reports").rglob("*.md"))
for name in ["PROJECT_OVERVIEW", "SOFTBALL_GAME_MASTER_SPEC", "PROJECT_AUDIT", "ARCHITECTURE", "EDITOR_GUIDE", "VALIDATION", "CHANGELOG_PHASE_0_1", "CONTRIBUTING", "DEVELOPMENT_RULES"]:
    require((ROOT / "Docs" / (name + ".md")).is_file(), "Missing document: " + name)
require(not list(MVP.rglob("*.md")), "Development documentation belongs in Docs, outside Assets")
for document in documents:
    for target in re.findall(r"\[[^\]]+\]\(([^)]+)\)", document.read_text()):
        if "://" in target or target.startswith("#"):
            continue
        require((document.parent / target.split("#", 1)[0]).exists(), "Broken link: " + str(document) + " -> " + target)

version_text = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()
version = re.search(r"^m_EditorVersion: (.+)$", version_text, re.M).group(1)
require("Unity " + version in (ROOT / "README.md").read_text(), "README Unity version differs from ProjectVersion")
manifest = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
locked = json.loads((ROOT / "Packages/packages-lock.json").read_text())["dependencies"]
for package, expected in manifest.items():
    require(package in locked and locked[package]["version"] == expected, "Package lock mismatch: " + package)

build = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text()
for role, name in enumerate(["Bootstrap", "MainMenu", "Stadium_PC"]):
    scene = MVP / "Scenes" / (name + ".unity")
    data = scene.read_text()
    require("Role: " + str(role) in data, "Incorrect scene role: " + name)
    require("path: Assets/_SoftballConnect/Scenes/" + name + ".unity" in build, "Scene missing from build: " + name)
    for guid in re.findall(r"guid: ([0-9a-f]{32})", data):
        if not guid.startswith("0000000000000000"):
            require(guid in guids, "Broken scene reference: " + name + " " + guid)
for asset in (MVP / "Config").glob("*.asset"):
    for guid in re.findall(r"guid: ([0-9a-f]{32})", asset.read_text()):
        require(guid in guids, "Broken config reference: " + str(asset) + " " + guid)

for scene_path, guid in re.findall(r"path: (.+)\n\s+guid: ([0-9a-f]{32})", build):
    require((ROOT / scene_path).is_file(), "Build settings reference a deleted scene: " + scene_path)
    require(guids.get(guid) == ROOT / scene_path, "Build scene GUID mismatch: " + scene_path)
require("com.unity.input.settings.actions" not in build, "Unused template-wide input actions must not be referenced")
print("PASS: GUIDs, metadata, scene/config references, input, translations and layer boundaries")
print("PASS: Docs links and Unity " + version + " package lock consistency")
print("Unity compilation, gameplay and visual QA are recorded separately in Docs/VALIDATION.md")
