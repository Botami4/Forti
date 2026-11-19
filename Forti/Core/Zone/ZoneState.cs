using System;
using System.Numerics;
using Forti.Core.Math;

namespace Forti.Core.Zone
{
    public class ZoneState
    {
        public Vector2 CurrentCenter { get; set; } = Vector2.Zero;
        public float CurrentRadius { get; set; } = 300f;
        public Vector2 NextCenter { get; set; } = Vector2.Zero;
        public float NextRadius { get; set; } = 150f;

        public float PhaseWaitTimer { get; set; } = 30.0f;
        public float PhaseShrinkDuration { get; set; } = 40.0f;
        public float CurrentShrinkProgress { get; set; } = 0.0f;
        public bool IsShrinking { get; set; } = false;
        public float DamagePerSecond { get; set; } = 5.0f;
        public int CurrentPhase { get; set; } = 1;

        public bool IsInsideZone(Vector3 position)
        {
            Vector2 pos2D = new Vector2(position.X, position.Z);
            return Vector2.Distance(pos2D, CurrentCenter) <= CurrentRadius;
        }

        public void Update(float deltaTime)
        {
            if (!IsShrinking)
            {
                PhaseWaitTimer -= deltaTime;
                if (PhaseWaitTimer <= 0f)
                {
                    IsShrinking = true;
                    CurrentShrinkProgress = 0f;
                }
            }
            else
            {
                CurrentShrinkProgress += deltaTime / PhaseShrinkDuration;
                float t = MathUtils.Clamp(CurrentShrinkProgress, 0f, 1f);

                CurrentRadius = MathUtils.Lerp(CurrentRadius, NextRadius, t);
                CurrentCenter = Vector2.Lerp(CurrentCenter, NextCenter, t);

                if (CurrentShrinkProgress >= 1.0f)
                {
                    IsShrinking = false;
                    CurrentPhase++;
                    PhaseWaitTimer = 25.0f;
                    CurrentRadius = NextRadius;
                    CurrentCenter = NextCenter;
                    NextRadius = MathF.Max(NextRadius * 0.5f, 20f);
                    DamagePerSecond += 2.5f;
                }
            }
        }
    }
}
