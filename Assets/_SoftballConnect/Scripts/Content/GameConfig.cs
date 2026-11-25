using SoftballConnect.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoftballConnect.Content
{
    [CreateAssetMenu(menuName = "Softball Connect/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        public SimulationSettings Simulation = new SimulationSettings();
        public PlayerViewSettings PlayerView = new PlayerViewSettings();
        public InputActionAsset DesktopActions;
        public TextAsset Localization;
        public VisualThemeConfig Visuals;
        [Header("Scoring music")]
        [Tooltip("Played when a runner crosses home in Match mode. Leave empty for the original synthesized fanfare.")]
        public AudioClip ScoreMusic;
        [Range(0, 1)] public float ScoreMusicVolume = 0.35f;
        public SimulationSettings CreateSettings() => JsonUtility.FromJson<SimulationSettings>(JsonUtility.ToJson(Simulation));
    }
}
