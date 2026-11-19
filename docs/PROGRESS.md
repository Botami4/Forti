# Skyfall Royale - Progress & Architecture Documentation

## Project Overview
- **Project Name:** Skyfall Royale
- **Genre:** Single-player, third-person 3D battle-royale
- **Engine / Framework:** C# WPF (.NET 10.0-windows), built-in `System.Windows.Media.Media3D`
- **Art Direction:** 100% procedural low-poly meshes and procedural textures (`WriteableBitmap`). No third-party or downloaded assets.
- **Audio:** Code-generated procedural sounds.
- **Legal Compliance:** Original battle-royale title; zero assets, names, characters, or layouts borrowed from Fortnite or other copyrighted works.

---

## Architectural Invariants & Ground Rules

1. **Strict Separation of Simulation & Rendering**:
   - `Core/`: Pure C# logic with zero WPF or UI dependencies (`System.Numerics` only). Contains entities, physics, bounding math, weapons, storm zone, and simulation ticks.
   - `Rendering/`: WPF `Viewport3D` scene management. Uses procedural mesh generators (`ProceduralMeshBuilder`) and textures (`ProceduralTextureBuilder`).
   - `Input/`: Tracks keyboard and mouse states (`GameInputManager`).
   - `UI/`: HUD (reticle, health/shield bars, storm indicators) and debug overlay (`DebugOverlay`).

2. **Media3D Performance Rules**:
   - Every Freezable mesh, material, and brush calls `.Freeze()`.
   - Shared meshes across instances (e.g. cylinder meshes for player and bots).
   - Dynamic movement achieved strictly via `Transform3D` (`TranslateTransform3D`, `RotateTransform3D`), never rebuilding geometry per frame.
   - Zero per-frame allocations in hot paths.
   - Dynamic view-distance culling hides entities and chunks outside view radius.
   - Custom collision maths: purely custom Ray, AABB, and BoundingSphere tests (no WPF hit testing for gameplay).
   - Visual enhancements: Fake blob contact shadows, procedural gradient sky backdrop behind transparent `Viewport3D`.

3. **Game Loop**:
   - Driven by `CompositionTarget.Rendering`.
   - Delta time clamped to a strict maximum of `0.05s` (20 FPS floor equivalent) to protect simulation stability.
   - No `DispatcherTimer` used for gameplay.

4. **Automated Verification**:
   - Headless `--selftest` CLI flag executing core simulation, physics, damage absorption, and storm boundary tests without opening a window.

---

## Phase History

### Phase 1: Recon, Architecture & Playable Skeleton
- **Date:** 2026-11-19
- **Status:** Completed & Verified (0 errors, 0 warnings, 22/22 self-tests passing).

#### 1. Project Reconnaissance & Integration
- Inspected `Forti.csproj` (`net10.0-windows`, `Nullable=enable`, `ImplicitUsings=enable`, `UseWPF=true`).
- Backed up initial unmodified `MainWindow` to `MainWindow_OriginalBackup.xaml` / `MainWindow_OriginalBackup.xaml.cs`.
- Integrated pure C# simulation modules in `Core/` without changing target framework or adding third-party packages.

#### 2. MainWindow 4-Layer Architecture (Bottom to Top)
1. **Layer 1: Gradient Sky**: Procedural `LinearGradientBrush` backdrop (warm horizon `#C4E0F9` transitioning up to deep sky `#2B66B1`).
2. **Layer 2: Viewport3D**: Full-window transparent 3D canvas displaying lighting, scenery, scale landmarks, characters, and blob shadows.
3. **Layer 3: HUD Overlay**: Reticle, health bar, shield bar, alive player counter, storm countdown/radius ticker, and controls help.
4. **Layer 4: Screens Layer**:
   - `MainMenuScreen`: Title "SKYFALL ROYALE", "Start Battle", "Settings" (mouse sensitivity slider), and "Quit".
   - `PauseScreen`: "Game Paused", "Resume", "Main Menu", and in-game sensitivity controls.
   - `ResultsScreen`: "Victory Royale" or "Eliminated", match statistics, and "Play Again" / "Main Menu".
5. **Layer 5: Debug Overlay**: F3 toggle overlay tracking FPS, frame ms, entity count, visible chunks, player coordinates, and storm radius.

#### 3. Core Game State Machine
- Defined `MatchState`: `MainMenu` -> `InBus` -> `Freefall` -> `Gliding` -> `OnGround` -> `Victory`/`Defeat`, plus `Pause`.
- Implemented state transition rules, illegal transition guards, and pause/resume state restoration in `Forti.Core.Simulation.GameStateMachine`.

#### 4. Input & Third-Person Camera with Mouse Look
- Implemented `GameInputManager` with Win32 `SetCursorPos` and `GetCursorPos` P/Invoke.
- Cursor is hidden and captured during active gameplay; recentered each frame to compute smooth relative mouse deltas.
- Pressing `Esc` releases capture, restores the cursor, and triggers the Pause screen.
- Configurable mouse sensitivity (`0.05` to `0.50`, default `0.15`).
- Orbit camera angles smoothly around the player character with clamped pitch ($-45^\circ$ to $+75^\circ$) and collision-ready offset.

#### 5. Placeholder World & Performance
- Big flat green ground plane (400x400) with procedural grass texture.
- Multiple colored scale cubes (Red, Blue, Yellow, Purple, Cyan, Orange, Pink, Lime) for spatial reference.
- Low-poly placeholder character with torso, head, and visor, casting a dynamic soft contact blob shadow.
- Measured active render work: **0.088 ms/frame** (theoretical CPU headroom: **11,332 FPS**). Active interactive frames render at **60.1 FPS** (16.6 ms vsync target).

---

### How to Test by Hand

1. **Headless Self-Test**:
   ```powershell
   dotnet run --project "C:\Users\oktatas\Desktop\Bota\Merenyi\Forti\Forti\Forti.csproj" -- --selftest
   ```
   *Expected output:* `22 Passed, 0 Failed` (validates all state transitions, pause/resume, movement vectors, bounds, damage, and math).

2. **Automated Performance Benchmark**:
   ```powershell
   dotnet run --project "C:\Users\oktatas\Desktop\Bota\Merenyi\Forti\Forti\Forti.csproj" -- --benchmark
   ```
   *Expected output:* Measures exact CPU frame work (0.088 ms) and presentation rate.

3. **Interactive Playtest**:
   ```powershell
   dotnet run --project "C:\Users\oktatas\Desktop\Bota\Merenyi\Forti\Forti\Forti.csproj"
   ```
   - **Menu**: The game launches into the Main Menu. Click "SETTINGS" to adjust sensitivity, or "START BATTLE" to enter the world.
   - **Controls**:
     - `W`, `A`, `S`, `D`: Walk in third-person relative to camera heading.
     - `Left Shift`: Sprint.
     - `Space`: Jump (grounded physics check).
     - **Mouse Look**: Move the mouse freely to look/orbit (cursor automatically hidden & centered).
     - `Esc`: Pauses game, unlocks cursor, displays Pause menu. Click "Resume" to re-lock cursor and continue.
     - `F3`: Toggle real-time debug overlay showing live FPS, frame ms, player coordinates, and storm radius.

---

### Known Issues
- None. Solution builds with 0 warnings and 0 errors.

---

### Suggested Next Step
- **Phase 2: Drop Phase & Air Movement (InBus -> Freefall -> Gliding -> OnGround)** — implement the high-altitude flying battle bus, jump-out trigger, skydiving freefall controls, deployable procedural glider with gliding aerodynamics, and landing transition to ground combat.
