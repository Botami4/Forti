using System;
using System.Numerics;

namespace Forti.Core.Entities
{
    public class PlayerEntity : Entity
    {
        public CharacterState CharacterState { get; } = new CharacterState();
        public float MoveSpeed { get; set; } = 8.0f;
        public float SprintMultiplier { get; set; } = 1.5f;

        public PlayerEntity()
        {
            Radius = 0.45f;
            Height = 1.8f;
        }

        public void ApplyMovementInput(Vector2 wishDir, bool isSprinting, float deltaTime)
        {
            if (!CharacterState.IsAlive)
            {
                Velocity = Vector3.Zero;
                return;
            }

            float currentSpeed = MoveSpeed * (isSprinting ? SprintMultiplier : 1.0f);

            // Compute forward and right unit vectors from Yaw (degrees)
            float yawRad = Yaw * (MathF.PI / 180.0f);
            Vector3 forward = new Vector3(MathF.Sin(yawRad), 0f, MathF.Cos(yawRad));
            Vector3 right = new Vector3(MathF.Cos(yawRad), 0f, -MathF.Sin(yawRad));

            Vector3 move = forward * wishDir.Y + right * wishDir.X;
            if (move.LengthSquared() > 0.001f)
            {
                move = Vector3.Normalize(move) * currentSpeed;
            }

            Velocity = new Vector3(move.X, Velocity.Y, move.Z);
        }
    }
}
