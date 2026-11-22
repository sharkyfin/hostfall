# Local editor workbench — 2026-11-14

## Installed and exercised

- Unity 6000.6.8f1 / MCP for Unity 10.3.0. Existing project package retained.
- Local Unity HTTP server: `http://127.0.0.1:8080/mcp`. User LaunchAgent
  `~/Library/LaunchAgents/com.hostfall.unity-mcp.plist` starts the pinned server after login.
- Blender 4.5.16 LTS / **mcp-for-blender 2.2.9**. Addon enabled, preferences saved,
  automatic socket on `127.0.0.1:9876`. No cloud generation accounts or API keys needed.
- Blender stdio MCP added to the user's Codex configuration. Existing servers preserved;
  private backup is outside the repository in `~/.codex/backups/hostfall-2026-11-14/`.
- Telemetry disabled in Blender server environment and addon; Unity bridge preference and
  server environment disable Unity MCP telemetry. Both listeners bind to loopback.

Both editors answered real MCP requests. Unity's Blender Bridge exported a selected
original mesh as FBX and imported it successfully. `tools/blender/make-service-junction.py`
is its reproducible authoring source. The user's original Blender scene was preserved;
asset review uses a separate `HOSTFALL Asset Workbench` scene.

Codex may require a restart to expose a newly registered server as native tools in a
new chat. The local client below works in the current chat, so this does not block work.
Native Unity and Blender tools are now registered and have also answered in Codex directly.
Native app window enumeration did not provide a reliable Unity window in this session;
use actual editor camera/viewport captures instead. Do not describe this as manual play.

## Commands

```sh
python3 tools/editor-tools-doctor.py
tools/editor-mcp.sh resources
tools/editor-mcp.sh read mcpforunity://instances
tools/editor-mcp.sh --instance '<name>@<hash>' read mcpforunity://editor/state
tools/editor-mcp.sh --instance '<name>@<hash>' call read_console '{"types":["error"],"count":10}'
tools/editor-mcp.sh --instance '<name>@<hash>' --group asset_gen call blender_bridge '{"action":"status"}'
tools/editor-mcp.sh --blender call get_scene_info
```

Select an instance explicitly before every editor operation. Main project and isolated
verification copy can both be connected. Never infer identity from the last active tool.
`@path.json` reads structured tool arguments from a file; images are decoded to
`.artifacts/tooling/images/`. The wrapper pins Python MCP SDK 1.26.0: the unpinned newer
SDK's cryptography wheel failed to load OpenSSL on this Intel Mac. No system libraries
were changed to work around it.

`HostfallEditorTools` is opt-in: `.artifacts/tooling/enable-editor-tools` enables automatic
connection in the main editor only. `HOSTFALL/Tools/Connect Local Editor Bridge` connects
manually. It never opens or saves a scene. Remove the opt-in marker to disable autoconnect.
Unloading the LaunchAgent with `launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.hostfall.unity-mcp.plist`
stops its local server; this does not remove Unity or Blender.

## Geometry and art workflow

1. Read current scene state and dirty flag. Preserve unsaved authoring work. Never use full
   `author` to apply a targeted repair. Back up/hash the saved scene before promotion.
2. Inventory installed sources and **actual scene material/model use**. Quaternius SciFi,
   Quaternius robots and the three curated Poly Haven sources are the palette; use those
   before downloading more packs. Source files alone are not evidence of use in the scene.
3. In an isolated copy, run `HOSTFALL/Tools/Audit Saved World`: measured renderer/collider
   bounds, material texture assignments and multi-angle camera renders go to
   `.artifacts/world-workbench/`. Compare the images, not only the hierarchy.
4. `HOSTFALL/Tools/Scan Ground Boundaries` checks floor/body clearance at 0.5m spacing
   and tests exposed edges with a capsule sweep. It reloads the saved scene afterwards.
   Doors/threats are excluded. It is a static ground-layer heuristic, not a jump proof or
   campaign playthrough. Review suspected overlaps before modifying them.
5. `HOSTFALL/Tools/Repair World Circulation` applies the specific reviewed repairs and
   curated service bays, then saves. Do this in the verification copy first. World devices,
   their IDs and simulation data survive. Do not add generic overlap deletion or invisible
   global walls that hide incorrect architecture.
6. Run `WorldCirculationTests` with the real CharacterController, then broader Play/Edit
   regressions and native build. Test both ascent and descent; validate headroom, deck
   transitions, prop intersections and boundary collision.
7. Promote only checked scene/assets after verifying the original scene hash still matches.
   Keep findings, actual test results and limitations in `docs/qa` and PROJECT_STATE.

Quaternius stair meshes rise toward local **-Z**. `StairWalkSurface` stores the slope in
mesh vertices; a tilted BoxCollider beneath a nonuniform scale has a different plane.
Quarter-turned modules need their desired dimensions mapped to local axes. Large static
surface UVs use metric tiling; preserve source trim-sheet UVs on imported kit meshes.
URP Forward+ keeps local lighting usable across large floor objects without raising global
ambient intensity. It is the same URP renderer, with a different lighting path.

## Primary references consulted

- [Codex MCP configuration](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)
- [MCP for Unity](https://github.com/CoplayDev/unity-mcp)
- [MCP for Blender](https://github.com/ahujasid/mcp-for-blender)
- [Unity 6000.6 Forward/Forward+](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/urp-concepts/rendering-paths/forward-rendering-paths)

These are tooling/editorial references, not links in the game's learning UI.


Combat workshop follow-up (14 November): native computer use via getApp("Unity")
successfully exposed the external-scene-change Reload dialog. Reloading the
hash-verified promoted scene unblocked the main MCP; its active scene was clean and
console had zero errors. Earlier window-enumeration failures are not evidence that
all native app access is unavailable. This was an editor reload, not a manual game
playthrough. The original weapon/guardian Blender workflow also ran successfully;
see ADR 0010 and COMBAT_WORKSHOP_2026-11-14.md.


Collision audit follow-up (15 November): `Scan Ground Boundaries` now derives its
range from actual scene solids, including the shelter and evacuation room. The old
Z=-12..181 range missed both ends. `Audit Collision Visibility`, `Audit Collider Surfaces`
and `Scan Collision Floor Grid` are inspection tools restricted to the verification
copy. Surface mismatches are candidates, not automatic deletion instructions. Use
`Repair Visible Collider Profiles` only for its reviewed weapon/fixture/S10 fixes;
`ColliderProfileTests` and `CollisionAuditTests` exercise actual character movement.

## Native diagnostics and saves — 2026-11-15

Explicit `WorldSaveController.TestStorageDirectory` must be honoured in both normal
and batch players. Production saves must never be used by a replay. Development
workstation builds also use the separate `com.northline.hostfall.workstationqa`
bundle identifier so window preferences are isolated. Verify hashes of all real
save slots before/after native runs. `unity.NORTHLINE.HOSTFALL` is a legacy domain;
current Unity Screenmanager keys live in `com.northline.hostfall`. Do not overwrite
the entire preference domain from an older resolution backup. See workstation QA
for the initial isolation failure and recovery.
