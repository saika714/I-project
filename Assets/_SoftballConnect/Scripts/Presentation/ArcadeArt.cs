using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoftballConnect.Presentation
{
    // Owns generated visual resources. No imported models or physics are required.
    public sealed class ArcadeArt : MonoBehaviour
    {
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        Material glow;
        Material surfaceTemplate, trailTemplate;
        public static readonly Color Cream = new Color(1, 0.97f, 0.86f);
        public static readonly Color Ink = new Color(0.07f, 0.15f, 0.25f);
        public static readonly Color Skin = new Color(1, 0.79f, 0.59f);

        public void Initialize(SoftballConnect.Content.VisualThemeConfig theme)
        { surfaceTemplate = theme.SurfaceMaterial; trailTemplate = theme.TrailMaterial; }

        public Material Paint(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            material = surfaceTemplate != null ? new Material(surfaceTemplate) : new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            material.name = "Arcade " + ColorUtility.ToHtmlStringRGB(color);
            material.SetColor("_BaseColor", color);
            material.SetColor("_SpecColor", new Color(0.1f, 0.1f, 0.1f));
            material.SetFloat("_Smoothness", 0.18f);
            materials.Add(color, material);
            return material;
        }

        public Material Glow
        {
            get
            {
                if (glow == null)
                {
                    glow = trailTemplate != null ? new Material(trailTemplate) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                    glow.name = "Arcade Trail";
                    glow.SetColor("_BaseColor", Color.white);
                    glow.SetFloat("_Surface", 1);
                    glow.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    glow.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    glow.SetFloat("_ZWrite", 0);
                    glow.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    glow.renderQueue = (int)RenderQueue.Transparent;
                }
                return glow;
            }
        }

        public Transform Group(string name, Transform parent, Vector3 position)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false); group.localPosition = position;
            return group;
        }

        public Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 size, Color color)
        {
            return StadiumView.Primitive(name, type, parent, position, size, Paint(color)).transform;
        }

        public Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Color color)
            => Shape(name, PrimitiveType.Cube, parent, position, size, color);
        public Transform Orb(string name, Transform parent, Vector3 position, Vector3 size, Color color)
            => Shape(name, PrimitiveType.Sphere, parent, position, size, color);
        public Transform Disc(string name, Transform parent, Vector3 position, float radius, float height, Color color)
            => Shape(name, PrimitiveType.Cylinder, parent, position, new Vector3(radius * 2, height * 0.5f, radius * 2), color);

        public Transform Segment(string name, Transform parent, Vector3 from, Vector3 to, float radius, Color color)
        {
            var part = Shape(name, PrimitiveType.Cylinder, parent, (from + to) * 0.5f,
                new Vector3(radius * 2, (to - from).magnitude * 0.5f, radius * 2), color);
            part.up = parent.TransformDirection((to - from).normalized);
            return part;
        }

        public Transform Surface(string name, Transform parent, Vector3[] vertices, int[] triangles, Color color, Material material = null)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(vertices[i].x, vertices[i].z) * 0.35f;
            mesh.uv = uv;
            meshes.Add(mesh);
            var surface = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(parent, false);
            surface.GetComponent<MeshFilter>().sharedMesh = mesh;
            surface.GetComponent<MeshRenderer>().sharedMaterial = material != null ? material : Paint(color);
            return surface.transform;
        }

        public void Arc(string name, Transform parent, float inner, float outer, float start, float end, float height, Color color, Material material = null)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt((end - start) / 3));
            var vertices = new Vector3[(count + 1) * 2]; var triangles = new int[count * 6];
            for (int i = 0; i <= count; i++)
            {
                float angle = Mathf.Lerp(start, end, i / (float)count) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                vertices[i * 2] = direction * inner + Vector3.up * height;
                vertices[i * 2 + 1] = direction * outer + Vector3.up * height;
                if (i == count) continue;
                int t = i * 6, v = i * 2;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }
            Surface(name, parent, vertices, triangles, color, material);
        }

        public void Lettering(string name, Transform parent, Vector3 position, Quaternion rotation, string value, float size, Color color)
        {
            var label = new GameObject(name, typeof(TextMesh)); label.transform.SetParent(parent, false);
            label.transform.localPosition = position; label.transform.localRotation = rotation;
            var text = label.GetComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            text.text = value; text.fontSize = 64; text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = color;
        }

        public void CombineStatic(Transform parent)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in parent.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.enabled) continue;
                if (!groups.TryGetValue(renderer.sharedMaterial, out var instances))
                    groups.Add(renderer.sharedMaterial, instances = new List<CombineInstance>());
                instances.Add(new CombineInstance { mesh = filter.sharedMesh,
                    transform = parent.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                renderer.enabled = false;
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "Combined stadium", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray()); meshes.Add(mesh);
                var batch = new GameObject("StadiumBatch", typeof(MeshFilter), typeof(MeshRenderer));
                batch.transform.SetParent(parent, false);
                batch.GetComponent<MeshFilter>().sharedMesh = mesh;
                batch.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
        }

        void OnDestroy()
        {
            foreach (var material in materials.Values) Destroy(material);
            foreach (var mesh in meshes) Destroy(mesh);
            if (glow != null) Destroy(glow);
        }
    }
}
