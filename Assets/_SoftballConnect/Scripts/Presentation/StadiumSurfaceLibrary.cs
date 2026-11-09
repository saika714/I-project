using System.Collections.Generic;
using UnityEngine;

namespace SoftballConnect.Presentation
{
    // Small deterministic surface textures created locally. No external image/model dependencies.
    public sealed class StadiumSurfaceLibrary : MonoBehaviour
    {
        readonly List<Material> materials = new List<Material>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        public Material Grass { get; private set; }
        public Material GrassStripe { get; private set; }
        public Material Dirt { get; private set; }
        public void Initialize(Content.VisualThemeConfig theme)
        {
            var grass = Texture("Grass fibres", true);
            Grass = Surface(theme.SurfaceMaterial, "Stadium grass", theme.Turf, grass);
            GrassStripe = Surface(theme.SurfaceMaterial, "Stadium grass stripe", theme.TurfStripe, grass);
            Dirt = Surface(theme.SurfaceMaterial, "Infield clay", theme.Clay, Texture("Clay grain", false));
        }
        Material Surface(Material template, string name, Color color, Texture2D texture)
        {
            var material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            material.name = name; material.SetColor("_BaseColor", color); material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Smoothness", 0.04f); material.SetColor("_SpecColor", new Color(0.035f, 0.035f, 0.035f));
            materials.Add(material); return material;
        }
        Texture2D Texture(string name, bool grass)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true) { name = name,
                wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float coarse = Mathf.PerlinNoise(x * 0.045f, y * 0.045f);
                    float fine = Mathf.PerlinNoise(x * (grass ? 0.70f : 0.47f) + 19, y * (grass ? 0.11f : 0.47f) + 37);
                    float grain = ((x * 73856093L ^ y * 19349663L) & 255) / 255f;
                    float value = 0.65f + coarse * 0.18f + fine * 0.24f + grain * 0.10f;
                    pixels[y * size + x] = new Color(value, value, value);
                }
            texture.SetPixels(pixels); texture.Apply(true, true); textures.Add(texture); return texture;
        }
        void OnDestroy()
        {
            foreach (var material in materials) Destroy(material);
            foreach (var texture in textures) Destroy(texture);
        }
    }
}
