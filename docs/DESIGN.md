# QiLight — Design & Implementation Notes

Cumulative record of the gameplay and rendering work. Each section is a
self-contained feature/fix with its problem, approach, and the key files touched.
Reflects the implemented state of the code.

---

## 1. Fix: Player couldn't enter the board to draw

**Problem.** From the top-left corner spawn (`PlayField.Vertices[0]`), every
axis-aligned direction runs *along* a border edge. Pressing Space+direction started
a draw whose trail was collinear with the border; it auto-completed almost
immediately as a degenerate, zero-area cut — so drawing appeared to do nothing.

**Approach.**
- `Player` only starts a draw when the pressed direction actually leaves the border
  into the interior — `DirectionLeavesBorder` probes `BorderExitProbe = 8f` ahead and
  checks `PlayField.IsOnBorder`. Otherwise the input slides the player along the
  border.
- A minimum-distance guard (`MinDrawCompletionDistance`) prevents completing a draw
  right next to its start.

**Files:** `src/QiLight.Game/Core/Player.cs`.

---

## 2. Fix: Completing a draw collapsed the board to a sliver

**Problem.** `Territory.CompleteCut` splits the uncaptured polygon into two
sub-polygons via `BuildSubPolygon`. The "backward" (`clockwise=false`) branch walked
the border ring the wrong way and appended only one vertex on a **same-edge** cut
(the common case of drawing out and back to the same edge), so the surviving
`UncapturedPolygon` became a tiny loop — the board collapsed.

**Approach.**
- Rewrote the backward branch to walk the ring **forward** from `startEdge+1`,
  stopping after `endEdge`, using a `do…while` so a same-edge cut wraps the whole
  ring ("everything except the pocket").
- Added a degenerate-cut guard in `CompleteCut`: if either sub-polygon area is
  below `1f`, bail out (prevents zero-area slivers from ever replacing the border).

**Files:** `src/QiLight.Game/Core/Territory.cs` (`CompleteCut`, `BuildSubPolygon`),
verified against `MathUtils.PolygonArea`.

---

## 3. Player glow

**Problem.** The player was a flat 6px dot with no glow of its own.

**Approach.** `NeonRenderer.DrawGlowDot` stacks concentric additive dots (large/faint
→ bright) into a radial halo, then draws a crisp bright core. `GameRenderer.DrawPlayer`
uses it with a sine pulse (matching the trail pulse) and the player's
mode color (`Theme.Border` on the border, `Theme.Trail` while drawing). Blend state
is saved/restored so the rest of the scene is unaffected.

**Files:** `src/QiLight.Game/Rendering/NeonRenderer.cs`, `GameRenderer.cs`.

---

## 4. Player as a light source: floor pool + object shadows

**Goal.** Treat the player as a light: light the floor near it and have the other
moving objects (Qix, Sparx) cast shadows projected away from the player.

**Approach (no new .fx shader).** A dedicated full-screen **light render target**:
1. `ShaderPipeline.BeginLight` binds the light target.
2. `NeonRenderer.DrawRadialLight` draws a radial pool (bright center → black rim)
   under a pure-additive blend (`NeonRenderer.AddRGB`, `One`/`One`, alpha-agnostic).
3. `NeonRenderer.DrawShadowVolumes` projects each occluder edge away from the player
   and fills the quad with **opaque black**, carving shadows out of the pool.
4. `ShaderPipeline.CompositeLight` additively blends the light buffer onto the scene
   target before objects are drawn; bloom then softens it.

Occluders are gathered in `GameRenderer.RenderLightBuffer`: Qix edges, and each Sparx
as a short player-facing segment. The light target is created/disposed alongside the
other render targets so `HandleResize` covers it.

**Files:** `src/QiLight.Game/Rendering/ShaderPipeline.cs`, `NeonRenderer.cs`,
`GameRenderer.cs`.

---

## 5. Constant screen-wide ambient glow (shadows visible everywhere)

**Problem.** Shadows only showed inside the player's ~240px pool; the black floor
beyond it had no light to carve.

**Approach.** `BeginLight(Color ambient)` clears the light buffer to a dim ambient
color (`AmbientColor = (26,24,36)`) instead of black, so the whole floor is faintly
lit; the radial pool still adds a brighter region near the player. Shadow volumes are
projected to the **full screen diagonal** so the player-relative shadows reach the
edges. Ambient is kept below the bloom threshold so it stays subtle.

**Files:** `src/QiLight.Game/Rendering/ShaderPipeline.cs`, `GameRenderer.cs`.

---

## 6. 8-bit pixel font + very light text glow

**Goal.** Replace system Arial with an authentic pixel font and give text a faint
glow.

**Approach.**
- Bundled the OFL **Press Start 2P** font in `Content/Fonts/` (+ `PressStart2P-OFL.txt`).
  `NeonFont.spritefont` references the TTF **by filename** (`PressStart2P-Regular.ttf`)
  — the macOS font processor couldn't resolve it by family name — at base size 10,
  Regular.
- One shared font serves both the dense HUD and big titles: titles/headings are drawn
  with a **scale** factor and `SamplerState.PointClamp` so upscaled pixels stay crisp.
  Scales: title 4×, headings 2.5×, prompts 1.5×, HUD 1×.
- `GameRenderer.DrawTextGlow` draws four low-alpha (~0.25) offset copies behind the
  crisp text; all text sites route through it. `CenteredX` recomputes centered
  positions for the scaled text.

**Files:** `src/QiLight.Game/Content/Fonts/` (font + license + spritefont),
`src/QiLight.Game/Rendering/GameRenderer.cs`.

---

## 7. Retract: hold to back out along the trail

**Goal.** Let the player safely abort a draw by backing out along its own tail.

**Approach.** A dedicated, non-conflicting key — **Backspace** (`Keys.Back`) / gamepad
**B** — set as `InputManager.RetractHeld` and passed through `QiLightGame` into
`Player.Update`. While held in Drawing mode, `Player.RetractAlongTrail` walks the
trail head backward along the polyline at `RetractSpeed = 250f`, consuming vertices
and preserving the `Trail[^1] == Position` invariant. Releasing resumes drawing from
the current head; reaching the start clears the trail and returns to Border mode with
**no capture**. Rendering is unchanged — the trail shrinks because it's drawn from
`Trail` each frame.

**Files:** `src/QiLight.Game/Input/InputManager.cs`, `QiLightGame.cs`,
`src/QiLight.Game/Core/Player.cs`.

---

## Build & run (macOS)

```bash
DYLD_FALLBACK_LIBRARY_PATH=/opt/homebrew/lib dotnet build
DYLD_FALLBACK_LIBRARY_PATH=/opt/homebrew/lib dotnet run --project src/QiLight.Game
```

Controls: move with arrows/WASD; **Space + direction** to draw into the field;
**LeftShift** speed-boost; **Backspace** to retract; **P/Esc** pause; **Enter** to
start/continue.
