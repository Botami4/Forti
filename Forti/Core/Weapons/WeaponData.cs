using System;

namespace Forti.Core.Weapons
{
    public enum WeaponType
    {
        PulseCarbine,
        ScatterBlaster,
        MarksmanRifle
    }

    public class WeaponData
    {
        public WeaponType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Damage { get; set; } = 25f;
        public float FireRate { get; set; } = 0.15f; // Seconds per shot
        public int MagazineSize { get; set; } = 30;
        public int CurrentAmmo { get; set; } = 30;
        public float ReloadTime { get; set; } = 1.8f;
        public float ProjectileSpeed { get; set; } = 120.0f;
        public float Range { get; set; } = 150.0f;

        public static WeaponData Create(WeaponType type)
        {
            return type switch
            {
                WeaponType.PulseCarbine => new WeaponData
                {
                    Type = type,
                    Name = "Vortex Pulse Carbine",
                    Damage = 22f,
                    FireRate = 0.12f,
                    MagazineSize = 30,
                    CurrentAmmo = 30,
                    ReloadTime = 1.6f,
                    ProjectileSpeed = 140f,
                    Range = 120f
                },
                WeaponType.ScatterBlaster => new WeaponData
                {
                    Type = type,
                    Name = "Kinetic Scatter Blaster",
                    Damage = 75f,
                    FireRate = 0.75f,
                    MagazineSize = 8,
                    CurrentAmmo = 8,
                    ReloadTime = 2.2f,
                    ProjectileSpeed = 90f,
                    Range = 40f
                },
                WeaponType.MarksmanRifle => new WeaponData
                {
                    Type = type,
                    Name = "Apex Marksman Rifle",
                    Damage = 85f,
                    FireRate = 1.1f,
                    MagazineSize = 5,
                    CurrentAmmo = 5,
                    ReloadTime = 2.5f,
                    ProjectileSpeed = 220f,
                    Range = 250f
                },
                _ => new WeaponData { Name = "Standard Blaster" }
            };
        }
    }
}
