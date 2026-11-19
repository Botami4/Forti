using System;
using Forti.Core.Math;

namespace Forti.Core.Entities
{
    public class CharacterState
    {
        public float Health { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public float Shield { get; set; } = 0f;
        public float MaxShield { get; set; } = 100f;

        public bool IsAlive => Health > 0f;
        public bool IsGrounded { get; set; } = true;
        public bool IsSkydiving { get; set; } = false;

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive) return;

            if (Shield > 0f)
            {
                if (Shield >= amount)
                {
                    Shield -= amount;
                    return;
                }
                else
                {
                    amount -= Shield;
                    Shield = 0f;
                }
            }

            Health = MathUtils.Clamp(Health - amount, 0f, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Health = MathUtils.Clamp(Health + amount, 0f, MaxHealth);
        }

        public void AddShield(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Shield = MathUtils.Clamp(Shield + amount, 0f, MaxShield);
        }
    }
}
