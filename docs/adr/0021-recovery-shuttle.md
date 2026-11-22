# 0021 — Recovery and the service shuttle

2026-11-18 · Accepted for the 2.0 preview.

## Context

Recovery ties together the completed computer systems and gives the player
a shortcut from the shelter to the Central Ring. The shortcut needs to work
after leaving the terminal and across saves taken during travel.

## Decision

ComputerCampaign derives the six Recovery records from eight earned receipts.
RecoveryProtocolSession validates the three stages and emits one completion
receipt. RecoveryTransit owns the trip and samples a deterministic path through
the lower service tunnel. The world controller applies that pose to the cab
and its passenger and keeps the dock doors interlocked.

## Consequences

The v3 snapshot records the active trip. The save reader accepts a below-floor
player pose during an authorized trip only when it matches the sampled cab.
Older v3 snapshots start with the cab docked at the shelter. Optional terminals
remain independent of the Recovery prerequisites. Play Mode covers boarding,
pause, mid-trip reload and exit in both travel directions.
