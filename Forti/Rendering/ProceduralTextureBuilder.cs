using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Forti.Rendering
{
    public static class ProceduralTextureBuilder
    {
        public static Material CreateSolidMaterial(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            var material = new DiffuseMaterial(brush);
            material.Freeze();
            return material;
        }

        public static Material CreateBlobShadowMaterial(int size = 64)
        {
            var wb = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = new byte[size * size * 4];
            float center = (size - 1) * 0.5f;
            float maxR = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    float t = Math.Clamp(dist / maxR, 0f, 1f);

                    // Smoothstep alpha drop-off
                    float alphaFactor = (1.0f - t * t);
                    if (alphaFactor < 0f) alphaFactor = 0f;

                    byte alpha = (byte)(alphaFactor * 160); // Soft dark blob

                    int idx = (y * size + x) * 4;
                    pixels[idx + 0] = 10;  // B
                    pixels[idx + 1] = 10;  // G
                    pixels[idx + 2] = 10;  // R
                    pixels[idx + 3] = alpha; // A
                }
            }

            wb.WritePixels(new System.Windows.Int32Rect(0, 0, size, size), pixels, size * 4, 0);
            wb.Freeze();

            var brush = new ImageBrush(wb);
            brush.Freeze();
            var material = new DiffuseMaterial(brush);
            material.Freeze();
            return material;
        }

        public static Material CreateGridMaterial(int size = 64, Color? lineCol = null, Color? fillCol = null)
        {
            Color line = lineCol ?? Color.FromRgb(45, 120, 65);
            Color fill = fillCol ?? Color.FromRgb(35, 95, 50);

            var wb = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = new byte[size * size * 4];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x == 0 || x == size - 1 || y == 0 || y == size - 1);
                    Color c = isBorder ? line : fill;

                    int idx = (y * size + x) * 4;
                    pixels[idx + 0] = c.B;
                    pixels[idx + 1] = c.G;
                    pixels[idx + 2] = c.R;
                    pixels[idx + 3] = 255;
                }
            }

            wb.WritePixels(new System.Windows.Int32Rect(0, 0, size, size), pixels, size * 4, 0);
            wb.Freeze();

            var brush = new ImageBrush(wb)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new System.Windows.Rect(0, 0, 1, 1)
            };
            brush.Freeze();

            var material = new DiffuseMaterial(brush);
            material.Freeze();
            return material;
        }
    }
}
