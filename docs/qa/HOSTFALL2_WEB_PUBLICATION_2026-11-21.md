# HOSTFALL 2.0 browser publication

November 21, 2026. The public pages use the Unity WebGL candidate from
`apps/unity/Builds/Web/HOSTFALL-2-Candidate`. The six files in
`HOSTFALL-WebGL-v2-itch.zip` match the local build byte for byte. The ZIP passed
integrity checking. SHA-256:
`98ca80d12a189c707d2a873e3e33de11d849259e327592b6a743a43abb3c26e7`.

- [itch.io](https://littleprank.itch.io/hostfall) was published with free browser
  access and click-to-launch. A private Firefox window showed the public page,
  loaded the player and rendered the main menu. The page has a dark theme,
  Russian description, controls, an inline cover and three screenshots. The cover
  is in the description because the Chrome extension could not use the separate
  catalog cover uploader.
- [Websim](https://websim.com/@softfire3062336/hostfall-2-northline) serves revision
  4 at the public address. The Websim CLI uploaded all six build files. A guest
  session confirmed the revision in the player iframe and rendered the main
  menu. The project has a description and custom thumbnail. `Public` allows
  anyone with the link to play. Posting to the home feed requires profile setup.

These checks covered public access and rendering through the main menu.
The Websim player logged a warning about the URP Edge Adaptive Spatial Upsampling
shader. The menu rendered after the player received focus. Campaign progression
and performance on other hardware were outside the publication check.

## Desktop archives update

Both ZIP archives provided on the desktop passed integrity checks and contain
the same six game files. SHA-256 for the Websim archive is
`cdfcbc4bc1b8c71af53e6d736de3a1d1e6f3d5217ef468db277e51173e6581bc`.
The itch.io archive SHA-256 is
`f64b28b33c07146eca43ad5c8639fb355e75f19db0d868374177d0468e850cc0`.

- [Websim](https://websim.com/@littleprank/hostfall-2-northline) launched
  revision 5 through the public link in a guest browser.
- itch.io accepted the ZIP and the public page points to upload `20713840`.
  The CDN data file matched the archive SHA-256 and the direct player reached
  the main menu in Chrome. The guest iframe cached a truncated data response
  and reported `Invalid binary data file size`. Further itch.io work stopped
  at the user's request.
