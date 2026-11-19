using System;
using System.Numerics;
using Forti.Core.Math;

namespace Forti.Core.Entities
{
    public abstract class Entity
    {
        public int Id { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public bool IsActive { get; set; } = true;
        public float Radius { get; set; } = 0.5f;
        public float Height { get; set; } = 1.8f;

        public virtual AABB GetBoundingBox()
        {
            Vector3 min = new Vector3(Position.X - Radius, Position.Y, Position.Z - Radius);
            Vector3 max = new Vector3(Position.X + Radius, Position.Y + Height, Position.Z + Radius);
            return new AABB(min, max);
        }

        public virtual void Update(float deltaTime)
        {
            Position += Velocity * deltaTime;
        }
    }
}
