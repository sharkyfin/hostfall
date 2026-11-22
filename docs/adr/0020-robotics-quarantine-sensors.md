# ADR 0020 — Local robotics quarantine

## Context

RBT-01 controls an independent safety lane. Its success must depend on a
physical sentry, door positions and interlocks. Computer distance and native
render cost matter because the rest of the facility runs during the session.

## Decision

Assign the original R26 actor a patrol, calibration loop or cage transfer through
its existing navigation. Use authored trigger volumes as inputs to a pure
ContainmentSession. The session saves requested and measured leaf openings,
camera choice and verification time. Closing the leaves requires clear beams.
Player presence opens both leaves. Local release panels allow exit.

Once the sensors confirm the settled cage, the console disables the local safety
relay and opens the Central Ring. A destroyed R26 still allows the relay to be
isolated. The central core's separate final gate remains under Recovery control.

A single disabled URP camera renders on demand into a shared 480×270 texture at
most once per 160 ms between camera selections. The projected desktop pulls the
current feed only while focused. Controller changes and optical views are part
of the same physical scene. [Unity URP render requests](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/User-Render-Requests.html)

## Consequences

R26 keeps local behavior independently of the server process. Its current
order and the shutters survive reload. A local exit on the inside of the Ring
supports older saves beyond the newly locked gate. Gameplay uses the same
button and door state on the monitor and during direct interaction.
