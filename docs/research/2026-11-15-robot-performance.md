# Robot armour and optimisation options — 15 November 2026

Request: repair floating robot armour and choose the strongest practical performance approach.
Inspection used the saved authored world in the isolated Unity 6000.6.8f1 copy, not regeneration.

## Cause and selection criteria

QuadShell and Trilobite received the same four actor-space plates despite different torso heights
and widths. They were already under Body, but a correct parent does not repair a wrong mounting pose.
Fit against rigid Body-weighted torso triangles, account for FBX bind poses/centimetre scale, embed
plates slightly, then retain the Body parent. This is an editor operation, with no per-frame BakeMesh.
Temporary CPU-readable FBX import is restored afterwards. Names, damage ordering and save IDs survive.

Optimisation must preserve robot activity, floor/door collision, weak points and current visual quality.
Do not infer rendered FPS from headless/batch CPU checks. Native comparisons use AMD Radeon Pro 5300M,
Metal, 1280×800, six fixed views, 45 warm-up and 90 measured frames per view. VSync is disabled only
in an explicitly requested development replay, which has private saves and does not run in release.

## Alternatives reviewed

| Option | Decision / reason |
|---|---|
| Non-alloc physics queries alone | Necessary, insufficient: removes normal query arrays but leaves simultaneous long searches. |
| Shared budgeted local A* + reusable buffers | Selected. Fair queue, at most 96 candidate-edge collision checks and a 1.2 ms soft time budget per frame. One physics operation can overrun the soft limit; direct movement safety checks remain outside the search budget. |
| Async baked NavMesh / NavMeshAgent | Deferred. Requires validating doors, moving bridges, stair surfaces and all vertical routes; a blind replacement would risk new route bugs. |
| Freeze distant robots | Rejected for this pass: changes autonomous patrol and combat behaviour. |
| Cache damage appearance / shader block | Selected. Cache hierarchy once; update only when broken-part mask or death changes. Later boss lights register with the death cache. |
| Combine fixed mini-boss meshes per material | Selected. 141 → 21 renderers per MANTIS. Plates, coolant, core, sensor and hatch remain separate; transforms used by weapons remain. |
| Combine entire facility / static batching | Rejected here. Large combined bounds worsen culling and static batching duplicates geometry; GPU Resident Drawer is incompatible with static batching. |
| GPU Resident Drawer | Tested in native players; not selected. Draw submission fell, but repeated low-resolution compute-view captures were black. No production toggle enabled. |
| GPU occlusion | Tested separately; not selected. A black compute-view capture and worse high-resolution engine-frame intervals failed acceptance. |
| Reduce resolution/shadows/ambient lighting | Not selected. Hides cost by changing the requested visual quality. Only four scene lights cast shadows in the original inventory. |
| Automatic mesh decimation/LOD | Deferred. Requires authored silhouettes and distance checks; does not directly address burst navigation or fixed-part draw submission. |
| Pool every detached armour fragment | Deferred. Short bounded cosmetic fragments are lower priority than persistent scene/render and navigation work; no unmeasured universal pooling system introduced. |

## Primary references

- [Unity 6.6 GPU Resident Drawer performance](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer-performance)
- [Unity draw-call method comparison](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/reduce-draw-calls/optimizing-draw-calls-choose-method)
- [Unity Frame Timing Manager](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/profile-rendering/frame-timing-manager/manager)

This is a comparison of relevant viable approaches, not a claim to have benchmarked every imaginable
architecture. NavMesh, LOD, global mesh merging and pooling were assessed, not implemented and timed.
