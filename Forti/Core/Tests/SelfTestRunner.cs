using System;
using System.Numerics;
using Forti.Core.Entities;
using Forti.Core.Math;
using Forti.Core.Simulation;

namespace Forti.Core.Tests
{
    public static class SelfTestRunner
    {
        public static bool RunAll(Action<string>? log = null)
        {
            log ??= Console.WriteLine;
            int passed = 0;
            int failed = 0;

            void Assert(bool condition, string testName)
            {
                if (condition)
                {
                    passed++;
                    log($"  [PASS] {testName}");
                }
                else
                {
                    failed++;
                    log($"  [FAIL] {testName}");
                }
            }

            log("=== Running Skyfall Royale Headless Core Self-Tests (Phase 1) ===");

            // 1. Math: Ray-AABB intersection
            var box = new AABB(new Vector3(-1f, -1f, -1f), new Vector3(1f, 1f, 1f));
            var hitRay = new Ray(new Vector3(0f, 0f, -5f), new Vector3(0f, 0f, 1f));
            bool hit = box.RayIntersects(hitRay, out float hitDist);
            Assert(hit && System.Math.Abs(hitDist - 4.0f) < 0.01f, "Ray-AABB Intersection (Direct Hit)");

            var missRay = new Ray(new Vector3(0f, 5f, -5f), new Vector3(0f, 0f, 1f));
            bool miss = box.RayIntersects(missRay, out _);
            Assert(!miss, "Ray-AABB Miss Check");

            // 2. Math: BoundingSphere intersection
            var sphere = new BoundingSphere(new Vector3(0f, 0f, 0f), 2f);
            Assert(sphere.Contains(new Vector3(0.5f, 0.5f, 0.5f)), "BoundingSphere Contains Point Inside");
            Assert(!sphere.Contains(new Vector3(3f, 0f, 0f)), "BoundingSphere Excludes Point Outside");

            // 3. State Machine: Valid Progression Chain
            var sm = new GameStateMachine();
            Assert(sm.CurrentState == MatchState.MainMenu, "Initial state is MainMenu");
            Assert(sm.TryTransition(MatchState.InBus), "Transition MainMenu -> InBus");
            Assert(sm.TryTransition(MatchState.Freefall), "Transition InBus -> Freefall");
            Assert(sm.TryTransition(MatchState.Gliding), "Transition Freefall -> Gliding");
            Assert(sm.TryTransition(MatchState.OnGround), "Transition Gliding -> OnGround");
            Assert(sm.TryTransition(MatchState.Victory), "Transition OnGround -> Victory");

            // 4. State Machine: Illegal Transition Rejection
            var sm2 = new GameStateMachine();
            Assert(!sm2.TryTransition(MatchState.Victory), "Reject illegal direct transition MainMenu -> Victory");
            Assert(!sm2.TryTransition(MatchState.Gliding), "Reject illegal direct transition MainMenu -> Gliding");

            // 5. State Machine: Pause & Resume Behavior
            sm.ReturnToMainMenu();
            sm.StartMatch(skipAirDrop: true);
            Assert(sm.CurrentState == MatchState.OnGround, "StartMatch transitions to OnGround");

            sm.SetPaused(true);
            Assert(sm.IsPaused && sm.StateBeforePause == MatchState.OnGround, "Pause preserves state before pause");

            sm.SetPaused(false);
            Assert(!sm.IsPaused && sm.CurrentState == MatchState.OnGround, "Resume restores active match state");

            // 6. Core: Delta time clamping rule (Max 0.05s)
            var sim = new SimulationWorld();
            sim.ResetWorld();
            sim.StateMachine.StartMatch(skipAirDrop: true);

            float initialTime = sim.MatchTimer;
            sim.Step(0.25f); // Provide huge delta time (250ms)
            float advanced = sim.MatchTimer - initialTime;
            Assert(System.Math.Abs(advanced - SimulationWorld.MaxDeltaTime) < 0.001f, 
                $"Game loop delta time clamp enforced (stepped {advanced:F4}s <= {SimulationWorld.MaxDeltaTime}s)");

            // 7. Player WASD Movement & Directional Vectors
            sim.ResetWorld();
            sim.StateMachine.StartMatch(skipAirDrop: true);
            sim.Player.Yaw = 0f; // Facing +Z
            sim.Player.ApplyMovementInput(new Vector2(0f, 1f), false, 0.05f); // Forward
            sim.Step(0.05f);
            Assert(sim.Player.Position.Z > 0f && sim.Player.Position.X == 0f, "Player moves forward (+Z) with 'W' input");

            // 8. Entities: Character Shield & Health Damage Absorption
            var state = new CharacterState { Health = 100f, Shield = 50f };
            state.TakeDamage(30f);
            Assert(state.Shield == 20f && state.Health == 100f, "Shield absorbs damage before health");

            state.TakeDamage(40f);
            Assert(state.Shield == 0f && state.Health == 80f, "Excess damage depletes shield and reduces health");

            // 9. Zone: Inside / Outside boundary & Damage
            sim.ResetWorld();
            sim.StateMachine.StartMatch(skipAirDrop: true);
            sim.Player.Position = new Vector3(0f, 0f, 0f);
            sim.Player.CharacterState.Shield = 0f;
            float hpBefore = sim.Player.CharacterState.Health;
            sim.Step(0.05f);
            Assert(sim.Player.CharacterState.Health == hpBefore, "Player takes 0 zone damage inside safe circle");

            sim.Player.Position = new Vector3(500f, 0f, 500f); // Far outside zone
            sim.Step(0.05f);
            Assert(sim.Player.CharacterState.Health < hpBefore, "Player takes damage per second outside storm circle");

            // 10. Player Physics: Ground collision clamp
            sim.ResetWorld();
            sim.StateMachine.StartMatch(skipAirDrop: true);
            sim.Player.Position = new Vector3(0f, 10f, 0f);
            sim.Player.CharacterState.IsGrounded = false;
            sim.Player.Velocity = new Vector3(0f, -60f, 0f);
            sim.Step(0.05f);
            sim.Step(0.05f);
            sim.Step(0.05f);
            Assert(sim.Player.Position.Y >= 0f, "Player position clamped above ground plane (Y >= 0)");

            log($"=== Self-Test Summary: {passed} Passed, {failed} Failed ===");
            return failed == 0;
        }
    }
}
