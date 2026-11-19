using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Forti.Core.Entities;
using Forti.Core.Simulation;

namespace Forti.Rendering
{
    public class SceneRenderer
    {
        private readonly Viewport3D _viewport;
        private readonly PerspectiveCamera _camera;

        // Model visual references
        private readonly ModelVisual3D _playerVisual;
        private readonly TranslateTransform3D _playerTranslate;
        private readonly RotateTransform3D _playerRotate;
        private readonly AxisAngleRotation3D _playerYawRotation;

        // Player blob shadow
        private readonly ModelVisual3D _playerShadowVisual;
        private readonly TranslateTransform3D _playerShadowTranslate;
        private readonly ScaleTransform3D _playerShadowScale;

        // Scenery & terrain
        private readonly ModelVisual3D _staticSceneryVisual;

        // Bot visual pool
        private readonly List<BotVisualEntry> _botVisuals = new List<BotVisualEntry>();

        // Metrics for debug overlay
        public int VisibleEntityCount { get; private set; }
        public int VisibleChunkCount { get; private set; } = 1;
        public float ViewDistance { get; set; } = 250.0f;

        private class BotVisualEntry
        {
            public BotEntity Entity { get; }
            public ModelVisual3D Visual { get; }
            public TranslateTransform3D Translate { get; }

            public BotVisualEntry(BotEntity entity, ModelVisual3D visual, TranslateTransform3D translate)
            {
                Entity = entity;
                Visual = visual;
                Translate = translate;
            }
        }

        public SceneRenderer(Viewport3D viewport)
        {
            _viewport = viewport;
            _viewport.Children.Clear();

            // 1. Camera Setup
            _camera = new PerspectiveCamera
            {
                FieldOfView = 65,
                NearPlaneDistance = 0.2,
                FarPlaneDistance = 600.0,
                UpDirection = new Vector3D(0, 1, 0)
            };
            _viewport.Camera = _camera;

            // 2. Lighting Setup
            var lightGroup = new Model3DGroup();
            var sunLight = new DirectionalLight(Color.FromRgb(255, 250, 235), new Vector3D(-0.4, -0.85, -0.35));
            var fillLight = new DirectionalLight(Color.FromRgb(100, 130, 160), new Vector3D(0.4, 0.4, 0.35));
            var ambientLight = new AmbientLight(Color.FromRgb(85, 95, 110));
            lightGroup.Children.Add(sunLight);
            lightGroup.Children.Add(fillLight);
            lightGroup.Children.Add(ambientLight);
            _viewport.Children.Add(new ModelVisual3D { Content = lightGroup });

            // 3. Static Scenery: Big Flat Green Plane + Scale Cubes
            _staticSceneryVisual = new ModelVisual3D();
            var sceneryGroup = new Model3DGroup();

            // Ground Plane (400x400)
            var groundMesh = ProceduralMeshBuilder.CreatePlane(400, 400, 40, 40);
            var groundMaterial = ProceduralTextureBuilder.CreateGridMaterial(64, Color.FromRgb(55, 140, 75), Color.FromRgb(40, 110, 55));
            sceneryGroup.Children.Add(new GeometryModel3D(groundMesh, groundMaterial));

            // Colored Cubes for scale
            AddScaleCube(sceneryGroup, 3f, 3f, 3f, new Point3D(12, 1.5, 12), Color.FromRgb(225, 55, 50));     // Red
            AddScaleCube(sceneryGroup, 4f, 4f, 4f, new Point3D(-18, 2.0, 14), Color.FromRgb(45, 125, 235));   // Blue
            AddScaleCube(sceneryGroup, 5f, 5f, 5f, new Point3D(22, 2.5, -22), Color.FromRgb(250, 190, 30));   // Yellow
            AddScaleCube(sceneryGroup, 6f, 6f, 6f, new Point3D(-24, 3.0, -20), Color.FromRgb(155, 60, 215));  // Purple
            AddScaleCube(sceneryGroup, 2f, 10f, 2f, new Point3D(0, 5.0, 35), Color.FromRgb(40, 215, 185));    // Cyan Pillar
            AddScaleCube(sceneryGroup, 8f, 2f, 8f, new Point3D(-8, 1.0, -35), Color.FromRgb(245, 125, 45));   // Orange Platform
            AddScaleCube(sceneryGroup, 2.5f, 2.5f, 2.5f, new Point3D(30, 1.25, 0), Color.FromRgb(235, 75, 150)); // Pink
            AddScaleCube(sceneryGroup, 4f, 4f, 4f, new Point3D(-32, 2.0, 0), Color.FromRgb(70, 200, 70));     // Lime

            _staticSceneryVisual.Content = sceneryGroup;
            _viewport.Children.Add(_staticSceneryVisual);

            // 4. Placeholder Character (Low-poly body + head)
            var characterGroup = new Model3DGroup();

            // Torso (Cylinder)
            var bodyMesh = ProceduralMeshBuilder.CreateCylinder(0.42f, 1.2f, 12);
            var bodyMaterial = ProceduralTextureBuilder.CreateSolidMaterial(Color.FromRgb(30, 125, 240));
            var bodyModel = new GeometryModel3D(bodyMesh, bodyMaterial)
            {
                Transform = new TranslateTransform3D(0, 0.6, 0)
            };
            characterGroup.Children.Add(bodyModel);

            // Head (Box)
            var headMesh = ProceduralMeshBuilder.CreateBox(0.45f, 0.45f, 0.45f);
            var headMaterial = ProceduralTextureBuilder.CreateSolidMaterial(Color.FromRgb(245, 245, 250));
            var headModel = new GeometryModel3D(headMesh, headMaterial)
            {
                Transform = new TranslateTransform3D(0, 1.45, 0)
            };
            characterGroup.Children.Add(headModel);

            // Visor (accent box)
            var visorMesh = ProceduralMeshBuilder.CreateBox(0.35f, 0.15f, 0.15f);
            var visorMaterial = ProceduralTextureBuilder.CreateSolidMaterial(Color.FromRgb(255, 215, 0));
            var visorModel = new GeometryModel3D(visorMesh, visorMaterial)
            {
                Transform = new TranslateTransform3D(0, 1.45, 0.22) // Facing +Z
            };
            characterGroup.Children.Add(visorModel);

            // Player Transform Setup (Transforms only, no per-frame mesh rebuilds)
            _playerTranslate = new TranslateTransform3D(0, 0, 0);
            _playerYawRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            _playerRotate = new RotateTransform3D(_playerYawRotation);

            var playerTransformGroup = new Transform3DGroup();
            playerTransformGroup.Children.Add(_playerRotate);
            playerTransformGroup.Children.Add(_playerTranslate);

            _playerVisual = new ModelVisual3D
            {
                Content = characterGroup,
                Transform = playerTransformGroup
            };
            _viewport.Children.Add(_playerVisual);

            // 5. Contact Blob Shadow
            var shadowMesh = ProceduralMeshBuilder.CreatePlane(1.2f, 1.2f);
            var shadowMaterial = ProceduralTextureBuilder.CreateBlobShadowMaterial();
            var shadowModel = new GeometryModel3D(shadowMesh, shadowMaterial);

            _playerShadowTranslate = new TranslateTransform3D(0, 0.02, 0);
            _playerShadowScale = new ScaleTransform3D(1, 1, 1);
            var shadowTransformGroup = new Transform3DGroup();
            shadowTransformGroup.Children.Add(_playerShadowScale);
            shadowTransformGroup.Children.Add(_playerShadowTranslate);

            _playerShadowVisual = new ModelVisual3D
            {
                Content = shadowModel,
                Transform = shadowTransformGroup
            };
            _viewport.Children.Add(_playerShadowVisual);
        }

        private static void AddScaleCube(Model3DGroup group, float w, float h, float d, Point3D pos, Color color)
        {
            var mesh = ProceduralMeshBuilder.CreateBox(w, h, d);
            var material = ProceduralTextureBuilder.CreateSolidMaterial(color);
            var model = new GeometryModel3D(mesh, material)
            {
                Transform = new TranslateTransform3D(pos.X, pos.Y, pos.Z)
            };
            group.Children.Add(model);
        }

        public void BindBots(IEnumerable<BotEntity> bots)
        {
            var botMesh = ProceduralMeshBuilder.CreateCylinder(0.45f, 1.8f, 8);
            var botMaterial = ProceduralTextureBuilder.CreateSolidMaterial(Color.FromRgb(220, 65, 55));
            var sharedBotModel = new GeometryModel3D(botMesh, botMaterial);

            foreach (var bot in bots)
            {
                var translate = new TranslateTransform3D(bot.Position.X, bot.Position.Y + 0.9, bot.Position.Z);
                var visual = new ModelVisual3D
                {
                    Content = sharedBotModel,
                    Transform = translate
                };
                _viewport.Children.Add(visual);
                _botVisuals.Add(new BotVisualEntry(bot, visual, translate));
            }
        }

        public void Render(SimulationWorld sim)
        {
            int visibleCount = 0;

            // 1. Update Player Transform
            var playerPos = sim.Player.Position;
            _playerTranslate.OffsetX = playerPos.X;
            _playerTranslate.OffsetY = playerPos.Y;
            _playerTranslate.OffsetZ = playerPos.Z;
            _playerYawRotation.Angle = -sim.Player.Yaw; // Match rotation direction
            visibleCount++;

            // 2. Update Blob Shadow
            _playerShadowTranslate.OffsetX = playerPos.X;
            _playerShadowTranslate.OffsetY = 0.02; // Float slightly above ground
            _playerShadowTranslate.OffsetZ = playerPos.Z;

            float heightAboveGround = MathF.Max(0f, playerPos.Y);
            float shadowScale = Math.Clamp(1.0f - (heightAboveGround / 15.0f), 0.15f, 1.2f);
            _playerShadowScale.ScaleX = shadowScale;
            _playerShadowScale.ScaleZ = shadowScale;

            // 3. Update Bots with View Distance Culling
            for (int i = 0; i < _botVisuals.Count; i++)
            {
                var entry = _botVisuals[i];
                if (!entry.Entity.CharacterState.IsAlive)
                {
                    if (_viewport.Children.Contains(entry.Visual))
                    {
                        _viewport.Children.Remove(entry.Visual);
                    }
                    continue;
                }

                float distSq = System.Numerics.Vector3.DistanceSquared(entry.Entity.Position, playerPos);
                if (distSq > ViewDistance * ViewDistance)
                {
                    if (_viewport.Children.Contains(entry.Visual))
                    {
                        _viewport.Children.Remove(entry.Visual);
                    }
                }
                else
                {
                    if (!_viewport.Children.Contains(entry.Visual))
                    {
                        _viewport.Children.Add(entry.Visual);
                    }
                    entry.Translate.OffsetX = entry.Entity.Position.X;
                    entry.Translate.OffsetY = entry.Entity.Position.Y + 0.9;
                    entry.Translate.OffsetZ = entry.Entity.Position.Z;
                    visibleCount++;
                }
            }

            // Visible entities = player + active bots + 8 scale landmarks
            VisibleEntityCount = visibleCount + 8;
            VisibleChunkCount = 1;

            // 4. Update Third-Person Orbit Camera
            UpdateCamera(sim.Player);
        }

        private void UpdateCamera(PlayerEntity player)
        {
            float yawRad = player.Yaw * (MathF.PI / 180.0f);
            float pitchRad = player.Pitch * (MathF.PI / 180.0f);

            float cameraDistance = 5.5f;
            float cameraHeight = 1.9f;

            float cosPitch = MathF.Cos(pitchRad);
            float sinPitch = MathF.Sin(pitchRad);
            float sinYaw = MathF.Sin(yawRad);
            float cosYaw = MathF.Cos(yawRad);

            // Back off camera behind player's look heading
            float camX = player.Position.X - sinYaw * cameraDistance * cosPitch;
            float camY = player.Position.Y + cameraHeight + sinPitch * cameraDistance;
            float camZ = player.Position.Z - cosYaw * cameraDistance * cosPitch;

            Point3D cameraPos = new Point3D(camX, camY, camZ);
            Point3D lookTarget = new Point3D(player.Position.X, player.Position.Y + 1.2, player.Position.Z);
            Vector3D lookDir = lookTarget - cameraPos;

            _camera.Position = cameraPos;
            _camera.LookDirection = lookDir;
        }
    }
}
