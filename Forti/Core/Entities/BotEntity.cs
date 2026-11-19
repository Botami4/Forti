using System;
using System.Numerics;

namespace Forti.Core.Entities
{
    public enum BotBehaviorState
    {
        Idle,
        Wander,
        MoveToSafeZone,
        EngagePlayer
    }

    public class BotEntity : Entity
    {
        public CharacterState CharacterState { get; } = new CharacterState();
        public BotBehaviorState State { get; set; } = BotBehaviorState.Idle;
        public float MoveSpeed { get; set; } = 6.0f;
        public Vector3 TargetPosition { get; set; }
        public float StateTimer { get; set; } = 0f;

        public BotEntity(int id)
        {
            Id = id;
            Radius = 0.45f;
            Height = 1.8f;
        }

        public override void Update(float deltaTime)
        {
            StateTimer += deltaTime;
            base.Update(deltaTime);
        }
    }
}
