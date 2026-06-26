# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

QiLight is a Qix-like arcade game rendered as neon glowing vector lines with a bloom post-process. MonoGame DesktopGL, .NET 9, macOS desktop target (Milestone 1).

## Build & Run (macOS)

MonoGame DesktopGL needs Homebrew's freetype at runtime, so the `DYLD_FALLBACK_LIBRARY_PATH` env var is **required** for every build/run:

```bash
# one-time setup
brew install freetype

# build
DYLD_FALLBACK_LIBRARY_PATH=/opt/homebrew/lib dotnet build

# run
DYLD_FALLBACK_LIBRARY_PATH=/opt/homebrew/lib dotnet run --project src/QiLight.Game
```

In VS Code, the `build`/`run` tasks (`.vscode/tasks.json`) and the **Debug QiLight** launch config (`.vscode/launch.json`) already inject this env var — just press F5.

There is no test suite. Verify changes by running the game.

## Architecture

`Program.cs` launches `QiLightGame` (extends MonoGame `Game`). Everything is orchestrated from **`QiLightGame.cs`** — the per-frame `Update()`/`Draw()` and the state machine live here; the `Core/` and `Entities/` classes are plain logic objects it drives.

### Per-frame Update order (the `Playing` phase)

`InputManager` → `Player.Update()` (move along border OR extend drawing trail) → `Territory.SetQixPosition()` → `Qix.Update()` → `PlayField.UpdateBorder()` → `Sparx.Update()` → `CollisionSystem` checks → 75% win check. This order matters: Qix position is read by Territory, and Territory border changes feed PlayField/Sparx.

### State machine

`GameState.GamePhase`: `Menu → Playing → (Paused | Win | GameOver)`. Transitions are input-driven (Enter to start/advance, P/Esc to pause) or event-driven (≥75% captured → `Win`; lives ≤ 0 → `GameOver`). Each phase has its own update branch and renderer branch in `QiLightGame.cs`.

### Core gameplay coupling (the non-obvious part)

The playfield border is **not static** — it morphs on every capture, so subsystems are tightly interdependent:

- **`Player`** has two modes: *border* (parametric travel via `PlayField.MoveAlongBorder`) and *drawing* (leaves a `List<Vector2>` trail while Action is held). A trail that returns to the border completes a cut.
- **`Territory.CompleteCut(trail, player, drawDuration)`** splits the polygon into two sub-polygons (`BuildSubPolygon`, run both windings). **Whichever sub-polygon contains the Qix stays uncaptured**; the other is captured. If ambiguous, the larger one stays uncaptured. Captured regions are triangulated via `MathUtils.EarClipTriangulate` for fill rendering. Faster draws (<3s) score 2×.
- After a cut, `Territory` calls `PlayField.UpdateBorder()` so the perimeter, Sparx paths, and future draws all use the new shape.
- **`CollisionSystem`** is stateless; kills the player on trail self-intersection, Qix-vs-trail, Qix-vs-player, or Sparx-vs-player. **Post-respawn invincibility is enforced by the caller** (`if (!_player.IsInvincible)` in `QiLightGame`), not inside `CollisionSystem`.
- **`Qix`** is a multi-point shape bouncing inside `Territory.UncapturedPolygon`. **`Sparx`** patrol the perimeter parametrically. Both scale speed via `LevelManager` multipliers, read only at level init — no mid-level hot reload.
- `MathUtils` holds all the geometry: `SegmentsIntersect`, `PointInPolygon`, `DistanceToSegment`, `EarClipTriangulate`. Reuse these rather than reimplementing.

### Rendering

`GameRenderer` is pure presentation (passed full game state, no game logic). `NeonRenderer` draws lines as triangle-strip quads for thickness. `ShaderPipeline` applies bloom: it tries to load the compiled `Shaders/Bloom` effect and **silently falls back to additive-blend bloom if absent** (`_useShaderBloom`). On macOS the HLSL shader often isn't compiled (MGFXC needs wine), so the fallback path is the norm. `ColorTheme` (`Config/`) provides four palettes: Synthwave, Toxic, Ember, Ice.

## Gotchas

- DesktopGL has no `PrimitiveType.TriangleFan` — use `TriangleList`.
- Polygon-build loops in `Territory`/`Sparx` use `safety++` counters that exit silently; a triggered counter shows up as a skipped polygon or movement glitch, not an error.
- `BuildSubPolygon` winding (CW vs CCW) must be correct or sub-polygons self-intersect.
