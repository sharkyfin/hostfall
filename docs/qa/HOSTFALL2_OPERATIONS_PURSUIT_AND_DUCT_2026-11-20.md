# HOSTFALL 2.0. Operations pursuit and physical ventilation

## Reproduction

The previous build used Z = 1.65 m as the lower bound of WATCHER R17's target selection. The centre of the Operations airlock is Z = 1.00 m. The actor in the user screenshot was on the corridor side, looking toward Operations through its open leaf. The regression in the original scene did not reach the player in Operations. The final test holds the opening pose, checks line of sight directly before and after the leaf moves, and verifies actual locomotion in both directions.

The sliding grille in Operations was at X = 3.30 m. The inner section ended against the east wall. It looked as though its metal bars covered a short blind cabinet. The wall inspection found overlapping lining, skirting, modular facing and a structural shell. The new passage cuts those layers, encloses the extension and continues upward at the return. The actor starts at X = 6.94 m and runs past the former wall before it reaches the room. The coil shown behind the bars was moved out of the duct and placed against the measured floor.

## Results

- Unity 6000.6.8f1 Play Mode, 9 passed. WATCHER follows an acquired target through the open Operations airlock in both directions. The shut leaf blocks sight. The Vault watcher pursues through the Energy Center door beyond its patrol bounds.
- A ray from inside the grille reaches the duct bend. The crawler passes through the old wall line visibly before release. Completion of VENTFLOW closes the internal damper. The vent still permits the physical claw counter, mid-task save restoration, and free circulation through the adjacent security doorway.
- The scripted route reaches the end of the current campaign. Source scene after promotion `be6a70bb54a7fc8f1365776e7ac82fd6229dc0ef32483c9b047c5057954fcc6e`. Original 1.x scene hash `569da130d17813bd8fb0922e77bc48e2511d7be51242a7c1877bf3b78689afc7`.
- Repository integrity passed for 856 asset GUIDs. The Play Mode report lives at `.artifacts/hostfall2-soc-pursuit/playmode.xml`. The editor captures of the closed and open grille are in that folder.
- The Intel macOS preview built with zero errors. Its native first-loop replay passed at 1920×1080, including vent release and checkpoint restore. All three inventoried user save files retained their hashes. Build `apps/unity/Builds/macOS/HOSTFALL-2-Service-Duct-Preview.app`.

The Operations shaft is the first physical network segment. The two independently controlled branches and additional puzzle variants are described in [the design plan](../hostfall/HORROR_PUZZLES_2026-11-20.md). Remaining traversal QA covers stairs and optional rooms.
