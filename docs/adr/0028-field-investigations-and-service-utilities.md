# ADR 0028. Field investigations and service utilities

## Context

The campaign formerly mounted a separate game at each workstation. Their world
action receipts also controlled doors, equipment, encounters, and save validation.
The approved route retains Snake and places two investigations on later computers.

## Decision

Station consoles read an authored file catalog. CORE-01 and NEURO-01 require an
opened and cited pair of records and a confirmed answer. Other consoles display
logs and, where relevant, a local service script and its help. Scripts dispatch
a fixed operation restricted to the station that owns it. Circuit presets, field
intervals and R26 continue to use the physical systems. The freight alarm still
closes the focused station and calls the existing lockdown.

The home signature requires all six recovered records. A typed field result
writes the same earned world receipts as the historical route. Its optional
versioned save payload also stores the investigation read masks. The v3 reader
validates prerequisites and reconstructs canonical receipts from either input.
Old save fields retain their original meanings. Restoring an old slot does not
start a retired game.

## Consequences

Three games remain on the current route. Equipment can be operated while the
world clock runs. Access to the armory remains optional through DATA-01. The EDGE
reserve, CLOUD shortcut and optional SUPER telemetry persist in historical saves.
Their former games and UI are retired. No authored geometry was regenerated for
the workstation pass.
