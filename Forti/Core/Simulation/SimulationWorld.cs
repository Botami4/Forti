using System;
using System.Collections.Generic;
using System.Numerics;
using Forti.Core.Entities;
using Forti.Core.Math;
using Forti.Core.Zone;

namespace Forti.Core.Simulation
{
    public class SimulationWorld
    {
        public const float MaxDeltaTime = 0.05f;
        public const float Gravity = -19.6f;

        public GameStateMachine StateMachine { get; } = new GameStateMachine();
        public PlayerEntity Player { get; } = new PlayerEntity();
        public List<BotEntity> Bots { get; } = new List<BotEntity>();
        public ZoneState Zone { get; } = new ZoneState();

        public float MatchTimer { get; private set; } = 0f;
        public int TotalParticipants => 1 + Bots.Count;
        public int AliveParticipants
        {
            get
            {
                int count = Player.CharacterState.IsAlive ? 1 : 0;
                for (int i = 0; i < Bots.Count; i++)
                {
                    if (Bots[i].CharacterState.IsAlive) count++;
                }
                return count;
            }
        }

        public SimulationWorld()
        {
            ResetWorld();
        }

        public void ResetWorld()
        {
            MatchTimer = 0f;

            // Setup player
            Player.Position = new Vector3(0f, 0f, 0f);
            Player.Velocity = Vector3.Zero;
            Player.Yaw = 0f;
            Player.Pitch = 0f;
            Player.CharacterState.Health = 100f;
            Player.CharacterState.Shield = 50f;
            Player.CharacterState.IsGrounded = true;
            Player.CharacterState.IsSkydiving = false;

            // Setup bots
            Bots.Clear();
            var rand = new Random(42);
            for (int i = 0; i < 9; i++)
            {
                var bot = new BotEntity(i + 1);
                float angle = (float)(i * (2 * MathF.PI / 9));
                float dist = 25f + (float)rand.NextDouble() * 30f;
                bot.Position = new Vector3(MathF.Cos(angle) * dist, 0f, MathF.Sin(angle) * dist);
                bot.CharacterState.Health = 100f;
                bot.CharacterState.Shield = 25f;
                Bots.Add(bot);
            }

            // Setup Zone
            Zone.CurrentCenter = Vector2.Zero;
            Zone.CurrentRadius = 180f;
            Zone.NextCenter = new Vector2(10f, -10f);
            Zone.NextRadius = 90f;
            Zone.PhaseWaitTimer = 30.0f;
            Zone.PhaseShrinkDuration = 35.0f;
            Zone.IsShrinking = false;
            Zone.DamagePerSecond = 4.0f;
            Zone.CurrentPhase = 1;
        }

        public void Step(float rawDeltaTime)
        {
            // Do not advance simulation when paused or in Main Menu
            if (StateMachine.IsPaused || StateMachine.CurrentState == MatchState.MainMenu)
            {
                return;
            }

            // Enforce clamped delta time: max 0.05s
            float dt = MathF.Min(rawDeltaTime, MaxDeltaTime);
            if (dt <= 0f) return;

            MatchTimer += dt;

            // Update Zone
            Zone.Update(dt);

            // Update Player physics & state
            UpdatePlayerPhysics(dt);

            // Apply Zone damage to player
            if (!Zone.IsInsideZone(Player.Position))
            {
                Player.CharacterState.TakeDamage(Zone.DamagePerSecond * dt);
            }

            // Update Bots
            for (int i = 0; i < Bots.Count; i++)
            {
                var bot = Bots[i];
                if (!bot.CharacterState.IsAlive) continue;

                bot.Update(dt);

                if (!Zone.IsInsideZone(bot.Position))
                {
                    bot.CharacterState.TakeDamage(Zone.DamagePerSecond * dt);
                }
            }

            // Check match outcome
            if (!Player.CharacterState.IsAlive && StateMachine.CurrentState == MatchState.OnGround)
            {
                StateMachine.TriggerDefeat();
            }
            else if (AliveParticipants <= 1 && Player.CharacterState.IsAlive && StateMachine.CurrentState == MatchState.OnGround)
            {
                StateMachine.TriggerVictory();
            }
        }

        private void UpdatePlayerPhysics(float dt)
        {
            Vector3 pos = Player.Position;
            Vector3 vel = Player.Velocity;

            // Apply gravity if airborne
            if (!Player.CharacterState.IsGrounded)
            {
                vel.Y += Gravity * dt;
            }

            pos += vel * dt;

            // Ground plane at Y = 0
            if (pos.Y <= 0f)
            {
                pos.Y = 0f;
                vel.Y = 0f;
                Player.CharacterState.IsGrounded = true;
            }
            else
            {
                Player.CharacterState.IsGrounded = false;
            }

            Player.Position = pos;
            Player.Velocity = vel;
        }
    }
}
