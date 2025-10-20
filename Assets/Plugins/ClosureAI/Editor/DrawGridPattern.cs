#if UNITASK_INSTALLED
using UnityEngine;
using UnityEngine.UIElements;

namespace ClosureAI.Editor.UI
{
    public static class DrawGridPattern
    {
        public static Texture2D CreateGridTexture()
        {
            var dotSize = 1f;
            var dotSizeMajor = 1.9f;
            var dotMajorSpacing = 6f;
            var dotColor = Color.gray;
            var spacing = 20f;
            var sx = 6;
            var sy = 6;

            var textureWidth = Mathf.RoundToInt(sx * spacing);
            var textureHeight = Mathf.RoundToInt(sy * spacing);
            var textureDimensions = new Vector2Int(textureWidth, textureHeight);

            var texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            var pixels = new Color[textureWidth * textureHeight];

            // Fill with transparent background
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw dots
            for (var x = 0; x < sx; x++)
            {
                for (var y = 0; y < sy; y++)
                {
                    var ds = (x % dotMajorSpacing == 0 && y % dotMajorSpacing == 0) ? dotSizeMajor : dotSize;
                    var center = new Vector2(x * spacing + dotMajorSpacing, y * spacing + dotMajorSpacing);

                    DrawAntiAliasedCircle(pixels, textureDimensions, center, ds, dotColor);
                }
            }

            texture2D.SetPixels(pixels);
            texture2D.Apply();

            return texture2D;
        }

        private static void DrawAntiAliasedCircle(Color[] pixels, Vector2Int dimensions, Vector2 center, float radius, Color color)
        {
            // Expand the sampling area slightly for antialiasing
            var antiAliasBuffer = 1.5f;
            var samplingRadius = radius + antiAliasBuffer;

            var minX = Mathf.Max(0, Mathf.FloorToInt(center.x - samplingRadius));
            var maxX = Mathf.Min(dimensions.x - 1, Mathf.CeilToInt(center.x + samplingRadius));
            var minY = Mathf.Max(0, Mathf.FloorToInt(center.y - samplingRadius));
            var maxY = Mathf.Min(dimensions.y - 1, Mathf.CeilToInt(center.y + samplingRadius));

            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var coverage = CalculateCircleCoverage(x, y, center, radius);

                    if (coverage > 0f)
                    {
                        var index = y * dimensions.x + x;
                        var existingColor = pixels[index];

                        // Alpha blend the circle color with the existing pixel
                        var blendedAlpha = coverage * color.a + existingColor.a * (1f - coverage * color.a);
                        var blendedColor = new Color(
                            (coverage * color.a * color.r + existingColor.a * (1f - coverage * color.a) * existingColor.r) / Mathf.Max(blendedAlpha, 0.001f),
                            (coverage * color.a * color.g + existingColor.a * (1f - coverage * color.a) * existingColor.g) / Mathf.Max(blendedAlpha, 0.001f),
                            (coverage * color.a * color.b + existingColor.a * (1f - coverage * color.a) * existingColor.b) / Mathf.Max(blendedAlpha, 0.001f),
                            blendedAlpha
                        );

                        pixels[index] = blendedColor;
                    }
                }
            }
        }

        private static float CalculateCircleCoverage(int pixelX, int pixelY, Vector2 center, float radius)
        {
            // Use supersampling for better anti-aliasing quality
            const int samples = 4; // 4x4 supersampling
            const float sampleStep = 1f / samples;
            const float sampleOffset = sampleStep * 0.5f;

            var totalCoverage = 0f;
            var totalSamples = samples * samples;

            for (var sx = 0; sx < samples; sx++)
            {
                for (var sy = 0; sy < samples; sy++)
                {
                    var sampleX = pixelX + sampleOffset + sx * sampleStep;
                    var sampleY = pixelY + sampleOffset + sy * sampleStep;

                    var dx = sampleX - center.x;
                    var dy = sampleY - center.y;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);

                    // Use smoothstep for smooth antialiasing transition
                    var edgeDistance = distance - radius;
                    var coverage = 1f - Mathf.SmoothStep(0f, 1f, edgeDistance + 0.5f);

                    totalCoverage += Mathf.Clamp01(coverage);
                }
            }

            return totalCoverage / totalSamples;
        }
    }
}

#endif