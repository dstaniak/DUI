# DUI — Project Details

An educational prototype for adults about driving under the influence of alcohol,
including people who actually do so. The game aims for an approachable, playful
tone without shaming its audience.

The first game builds on the Snake mechanics from the DungeonSlime tutorial:
the player drives a red car and collects randomly selected beer, vodka, powder,
mushrooms, and sausages. Alcohol and powder extend the convoy of police cars,
snacks lower the alcohol level, and powerups temporarily change the visuals
and controls.

For a short introduction to the gameplay, see the [README](../README.md).

## Running the Game

Requires .NET SDK 10. From the repository root:

```sh
dotnet tool restore
dotnet run --project DUI.Desktop
```

The game opens directly onto the playfield. The DesktopGL project supports
iteration on macOS, Windows, and Linux. MGCB builds the assets during compilation.
Compiling shaders on macOS/Linux requires Wine configured for MonoGame MGFXC,
as in the source project.

## Downloadable Packages and itch.io

GitHub Actions runs tests on Windows, Linux, and macOS (Intel/Apple Silicon).
A push to `main` or a manual run of **Desktop builds** produces four packages
containing .NET and the game assets. They remain available for 30 days as the
`DUI-<version>-all-platforms` artifact. Pushing a `vA.B.C.D` tag also creates
a draft GitHub Release with the archives and SHA-256 checksums.

Publishing to itch.io is manual. Test packages for macOS use ad hoc signatures;
[Developer ID setup](MacSigning.md) enables signing and Apple notarization
for releases. The [release guide](Releasing.md) describes downloading packages,
checking the game, and uploading it to itch.io.

## Retro Presentation

The entire frame, including the UI, is **1280×720 pixels**. Tiles are 80×80,
and the storefront strip is 1280×160, keeping the shapes of cars, bottles,
snacks, and shaded artwork readable. `PointClamp` preserves sharp pixels
when scaling. The image fills the entire window without borders or letterboxing.
For other window aspect ratios, each axis scales independently; 16:9 gives
the natural proportions. Physics, speed, and collision boundaries remain
in playfield units.

The alcohol level, measured in per mille (‰), controls a double-vision shader
on the playfield layer. The UI is rendered separately and composited after
all world effects. At 0.0‰, or while powder is active, the shader is skipped.
Above zero, without powder, a copy of the image orbits the window center
once every 8 seconds; the image itself does not rotate. Alpha starts at 0.1
and increases by 0.01 for every 0.1‰, up to 0.5: it is 0.12 at 0.2‰,
0.2 at 1‰, and 0.3 at 2‰. It stays at 0.5 from 4‰ onward.
The radius grows linearly by 12 pixels per 1‰ at 720p: 2.4 pixels per beer
and 4.8 per vodka. Sausages reduce the effect. The radius scales with the
shorter window dimension, and the motion remains circular at other aspect
ratios. Only the duplicate uses linear filtering for smooth subpixel movement;
the original retains `PointClamp`. Beyond the edges of the shifted copy,
only the original is visible. Pausing or losing freezes the orbit;
RETRY resets the effect.

The artwork itself creates the retro look: simplified shapes, a pixel font,
C64-inspired colors, and selected two-color patterns in shadows.
Each display segment contains 12 slender bottles. Their sides alternate
between similar, darker shades of their own color: darker red on red bottles,
without black notches. Their centers remain solid. Cars and the collectible
beer bottle are redrawn as simple icons with large blocks of color and
a single shadow, without gloss, reflections, or fine outlines.
This simplifies the sprites rather than applying a resolution-reduction filter.
Patterns are part of the textures; no screen-wide dithering or 16-color limit
is applied. Original texture shades, transparency, and fading during pauses
retain their detail. `C64Palette` supplies the presentation's black and the
small white highlights on the police lightbar from the
[Pepto PAL palette used by VICE](https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/C64/pepto-pal.vpl).

The storefront is assembled from elements scaled equally along both axes.
The 2172×724 original forms a 480×160 central facade with an entrance and sign;
repeating side segments fill the remaining width. Doors, lettering, and goods
retain their proportions, while the shop strip still occupies the top two tiles.

The UI uses a logical 320×180 layout at 4× zoom, rendered into its own transparent
720p buffer and scaled in the same way as the playfield. The counter, buttons,
pause panel, and game-over panel are unaffected by double vision, rainbows,
or shaking. All UI text uses a C64 screen font on an 8×8 grid: alcohol digits,
the ‰ symbol, PAUSE, RESUME, RETRY, and panel headings.
The bar uses C64 Orange `#6F4F25` for its background and Yellow `#B8C76F`
for text and borders. Pause and game-over panels share the palette and
rectangular borders. Buttons are flat: focus or hover gives orange text
on yellow, and pressing a button brightens its background to white.
White icons and star fills retain their contrast; empty stars use C64 light
gray `#959595`. All UI colors are assigned to roles in `C64Theme`: background,
text, accent, pressed state, muted elements, and symbols. A new variation
only requires changing those assignments and restarting the game.
The bitmap font scales without smoothing: standard characters are 32×32 px,
and panel headings are 64×64 px at 720p.

## Rules and Controls

- Arrow keys / WASD or a controller's D-pad / left stick change direction.
  Keyboard presses are buffered from window events in arrival order, even
  if the key is released before the next frame. All presses reach the turn
  queue before movement; holding a key does not repeat the command.
- The car travels at 400 playfield units per second. Powder raises this
  to 480 (1.2×) for 10 seconds, mushrooms lower it to 200 (0.5×), and both
  effects together give 240 (0.6×).
  Straight paths pass through the centers of 80×80 tiles. A turn waits for
  the nearest tile edge, then connects row and column axes with a 40 px
  radius arc. A 90° turn takes about 157 ms of simulation time at constant
  speed. A command up to 20 px beyond the edge (50 ms at 400 px/s) begins
  a turn immediately: the radius shrinks by the overshoot distance, to
  a minimum of 20 px, and the arc ends tangent to the correct axis.
  The car neither moves backward nor teleports. Beyond that tolerance,
  the turn waits for the next edge. A U-turn still changes rows by exactly
  80 px; a short straight section connects the tighter arcs.
- The queue holds two pending turns. A maneuver already in progress always
  finishes before the next begins. Directions are checked against the last
  planned heading; immediate reversals, repeated headings, and queue overflow
  are ignored. The sequence “right → down → left” performs a smooth U-turn.
- Segments replay the head's positions and rotations from its trail,
  spaced 80 units apart along the path. A new car appears behind the tail
  without moving the others. The simulation uses 5 ms substeps and visual
  interpolation.
- Each tail segment is a silver-blue police car. Its roof lights alternate
  between bright blue and red every 250 ms. The lights are opaque 8×12 blocks
  with a small white highlight, keeping the flash color distinct.
  The lightbar rotates, scales, and hops with the car. Pausing or losing
  freezes the lights; RETRY starts them in the blue state. The player's
  car stays red.
- Gentle swaying has an amplitude of ±1.5 units and ±2°, with a 1.2 s period.
  Its phase depends on distance traveled, so each segment repeats the head's
  movement. It is a visual effect; pausing freezes it, and RETRY resets its phase.
- Tail collisions use moving circles with a radius of 32, excluding the
  head's immediate neighbor. Checks also cover motion between substep endpoints.
  Wall collisions use a 74×36 car footprint rotated to match its heading,
  excluding the texture's transparent margin. Pickup collection retains
  a head radius of 40. Collision boundaries follow the visible fence
  (x=46–1234, bottom y=666), rather than its tile edges. The closed gate
  occupies x=560–720 and extends to y=654; its corners are checked against
  the rotated car. The storefront ends at y=160.
- One pulsing pickup moves around the playfield, bouncing off the parking
  lot boundaries. At the start, after RETRY, and after each collection,
  the next item is chosen randomly: **50% beer, 20% vodka shot, 5% powder,
  5% mushroom, 20% sausage**.
  Beer adds 0.2‰ and one car, releasing a splash of beer and foam on collection.
  Vodka adds 0.4‰ and one car. A sausage subtracts 0.4‰, down to a minimum
  of 0‰, without changing the convoy's length. The counter starts at `0,0‰`
  and displays one decimal place with a decimal comma; `‰` is a glyph
  in the same C64 font.
  The next pickup appears on the opposite side of the parking lot.
  Hitting the storefront, fence, closed gate, or your own convoy ends the game.
- White powder in a zip-lock bag adds one police car and disables double
  vision, playfield shaking, and the rainbow for **10 seconds**.
  It also speeds up the convoy simulation to **1.2×**; spacing between cars
  stays unchanged, while turns and car animations follow the convoy's clock.
  It changes neither the alcohol level nor reversed controls; with a mushroom
  active, the speed multipliers combine to 0.6×.
- A mushroom with a pointed cap and thin stem reverses every direction
  (left ↔ right, up ↔ down) for **10 seconds**, on keyboard, controller,
  and gestures. It also slows the entire convoy to **0.5×** (200 px/s),
  giving the player more time to react. It changes neither the alcohol level
  nor the convoy's length. `rainbowEffect` overlays the entire playfield
  layer, below the UI, with a rainbow gradient at 0.22 alpha. The gradient
  moves smoothly through a cycle every 6 seconds.
- Collecting the same powerup again refreshes its duration to 10 seconds.
  Both powerup timers, shaking, and shaders use ordinary game time,
  independently of convoy speed changes. Pausing or losing freezes them;
  RETRY removes the effects and restores normal controls and speed.
  Music follows the current convoy multiplier (1.2×, 0.5×, or 0.6×),
  changing both playback speed and pitch without restarting the track.
  Powder hides the rainbow and shaking, but their timers continue running.
  The mushroom's slowdown and reversed controls remain active. If an effect
  still has time left when the powder expires, it becomes visible again.
- Three fixed icon slots follow the alcohol counter and ‰ symbol: vodka,
  powder, and mushroom. These are separate white 28×28 px sprites with
  transparent backgrounds, matched to the pixel counter. An active item's
  icon blinks for 0.3 s on and 0.3 s off. During the final second, it blinks
  three times faster: 0.1 s on, 0.1 s off, beginning with a visible flash
  at the one-second boundary. The countdown uses the effect's own timer
  (3 s / 10 s / 10 s). Collecting it again starts with a visible icon;
  expiry and RETRY turn it off, while pausing or losing freezes the blinking.
  Indicators show active timers even when powder temporarily hides their
  visual effects. All three move along with the alcohol counter as it
  gains digits.
- The score is the number of convoy segments, including the player's car.
  Five white 28×28 px stars follow the three powerup slots. Their C64 light
  gray backgrounds (`#959595`) are always visible; white fills grow from
  left to right on a scale of 0–84 segments, accurate to one pixel at 720p.
  Gaps between stars do not count toward progress. The start and RETRY
  award 1 point for the head; 42 segments fill 2.5 stars, and 84 or more
  fill all five. Beer, vodka, and powder add a point; sausages and mushrooms
  leave the score unchanged. Alcohol remains a separate counter.
  Stars do not blink and are unaffected by world effects.
- Vodka triggers strong horizontal shaking of playfield tiles and sprites:
  ±24 pixels at 720p, 18 cycles per second, for 3 seconds. The separately
  rendered UI does not shake. Another vodka refreshes the duration;
  beer and sausages do not interrupt it. Pausing or losing freezes both
  phase and countdown, while RETRY resets them. Shaking is purely visual
  and does not affect collisions; exposed playfield edges are black.
- The playfield is a parking lot outside a shop selling alcohol and snacks.
  The storefront occupies the top two rows. Chain-link fencing runs along
  the sides and bottom, with a closed two-tile-wide gate in the middle
  of the bottom fence. L-shaped corner pieces join the side fencing
  to the bottom sections.
- A subtle grid marks the 80×80 tile boundaries on asphalt and grass.
  C64 gray lines (`#959595`) are 1 pixel thick at 35% opacity, including
  intersections. They end at the fence and gate, sit beneath cars and
  pickups, and receive the same effects as the playfield.
- Two symmetrical grass islands each measure 2×4 tiles. Cars can drive
  across them: when a car's front crosses the curb from asphalt to grass,
  it hops 12 playfield units and grows to about 128% scale. Takeoff uses
  `EaseOutCubic`, landing uses `EaseInQuad`, and the whole hop lasts 200 ms.
  After landing, the scale stays at 106%; leaving the grass restores 100%.
  Driving on grass, leaving it, or being in an active hop does not trigger
  another hop. Pausing freezes its current phase; RETRY starts a new movement
  animation. The effect is calculated separately for each segment and does
  not change movement or collisions. One drivable row remains above and
  below the grass islands.
- Pulsing is purely visual; collision sizes stay fixed. Pausing also freezes
  the splash, and RETRY removes remaining particles.
- Escape / Start / Back or the PAUSE button pauses the game. RESUME continues;
  RETRY starts a new run with 1 point and `0,0‰`. Buttons also accept Enter
  or controller A. Pausing, resuming, and RETRY clear undelivered key presses
  and gestures. Returning to the window synchronizes held-key state so
  a key released outside the window cannot remain stuck.
- A swipe on the playfield enters the same queue. On desktop, it can be
  tested by dragging with the left mouse button. One contact means one move;
  a new contact allows the next move to be planned.
- Losing application focus pauses the game. Returning requires an explicit resume.

There is no main menu or button to return to one. The desktop window closes
with the standard system button.

## Structure

- `DUI.Desktop` — entry point and dependency on the DesktopGL backend.
- `DUI.Game` — shared game code, Gum UI, assets, and the required parts of
  the tutorial library. `DUI.Game.projitems` includes the source files in
  the platform project, allowing a future mobile host to use its own
  MonoGame backend.
- `KeyboardSteering` — arrow-key/WASD press ordering, suppression of automatic
  repeats, and input reset. The buffer holds eight events; the movement
  model still accepts at most two pending turns and rejects overflow
  and invalid directions.
- `ConvoyMovement` — turn queue, turns with a 20 px tolerance, fixed substeps,
  and tail collisions. `CarPose` stores a car's center, rotation, and distance
  traveled; `ConvoyTrail` limits history to the convoy's length plus room
  for a new segment. `ConvoyCollision` checks moving circles and the parking
  lot boundaries.
- `CarConvoy` — connects the model to rendering, updates hops, and checks
  boundaries after each substep. It draws the red player car and the police
  cars in the tail. `SegmentCount` exposes the score including the head.
  `PoliceLightbar` overlays two-frame roof lights using model time and the
  same transform as the car; it owns and disposes of a single-pixel texture.
  `CarSway` adds gentle visual swaying.
- `Easings` — static interpolation functions (30 curves plus `Linear`).
  `CarHop` uses `EaseOutCubic` and `EaseInQuad` for takeoff and landing.
  `ConvoyMovement` constructs tangent arcs geometrically while maintaining
  constant speed.
- `ParkingLot` — parking layout, driving boundaries, a proportionally assembled
  storefront with a central facade and repeated display sections, plus shared
  grass-tile geometry and smooth car scaling to 106%.
  It also draws the translucent tile grid and owns a single-pixel texture,
  disposed of by `GameScene.UnloadContent`. `GridOpacity` and `GridLineWidth`
  control the grid's appearance. `ParkingGeometry` defines fence edges,
  a separate gate footprint, and a rectangular pickup movement area that
  fits in front of the gate. `CarHop` tracks each segment separately and
  detects its front crossing a curb; rendering uses the movement model's time.
- `Pickup` — shared movement, pulsing, and collisions for all five items.
  `PickupType` distinguishes their kinds, and `PickupRules` defines selection
  probabilities, alcohol changes, and convoy growth. `BeerBurst` draws drops
  and foam only after beer. `ScreenShake` times the 3 seconds of shaking
  after vodka; `GameScene` offsets only the playfield drawing batch, before
  the Gum UI.
- `GameDisplay` — maps the playfield and 1280×720 frame to the entire window
  and defines the logical 320×180 UI layout at 4× zoom. Its inverse transform
  handles gestures and the Gum cursor up to the edges of the visible playfield.
- `RetroRenderer` — owns two 1280×720 buffers: the world and transparent UI.
  It applies double vision and the rainbow to the world, then composites
  the unaffected UI. Both layers scale to the same area using `PointClamp`.
  `DuiGame` supplies the alcohol level, active game time, and powerup flags;
  it switches buffers between `GameScene.Draw` and `DrawUI` and disposes
  of both on exit. Shaders belong to the main `ContentManager`.
- `C64Theme` — shared font, color roles, and rectangular surfaces using
  the C64 palette. `C64Button` retains Gum input handling while supplying
  its own focus, hover, pressed, and disabled colors.
- `ScoreStars` — five dimmed stars with white fills clipped to whole pixels
  based on `GameScene.Score` (convoy length). `GameSceneUI` positions them
  after the powerup slots in the separate UI layer.
- `DUI.Tests` — regression tests that run without a window or graphics device.
- [Class map](ClassMap.html) — application responsibilities and flows.

## Audio

The supplied `SourceAssets/pydim-byte-hook-620646.mp3` replaces the tutorial
music. MonoGame builds its Vorbis source (`Audio/theme`) as a `SoundEffect`.
`DuiGame` loads it once and `AudioController` owns a separate looping playback
instance at 35% volume, leaving room for the pickup effects.

After each scene update, `DuiGame` sends `GameScene.MovementSpeedMultiplier`
from `PowerupEffects` to `AudioController.SongPlaybackRate`: normal 1×,
powder 1.2×, mushroom 0.5×, both 0.6×. Collection, independent expiry,
refresh, and RETRY update this value in the same frame. The multiplier
shares the movement calculation's speed constants; it describes the current
state, not the average of a frame spanning an expiry.
The player sets `SoundEffectInstance.Pitch` to `log2(rate)`, changing speed
and pitch in place without restarting or seeking the track. Effects retain
their normal speed. Music and effect volumes are controlled separately;
mute affects both channels, including effects started while muted.

The full stereo track occupies about 22.2 MiB of PCM audio at runtime.
Preloading avoids decoding or allocating another track when a powerup changes
speed. The release includes `Audio/theme.xnb`; it does not need a separate
streamed OGG. `scripts/release.py` distinguishes SoundEffectProcessor from
SongProcessor when checking package contents.

`GameScene` preloads effects and chooses one using the collected `PickupType`
before spawning the next item: beer burps, vodka exhales, powder sniffs,
and mushrooms and sausages share an eating crunch. A separate car-crash
sound plays once when either a boundary or tail collision changes the state
from Playing to GameOver; repeated collision notifications are ignored.
Effects are owned by the scene's ContentManager. UI clicks and pickup-wall
bounces retain their existing sounds. Music continues during ordinary pause
and game over at the frozen powerup rate. RETRY restores 1× at the current
track position. Losing app focus pauses audio; regaining focus resumes it
without changing the rate or position, while gameplay still waits for RESUME.

[Audio source notes](../SourceAssets/Audio/README.md) record the CC0 effects,
original files, and reproduction steps. `SourceAssets/Audio/build_audio.py`
uses FFmpeg to create short mono 44.1 kHz PCM effects with edge fades and
-3 dBFS peaks, and the full stereo Vorbis track. MonoGame builds the finished
assets and includes `Audio/credits.txt` in release content.

## Mobile

The code accounts for touch gestures, pausing on focus loss, and a fixed
1280×720 playfield and frame fitted to the entire window. Resizing the screen
does not change rules, collision geometry, or game speed. Updates run at
a fixed 60 Hz, including on higher-refresh-rate displays.

The first mobile version should use landscape orientation to preserve
the tutorial's layout. The project does not yet contain Android/iOS hosts
or APK/IPA packages. The next step is choosing the first platform and adding
its host, SDK, safe areas for screen cutouts, and physical-device tests.
Gestures and button sizes will then need evaluation on a real phone.
Portrait mode currently scales the playfield; a dedicated portrait layout
requires a separate presentation decision.

MonoGame provides separate Android and iOS backends:
[supported platforms](https://docs.monogame.net/articles/getting_started/platforms.html),
[touch input](https://docs.monogame.net/articles/getting_to_know/howto/input/HowTo_UseMultiTouchInput.html).

## Verification

Run from the repository root:

```sh
dotnet build DUI.slnx
dotnet run --project DUI.Tests
```

The 64 tests cover waiting for tile edges and commands outside the tolerance,
500 consecutive turns without leaving the grid axes, all turn directions,
queuing, rejection of invalid commands, constant speed, interpolation,
and different frame-time partitions. They check trail replay through four
corners, spacing, growth, bounded trail memory, tight U-turns, crossing
the tail between substeps, parking boundaries, pause, and restart.

Six `SteeringTests` check immediate turns up to 20 px past an edge, constant
speed without teleportation, return to the correct axis, police-car trails,
spacing, frame-time partitioning, and short key presses between updates.
They cover arrow-key/WASD ordering, reversal of the entire sequence by
a mushroom, suppression of automatic repeats, and reset on focus loss.

Animation tests cover bounded swaying, grass entry from four directions
and diagonally, takeoff and landing, no repeated hops during an animation
or while driving on grass, re-entry, and delays between segments.
Four wall regressions check ordinary and late turns with police cars near
all four edges, rotated car sides and corners, and impacts after passing
the last safe turn point and its 20 px tolerance. Four parking-geometry tests
cover asphalt inside fence tiles, passing gate corners with an angled car,
separate gate and fence edges, and a safe pickup movement area.

Three `GameDisplayTests` check the absence of borders and cropping, full
window coverage, agreement between the Gum cursor at 4× zoom and playfield
input after resizing (including odd-sized and very small windows), and
rejection of gestures in the HUD or outside the playfield. Gesture and bottle
collision tests also remain. The test program exits with a nonzero code
if any test fails.

Three `PickupTests` check the exact 50/20/5/5/20 probabilities, mixed sequences
of score changes and growth, and the 0‰ lower limit after a snack.
Four `ScreenShakeTests` check amplitude and frequency, the 3-second duration,
pause, reset, retriggering, and independence from frame-time partitioning.
Eleven `PowerupEffectsTests` check directions, both 10-second durations,
refreshing, overlapping effects, reset, time partitioning, and speeds of
480, 200, and 240 px/s. They also check car spacing, ordinary timer speed,
independent expiry of speed multipliers in either order, the portion of
a frame after expiry, and fractional ticks.

Rendering, audio, Gum interactions, and the application lifecycle also require
checks in the running game. Audio checks include the music loop and balance,
pitch and speed changes for each powerup and their overlap, independent expiry,
RETRY returning to 1× without restarting, separate channel volumes and mute,
each pickup sound, one crash sound for both boundary and tail collisions,
and focus-loss pause/resume. Visual checks include unchanged UI colors and positions
with active effects and after resizing, as well as the blinking, expiry,
and positioning of the three indicators after the alcohol counter.
These tests do not establish that the game works on a phone.

## Origins and Assets

Code and assets were copied from the local DungeonSlime project at
`ec41fb7eefb4787a652538e37bc0eb0d025998dd`. The tutorial's pace, playfield size,
UI, and original audio were initially retained; smooth turns replaced grid
movement. The supplied music and per-pickup/crash sounds now replace the
tutorial theme and shared collection sound; UI and bounce audio remain.
New car and bottle artwork replaces the characters, and the beer splash
is generated in code. A parking lot replaces the dungeon. The title screen,
its options and assets, the ImGui overlay, and desktop shader watching
were omitted. Color fading during pauses and game over remains.

Tutorial: [Building a 2D Game — MonoGame](https://docs.monogame.net/articles/tutorials/building_2d_games/).

The [car](../DUI.Game/Content/Images/car.png) and
[bottle](../DUI.Game/Content/Images/beer-bottle.png) artwork was generated
with `image_gen` using transparent backgrounds. Full prompts are in
[AssetPrompts.json](AssetPrompts.json).
The [police car](../DUI.Game/Content/Images/police-car.png) is a variation
of the simplified car, generated with the built-in `image_gen` while preserving
its silhouette, orientation, and transparency. Both cars use fewer details,
larger color blocks, and solid windows without reflections, in a C64-inspired
style. The police car's roof is blank in the texture; its lights are drawn
in code. The collectible bottle has flat glass, a single shadow, and a simple
label. The new [vodka shot](../DUI.Game/Content/Images/vodka-shot.png) and
[sausage](../DUI.Game/Content/Images/sausage.png) were created with the built-in
`image_gen` as separate retro sprites with transparent backgrounds.
Their prompts are also recorded in `AssetPrompts.json`.
The [powder bag](../DUI.Game/Content/Images/powder-bag.png) and
[mushroom](../DUI.Game/Content/Images/mushroom.png) were likewise generated
with the built-in `image_gen` as transparent pixel sprites, with prompts
in the same file.

The active [C64 parking atlas](../DUI.Game/Content/Images/c64-parking-atlas.png)
contains 16 tiles for asphalt, lines, curbs, grass, fencing, and the gate.
The [C64 storefront](../DUI.Game/Content/Images/c64-storefront.png) is a separate
panorama. Both assets and their patterned variants were created with the
built-in `image_gen`; their prompts are also in `AssetPrompts.json`.
The map definition crops tile interiors without the atlas separator lines;
`ParkingLot` scales them to the logical 80×80 size. The original
`parking-atlas.png` and `storefront.png` remain as source material.
The tutorial font and UI atlas have been replaced with the C64 theme.
Corners are assembled from parts of the same atlas's vertical and horizontal
fence sections, preserving rail spacing and mesh patterns. At each joint,
a [corner piece](../DUI.Game/Content/Images/fence-corner.png) is cropped during
rendering from artwork generated with the built-in `image_gen`.
The entire assembly is mirrored on the right. Its prompt is in `AssetPrompts.json`.

The [C64 UI font](../DUI.Game/Content/Fonts/c64.fnt) uses
[zeiche's C64 font](https://github.com/zeiche/c64font), released under CC0.
Its [source and license](../SourceAssets/Fonts/README.md) are in the repository.
`SourceAssets/Fonts/build_font.py` generates an atlas without antialiasing:
ASCII 32–95 plus the ‰ symbol, aligned to the pixel grid. The game loads
the finished bitmap font; no system font installation is required.
Alcohol levels use `decimal` so increments of 0.2 remain exact.

Active-effect indicators use separate white 28×28 sprites:
[vodka shot](../DUI.Game/Content/Images/UI/vodka-indicator.png),
[bag](../DUI.Game/Content/Images/UI/powder-indicator.png), and
[mushroom](../DUI.Game/Content/Images/UI/mushroom-indicator.png).
Their source SVGs are in `SourceAssets/UI/`. They were built from pixel
rectangles on a 14×14 grid, doubled without antialiasing, and use only
white and transparency, like the new font atlas.
