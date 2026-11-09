using UnityEngine;

namespace SoftballConnect.Content
{
    [CreateAssetMenu(menuName = "Softball Connect/Visual Theme")]
    public sealed class VisualThemeConfig : ScriptableObject
    {
        [Header("Stadium surfaces")]
        public Color Turf = new Color(0.18f, 0.37f, 0.19f);
        public Color TurfStripe = new Color(0.21f, 0.41f, 0.22f);
        public Color Clay = new Color(0.55f, 0.39f, 0.27f);
        public Color HomeTeam = new Color(0.04f, 0.39f, 0.74f);
        public Color AwayTeam = new Color(0.96f, 0.28f, 0.25f);
        public Color Sky = new Color(0.48f, 0.80f, 0.94f);
        public Color Fence = new Color(0.05f, 0.32f, 0.34f);
        public string ParkName = "CONNECT PARK";
        public Material SurfaceMaterial, TrailMaterial, SkyMaterial;
        public Color SeatColor = new Color(0.12f, 0.29f, 0.48f);
        public Color Concrete = new Color(0.52f, 0.53f, 0.51f);
        [Range(4, 14)] public int SeatingRows = 12;
        [Range(96, 320)] public int SeatsPerRow = 224;
        [Tooltip("Optional authored stadium. When set, generated visual geometry is replaced by this prefab.")]
        public GameObject StadiumVisualPrefab;
        [Range(0, 1)] public float EffectsVolume = 0.22f;

    }
}
