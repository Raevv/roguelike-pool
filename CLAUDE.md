# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

"Roguelike Pool": a top-down 2D roguelike pool game for **mobile, portrait orientation**, built incrementally, one step at a time as the user asks. The user writes in French and UI text is French. The user tests in Play Mode after each step and expects an exact checklist of what to test. Current stage: a playable sandbox. There is an American pool table with the 15 numbered balls racked, working pockets, aiming/shooting, and the scratch rule (cue ball pocketed → shot cancelled). Everything is in stylized pixel art. There are no turns, scoring, game rules or roguelike systems yet.

## Progress

### Done
1. **Table and shooting.** Top-down table with bouncing cushions. Cue ball with realistic 2D rolling friction. Aim by holding and dragging the pointer. Power gauge on the right and a TIRER confirm button. Trajectory preview: the path to the first contact, a ghost ball, a short post-bounce segment and the object-ball direction; it stops at pockets. Shooting is only possible when every ball has stopped.
2. **Balls, portrait, pockets, scratch.** The 15 American balls (solids, stripes, numbers) in a regulation 8-ball rack: 1 at the apex, 8 in the middle, one solid and one stripe in the back corners, the rest random. Portrait layout for mobile, with the gauge on the right for the thumb. Fine-aim toggle. Real pockets with angled jaws. Pocketed balls fall and disappear. Scratch: the shot is cancelled and the whole table restored to its state just before the shot.
3. **Pixel art overhaul (visual only).** Low-res render at 24 px/unit with integer upscale, low-res bloom and vignette. Painted shadows and AO. Impact sparks on ball-ball hits and ambient dust motes. Pixel-art UI with a bitmap font and an ARPG-style power bar that reddens, glows and throws embers near maximum.
4. **Style feedback pass.** Dithering removed everywhere (crisp pixels, smooth gradients). Balls made brighter and readable. Gauge redone as a solid Diablo 3-style bar with separators at 25/50/75 %. Fixed the "balls show only shadows" bug (stale static sprite cache with domain reload disabled).

### Not started (candidate next steps, not yet decided with the user)
- **Game rules:** 8-ball groups (solids/stripes), turns or shot counter, fouls (no rail after contact, wrong ball first, scratch), the 8 last, win/lose. Note that scratch currently *cancels* the shot instead of giving ball-in-hand: that was the user's explicit design, so keep it unless they change it.
- **Roguelike loop:** the core reason for the project. Nothing is designed yet. Ask the user before inventing systems: runs, tables/levels, objectives, upgrades/perks, currency/shop, meta progression.
- **Feel:** cue-ball spin/english (balls currently never rotate and have zero friction with each other and the cushions), screen shake, pocket-drop FX, cue strike FX.
- **Audio:** there is no sound at all yet (ball clicks, cushions, pockets, UI, music).
- **Menus and flow:** title, pause, game over, settings, saves.
- **Mobile shipping:** Android/iOS build, testing on a real device, safe areas/notches, haptics, performance profiling.

### Known issues / risks to check
- **Nothing is committed** since the user's "Projet initial" commit: all of `Assets/Scripts`, `Pool.unity`, `CLAUDE.md`, the ProjectSettings changes, `AgentScripts/` and `Screenshots/` are untracked or modified. `Screenshots/` (verification captures) should probably be gitignored or deleted. Commit only when the user asks.
- **Never tested on a real phone.** Touch aiming uses `Pointer.current` (primary touch only). The UI and the gauge slider haven't been tried with fingers. Notches/safe areas are not handled.
- **`Mobile_RPAsset` has `renderScale` 0.8.** Check whether it degrades the pixel-perfect render-to-texture on mobile quality. The Editor currently runs the PC quality level.
- **Small screens:** if even scale 1 can't fit the table (screen narrower than about 500 px for the left area), the rig keeps scale 1 and the table gets cropped. There is no fallback.
- **Editor preview:** a Game view zoomed below 1× drops whole pixel rows, so balls can look broken. Use the 540×960 preset and enlarge the window. This doesn't affect devices, which display at 1:1.
- **Performance:** `PowerGauge` redraws and uploads its texture every frame. `TableArt` bakes about 260k pixels at startup and the lamp spill is a large texture. This is fine on PC; profile on mobile.
- **Visual vs physics size:** ball sprites are 13 px (0.542 units) vs a 0.572 physics diameter, so racked balls show a hairline gap. This was intentional, so the numbers sit centered.
- **Gameplay tuning, unconfirmed by the user:** side pocket jaws reject shallow-angle shots (realistic, maybe too hard; `sideMouthHalfWidth`). Breaks sometimes pocket nothing (rack gap 0.002, restitution 0.97 × 0.97). Friction values are tunable in `Ball`.
- **Bitmap font coverage:** A–Z, É, digits and `% . : - !` only. Any new UI text with other accents (È, À, Ç, apostrophes) needs new glyphs in `PixelFont`.
- **Template leftovers:** `Assets/TutorialInfo/` and `Assets/Scenes/SampleScene.unity` are unused and can be deleted. The `Assets/Editor/HubForceResolve.cs` deletion shown at the start was part of the user's own commit.
- **Static state:** domain reload is disabled. Any new static cache/registry must be reset on Play (see Agent scripts below).
- **Unrelated Editor warning:** "Account API did not become accessible" comes from the Unity account/network, not the project.

## Architecture

- **Everything is built at runtime from code.** `Assets/Scenes/Pool.unity` (the only build scene) contains just a Main Camera and a `Game` object with `GameBootstrap` (its serialized `uiPanelFraction` = 0.2 overrides the code default). `GameBootstrap` creates the table, the balls (`BallFactory`, standard 8-ball rack), the shot UI, `TableRules`, `CueController` and the rendering/FX setup. There are no prefabs or art assets.
- **Pixel art rendering (Dead Cells-inspired: dark cool palette, warm glowing accents).** The work is split by layer:
  - **World rendering:** `PixelCameraRig` renders the world at exactly `PixelArt.PPU` = 24 px/unit into a point-filtered low-res RenderTexture. The texture is shown full screen by a RawImage at an integer `Scale`, the largest scale at which the table fits left of the panel. A separate `ScreenCamera` clears the real screen.
  - **Post-processing:** a global Volume created in code (`CreatePostProcessing`: bloom threshold 0.95, vignette, contrast). It runs at the low resolution. Don't use URP's PixelPerfectCamera: its upscale mode is incompatible with post-processing.
  - **Pointer input:** must go through `PixelCameraRig.ScreenToWorld`, not `Camera.ScreenToWorldPoint`.
  - **Art:** every texture is generated procedurally in `Assets/Scripts/Art`. **No dithering (user preference):** shapes stay pixel-crisp, but gradients are smooth (`PixelArt.Ramp` interpolates palette colors) and there is no per-pixel noise. Balls use clean 3-tone cel shading. `PixelArt` holds the palettes, ramps and noise. `PixelFont` holds the 5×7 UI font and the 3×5 ball digits. `TableArt` bakes the table into one texture: wood, felt, painted AO and lamp pool, cushions, brass pockets. It also builds the floor, the table shadow and the glowing diamonds. `BallArt` makes the 13 px balls plus their shadow, and `CueArt` the cue. Art pixels map 1:1 to texels: ball sprites use PPU = 24 × diameter because the ball transform is scaled. Keep odd-sized sprites centered on pixel centers.
  - **Bloom:** glow = `PixelArt.GlowMaterial(hdrTint)` (Sprites/Default with an HDR `_Color`). Keep regular colors below the bloom threshold.
  - **FX:** `ImpactFX` (sparks and flash on ball-ball hits, triggered by `BallImpactFX` on each ball) and `AmbientMotes` (dust in the lamp light).
  - **UI:** uses the same pixel grid. `ShotUI` canvas: ConstantPixelSize with `scaleFactor = rig.Scale`, and `referencePixelsPerUnit = 1`, so 1 canvas unit = 1 art pixel. Set Image sizes explicitly, because `SetNativeSize` runs before the scaler applies its PPU. `PixelUI` holds the panel, buttons (SpriteSwap states with baked labels), banner and text. `PowerGauge` is drawn as an ARPG-style experience bar (user asked for a Diablo 3 XP bar feel). It is a brass-framed glass tube with a solid continuous fill: gloss strip, rising light sweep and bright leading edge. Ornate separators with brass studs sit at 25/50/75 %. The texture is redrawn every frame and laid over an invisible Slider. Its heat color shifts with the value, and above 85 % it adds a pulsing halo and UI pixel embers.
- **Scale: 1 world unit = 10 cm.** The table is a 9ft table in portrait: play area 12.7 × 25.4 units centered at the origin, long axis along Y. The head/cue ball is at the bottom (`HeadSpot`) and the rack apex at the top (`FootSpot`). The ball radius is 0.286.
- **Physics:** legacy Rigidbody2D. The project settings have 2D gravity set to 0, `bounceThreshold` 0.1, and a fixed timestep of 1/120. `Ball` uses zero built-in damping and applies cloth friction manually in `FixedUpdate` (constant deceleration + speed drag + snap-to-stop). `Ball.All` (active balls only) / `Ball.AllStopped` gate shooting. Physics materials use `Multiply` combine, so effective restitution = ball × cushion. `TrajectoryPreview` relies on this to predict the cushion bounce exactly, and it stops the line where the path enters a pocket.
- **Table and pockets:** `PoolTable` builds 6 trapezoid cushions (`PolygonCollider2D`) cut at the pockets, with 45° corner jaws, plus safety walls at the outer rail edge. `Pocket` checks distances each `FixedUpdate` (no triggers): a ball whose center is within `captureRadius` calls `Ball.FallInto`, which runs a short animation, deactivates the ball and raises the static `Ball.Pocketed` event.
- **Shot flow:** `CueController` reads `Pointer.current` (mouse in the Editor, touch on mobile). The cue follows the pointer while it is pressed. Fine aim comes from the UI toggle or Shift. Wheel/arrows change power, Space shoots. `Shoot()` calls `TableRules.TakeSnapshot()` before the strike. If the cue ball is pocketed, `TableRules` waits `restoreDelay`, then calls `Ball.ResetTo` on every ball in the snapshot. `Busy` blocks shooting meanwhile.
- **UI layout:** the right `uiPanelFraction` (0.2) of the screen holds the FORCE gauge, VISÉE FINE and TIRER. A message banner appears over the table. `ShotUI.IsOverUI` raycasts the canvas directly, which is reliable for touch.
- `AgentScripts/` (outside `Assets/`) holds editor builder/test scripts run with `unity command script run --file AgentScripts/X.cs --entry X.Run`:
  - `BuildPoolScene.cs` regenerates the scene.
  - `SetPortrait.Run(w, h)` adds/selects a portrait Game view size and sets the mobile orientation to Portrait. Use 540×960, the native art resolution at scale 1. In a Game view zoomed below 1×, Unity drops whole pixel rows/columns and small sprites like balls disappear; `GameViewFit.cs` sets the zoom to fit. `GameViewInfo.cs` reports the zoom.
  - `CropPng.Run(src, x, y, w, h, zoom, dst)` zooms into a screenshot, to inspect UI pixel art.
  - The rest are Play Mode tests: `ShotTest`, `PocketTest.Scratch/Break` and `CombinedScratchTest`.
  - `CaptureArt.Run(x, y, w, h, zoom, path)` saves a nearest-neighbour zoom of the low-res world render, to inspect pixel art.
  - `BreakCaptureAll` captures a break at the moment of impact. `script run` accepts only one `--file`, so it is a merged copy that includes `CaptureArt`.
  - Capture screenshots with `capture game_view --width 1080 --height 1920`, since the default is 1280×720.
  - After a recompile, wait until `editor status` shows no compile or domain reload before entering Play Mode. Otherwise the old code runs.
  - **Domain reload is disabled on Play** (Enter Play Mode Options): statics survive between Play sessions, but runtime-created Unity objects they reference are destroyed. Every static cache or registry needs a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` reset and a Unity null check (`BallArt`, `Ball` do this). Always test **two consecutive Play sessions**, and check the real window with `capture editor_window --window Game`: `capture game_view` re-renders and can hide such bugs.
  - Python is not installed on this machine.

## Engine and packages

- **Unity 6000.6.9f1** (see `ProjectSettings/ProjectVersion.txt`). Look up APIs for this version; don't assume older Unity behavior.
- **URP 17.6** with two quality tiers: `Assets/Settings/PC_RPAsset` + `PC_Renderer` and `Mobile_RPAsset` + `Mobile_Renderer`. Rendering changes usually need to go in both.
- **Input System only** (`activeInputHandler: 1`). The legacy `UnityEngine.Input` API will throw. Use `Assets/InputSystem_Actions.inputactions` (it ships with the template's Player/UI action maps) or `UnityEngine.InputSystem` directly.
- **uGUI** (`com.unity.ugui`) is installed. UI Toolkit is available as a built-in module.
- **`com.unity.pipeline`** is installed, so an agent can drive a running Editor: compile, run tests, eval C#, enter Play Mode, take screenshots. Use the `unity-pipeline` / `unity-cli` skills for these jobs instead of guessing at command-line flags.
- Physics: the game uses **2D physics only** (legacy Rigidbody2D). The 3D PhysX and 2D Physics Core modules are enabled but unused.

## Commands

There are no build scripts, test assemblies, or CI yet. You compile, test, and build through the Unity Editor, or through the `unity-pipeline` skill against the open Editor. Typical loop:
- `unity command script recompile`, then poll `script recompile_status`, then wait for `editor status` to show no compile/reload.
- `unity command editor.playmode start`.
- Run `AgentScripts` tests, e.g. `unity command script run --file AgentScripts/PocketTest.cs --entry PocketTest.Scratch --args '[-6.45, -12.8, 0.4]'`. Expect `cuePocketed=True … maxPosDelta=0.0000`.
- Check `unity command console status`.

If you add real tests, put them in an asmdef-based Edit Mode or Play Mode test assembly using the Unity Test Framework, and record the commands here.

## Conventions

- Version control is git with Unity's standard `.gitignore` (`Library/`, `Temp/`, `Logs/`, `UserSettings/` are ignored). Always commit `.meta` files alongside their assets, and never hand-edit GUIDs.
- Scenes, prefabs, and `.asset` files are YAML. Prefer making changes through Editor scripts or the Editor itself over hand-editing serialized YAML.
