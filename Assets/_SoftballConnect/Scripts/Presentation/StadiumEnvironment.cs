using System.Collections.Generic;
using SoftballConnect.Content;
using SoftballConnect.Domain;
using UnityEngine;

namespace SoftballConnect.Presentation
{
    // Metre-scale visual construction. Playing-field collision still belongs to Domain.
    public sealed class StadiumEnvironment : MonoBehaviour
    {
        ArcadeArt art;
        VisualThemeConfig theme;
        SimulationSettings settings;
        StadiumSurfaceLibrary surfaces;
        readonly Color steel = new Color(0.32f, 0.35f, 0.37f);
        readonly Color chalk = new Color(0.91f, 0.90f, 0.85f);

        public void Build(Transform parent, ArcadeArt art, VisualThemeConfig theme, SimulationSettings settings)
        {
            this.art = art; this.theme = theme; this.settings = settings;
            if (theme.StadiumVisualPrefab != null)
            {
                var custom = Instantiate(theme.StadiumVisualPrefab, parent); custom.name = "AuthoredStadium";
                foreach (var collider in custom.GetComponentsInChildren<Collider>()) collider.enabled = false;
                return;
            }
            surfaces = gameObject.AddComponent<StadiumSurfaceLibrary>(); surfaces.Initialize(theme);
            Field(art.Group("FieldSurfaces", parent, Vector3.zero));
            Stands(art.Group("Grandstand", parent, Vector3.zero));
            Backstop(art.Group("Backstop", parent, Vector3.zero));
            Facilities(art.Group("Facilities", parent, Vector3.zero));
            Skyline(art.Group("Skyline", parent, Vector3.zero));
            art.CombineStatic(parent);
        }

        void Field(Transform parent)
        {
            for (int row = 0; row < 20; row++)
            {
                float z = -25 + row * 7;
                art.Surface("TurfStrip", parent, new[] { new Vector3(-95, -0.025f, z), new Vector3(95, -0.025f, z),
                    new Vector3(95, -0.025f, z + 7), new Vector3(-95, -0.025f, z + 7) },
                    new[] { 0, 2, 1, 0, 3, 2 }, Color.white, row % 2 == 0 ? surfaces.Grass : surfaces.GrassStripe);
            }
            art.Arc("InfieldClay", parent, 0, settings.BaseDistance + 3.4f, -56, 56, -0.006f, Color.white, surfaces.Dirt);
            art.Arc("HomeClay", parent, 0, 3.4f, -180, 180, 0, Color.white, surfaces.Dirt);
            float b = settings.BaseDistance / Mathf.Sqrt(2);
            art.Disc("PitchingCircle", parent, new Vector3(0, 0.005f, settings.PitchDistance), 1.65f, 0.014f, theme.Clay);
            art.Box("PitchingRubber", parent, new Vector3(0, 0.024f, settings.PitchDistance), new Vector3(0.61f, 0.02f, 0.15f), chalk);
            art.Surface("HomePlate", parent, new[] { new Vector3(-0.23f, 0.025f, 0.20f), new Vector3(0.23f, 0.025f, 0.20f),
                new Vector3(0.23f, 0.025f, -0.07f), new Vector3(0, 0.025f, -0.30f), new Vector3(-0.23f, 0.025f, -0.07f) },
                new[] { 0, 1, 2, 0, 2, 3, 0, 3, 4 }, chalk);
            foreach (var p in new[] { new Vector3(b, 0.035f, b), new Vector3(0, 0.035f, b * 2), new Vector3(-b, 0.035f, b) })
            {
                var plate = art.Box("Base", parent, p, new Vector3(0.40f, 0.07f, 0.40f), chalk);
                plate.localRotation = Quaternion.Euler(0, 45, 0);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Chalk(parent, "FoulLine", Vector3.zero, new Vector3(side, 0, 1).normalized * settings.FenceDistance, 0.065f);
                float x = side * 0.94f;
                Chalk(parent, "BattersBox", new Vector3(x - 0.31f, 0, -0.65f), new Vector3(x + 0.31f, 0, -0.65f), 0.04f);
                Chalk(parent, "BattersBox", new Vector3(x - 0.31f, 0, 0.60f), new Vector3(x + 0.31f, 0, 0.60f), 0.04f);
                foreach (float offset in new[] { -0.31f, 0.31f })
                    Chalk(parent, "BattersBox", new Vector3(x + offset, 0, -0.65f), new Vector3(x + offset, 0, 0.60f), 0.04f);
                var pole = new Vector3(side, 0, 1).normalized * settings.FenceDistance;
                art.Segment("FoulPole", parent, pole, pole + Vector3.up * 10, 0.06f, new Color(0.85f, 0.69f, 0.12f));
            }
            // Same radius/height as the analytical outfield fence for every forward-going ball.
            for (int i = 0; i < 80; i++)
            {
                float degrees = -100 + (i + 0.5f) * 2.5f, angle = degrees * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                float width = settings.FenceDistance * 2.5f * Mathf.Deg2Rad + 0.03f;
                var wall = art.Box("OutfieldWall", parent, direction * settings.FenceDistance + Vector3.up * settings.FenceHeight * 0.5f,
                    new Vector3(width, settings.FenceHeight, 0.16f), theme.Fence);
                wall.localRotation = Quaternion.Euler(0, degrees, 0);
                var cap = art.Box("FenceTop", parent, direction * settings.FenceDistance + Vector3.up * settings.FenceHeight,
                    new Vector3(width, 0.075f, 0.18f), new Color(0.73f, 0.65f, 0.26f)); cap.localRotation = wall.localRotation;
                if (i % 2 == 0)
                    art.Segment("FencePost", parent, direction * (settings.FenceDistance + 0.12f),
                        direction * (settings.FenceDistance + 0.12f) + Vector3.up * (settings.FenceHeight + 0.03f), 0.045f, steel);
            }
        }

        void Chalk(Transform parent, string name, Vector3 from, Vector3 to, float width)
        {
            var line = art.Box(name, parent, (from + to) * 0.5f + Vector3.up * 0.023f,
                new Vector3(width, 0.006f, Vector3.Distance(from, to)), chalk);
            line.localRotation = Quaternion.LookRotation(to - from);
        }

        void Stands(Transform parent)
        {
            int rows = Mathf.Clamp(theme.SeatingRows, 4, 14), columns = Mathf.Clamp(theme.SeatsPerRow, 96, 320);
            for (int row = 0; row < rows; row++)
            {
                float radius = settings.FenceDistance + 4 + row * 0.86f, y = 0.7f + row * 0.45f;
                art.Arc("ConcreteTerrace", parent, radius, radius + 0.86f, -110, 110, y, theme.Concrete);
                TerraceRiser(parent, radius, y);
                for (int i = 0; i < columns; i++)
                {
                    // Regular openings form radial aisles between eight seating blocks.
                    if (i % Mathf.Max(12, columns / 8) < 2) continue;
                    float degrees = Mathf.Lerp(-109, 109, i / (float)(columns - 1)), angle = degrees * Mathf.Deg2Rad;
                    var direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                    var position = direction * (radius + 0.42f) + Vector3.up * (y + 0.34f);
                    var seat = art.Box("Seat", parent, position, new Vector3(0.47f, 0.065f, 0.42f), theme.SeatColor);
                    seat.localRotation = Quaternion.Euler(0, degrees, 0);
                    var back = art.Box("SeatBack", parent, position + direction * 0.19f + Vector3.up * 0.22f,
                        new Vector3(0.47f, 0.41f, 0.055f), theme.SeatColor); back.localRotation = seat.localRotation;
                    if ((i * 13 + row * 7) % 11 == 0)
                    {
                        Color clothing = Color.Lerp(steel, theme.SeatColor, (i % 3) * 0.3f);
                        art.Orb("Spectator", parent, position + Vector3.up * 0.23f, new Vector3(0.32f, 0.43f, 0.24f), clothing);
                        art.Orb("SpectatorHead", parent, position + Vector3.up * 0.53f, Vector3.one * 0.18f, new Color(0.65f, 0.49f, 0.37f));
                    }
                }
            }
            float outer = settings.FenceDistance + 4 + rows * 0.86f;
            for (int i = 0; i < 44; i++)
            {
                float a = (-110 + i * 5) * Mathf.Deg2Rad, b = a + 5 * Mathf.Deg2Rad;
                Vector3 from = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * outer;
                Vector3 to = new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b)) * outer;
                float top = 0.7f + (rows - 1) * 0.45f + 1;
                art.Segment("StandRailing", parent, from + Vector3.up * top, to + Vector3.up * top, 0.025f, steel);
                art.Segment("RailingPost", parent, from, from + Vector3.up * top, 0.035f, steel);
                Vector3 direction = new Vector3(Mathf.Sin(a + 2.5f * Mathf.Deg2Rad), 0, Mathf.Cos(a + 2.5f * Mathf.Deg2Rad));
                var canopy = art.Box("StandCanopy", parent, direction * outer + Vector3.up * (top + 1.1f),
                    new Vector3(outer * 5 * Mathf.Deg2Rad + 0.08f, 0.14f, 4.8f), new Color(0.58f, 0.60f, 0.61f));
                canopy.localRotation = Quaternion.Euler(0, -107.5f + i * 5, 0);
                if (i % 3 == 0)
                    art.Segment("CanopySupport", parent, from + direction * 0.9f,
                        from + direction * 0.9f + Vector3.up * (top + 1.1f), 0.085f, steel);
            }
        }

        void TerraceRiser(Transform parent, float radius, float height)
        {
            const int count = 88;
            var vertices = new Vector3[(count + 1) * 2]; var triangles = new int[count * 6];
            for (int i = 0; i <= count; i++)
            {
                float angle = Mathf.Lerp(-110, 110, i / (float)count) * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
                vertices[i * 2] = position + Vector3.up * (height - 0.45f);
                vertices[i * 2 + 1] = position + Vector3.up * height;
                if (i == count) continue;
                int v = i * 2, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }
            art.Surface("TerraceRiser", parent, vertices, triangles, theme.Concrete);
        }

        void Backstop(Transform parent)
        {
            NetPanel(parent, new Vector3(-5.5f, 0, -5), new Vector3(5.5f, 0, -5), 6);
            NetPanel(parent, new Vector3(-5.5f, 0, -5), new Vector3(-8, 0, 1), 5);
            NetPanel(parent, new Vector3(5.5f, 0, -5), new Vector3(8, 0, 1), 5);
        }
        void NetPanel(Transform parent, Vector3 left, Vector3 right, float height)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            Vector3 horizontal = (right - left).normalized;
            int verticalLines = Mathf.CeilToInt(Vector3.Distance(left, right) / 0.23f);
            int horizontalLines = Mathf.CeilToInt(height / 0.23f);
            for (int i = 0; i <= verticalLines; i++)
            {
                Vector3 point = Vector3.Lerp(left, right, i / (float)verticalLines);
                Wire(vertices, triangles, point, point + Vector3.up * height, horizontal * 0.007f);
            }
            for (int i = 0; i <= horizontalLines; i++)
                Wire(vertices, triangles, left + Vector3.up * (height * i / horizontalLines),
                    right + Vector3.up * (height * i / horizontalLines), Vector3.up * 0.007f);
            art.Surface("SafetyNet", parent, vertices.ToArray(), triangles.ToArray(), steel);
            art.Segment("BackstopPost", parent, left, left + Vector3.up * height, 0.045f, steel);
            art.Segment("BackstopPost", parent, right, right + Vector3.up * height, 0.045f, steel);
            art.Segment("BackstopRail", parent, left + Vector3.up * height, right + Vector3.up * height, 0.035f, steel);
        }
        static void Wire(List<Vector3> vertices, List<int> triangles, Vector3 from, Vector3 to, Vector3 width)
        {
            int start = vertices.Count; vertices.AddRange(new[] { from - width, from + width, to + width, to - width });
            foreach (int index in new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 }) triangles.Add(start + index);
        }

        void Facilities(Transform parent)
        {
            float z = settings.FenceDistance + 8;
            art.Box("ScoreboardFrame", parent, new Vector3(0, 9.3f, z), new Vector3(16.4f, 5.7f, 0.65f), steel);
            art.Box("Scoreboard", parent, new Vector3(0, 9.3f, z - 0.38f), new Vector3(15.9f, 5.2f, 0.10f), new Color(0.055f, 0.065f, 0.07f));
            art.Lettering("ParkName", parent, new Vector3(0, 10.5f, z - 0.45f), Quaternion.identity, theme.ParkName, 0.24f, chalk);
            art.Lettering("ScoreboardHeader", parent, new Vector3(0, 9.1f, z - 0.45f), Quaternion.identity, "SOFTBALL CONNECT", 0.19f, new Color(0.76f, 0.78f, 0.70f));
            art.Lettering("Distance", parent, new Vector3(0, 7.9f, z - 0.45f), Quaternion.identity, settings.FenceDistance.ToString("0") + " m", 0.20f, chalk);
            foreach (float side in new[] { -1f, 1f })
            {
                art.Box("DugoutFloor", parent, new Vector3(side * 12.5f, 0, 2.5f), new Vector3(4.5f, 0.08f, 8), theme.Concrete);
                art.Box("DugoutRoof", parent, new Vector3(side * 12.5f, 2.45f, 2.5f), new Vector3(4.8f, 0.16f, 8.4f), theme.SeatColor);
                art.Box("DugoutBack", parent, new Vector3(side * 14.7f, 1.2f, 2.5f), new Vector3(0.14f, 2.4f, 8), theme.Concrete);
                art.Box("Bench", parent, new Vector3(side * 13.5f, 0.48f, 2.5f), new Vector3(0.48f, 0.09f, 6.5f), new Color(0.44f, 0.32f, 0.21f));
                foreach (float offset in new[] { -1.2f, 6.2f })
                    art.Segment("DugoutPost", parent, new Vector3(side * 10.4f, 0, offset), new Vector3(side * 10.4f, 2.45f, offset), 0.045f, steel);
                foreach (var point in new[] { new Vector3(side * 43, 0, 15), new Vector3(side * 38, 0, 49), new Vector3(side * 17, 0, -14) })
                    LightTower(parent, point);
            }
        }
        void LightTower(Transform parent, Vector3 ground)
        {
            const float height = 22;
            art.Shape("LightMast", PrimitiveType.Cylinder, parent, ground + Vector3.up * height * 0.5f,
                new Vector3(0.28f, height * 0.5f, 0.28f), steel);
            Vector3 head = ground + Vector3.up * height;
            art.Box("LightFrame", parent, head, new Vector3(4.2f, 1.5f, 0.22f), steel);
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 6; i++)
                    art.Box("Floodlight", parent, head + new Vector3(-1.7f + i * 0.68f, -0.35f + row * 0.7f, -0.18f),
                        new Vector3(0.52f, 0.47f, 0.18f), new Color(0.82f, 0.85f, 0.82f));
        }
        void Skyline(Transform parent)
        {
            for (int i = 0; i < 15; i++)
            {
                float height = 5 + (i * 7 % 11);
                art.Box("DistantBuilding", parent, new Vector3(-105 + i * 15, height * 0.5f, 108 + i % 3 * 6),
                    new Vector3(11, height, 12), Color.Lerp(theme.Concrete, theme.Sky, 0.6f));
            }
        }
    }
}
