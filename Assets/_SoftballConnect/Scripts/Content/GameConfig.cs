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
        public SimulationSettings CreateSettings() => JsonUtility.FromJson<SimulationSettings>(JsonUtility.ToJson(Simulation));
    }
}
