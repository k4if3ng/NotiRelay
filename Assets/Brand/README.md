# NotiRelay brand assets

`NotiRelay.svg` is the source of truth for the application mark. It combines an
N-shaped relay route, a source endpoint, a destination endpoint, and a subtle
forward indicator. `NotiRelay.Tray.svg` is a simplified small-size variant.

The generated PNG and ICO files live directly under `Assets/` because the MSIX
manifest and tray integration consume those files. Keep the SVG sources when
regenerating them; do not edit generated raster assets by hand.

Primary colors:

- Windows blue: `#168CFF`
- Deep blue: `#0047B3`
- Relay cyan: `#35D8FF`
- Relay highlight: `#FFFFFF`

The tray asset must remain recognizable at 16 px. Avoid adding text, extra
endpoint rings, or a detached arrow to the small-size variant.
