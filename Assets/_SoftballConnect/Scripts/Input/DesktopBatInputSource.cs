using SoftballConnect.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoftballConnect.Input
{
    public sealed class DesktopBatInputSource : MonoBehaviour, IBatInputSource
    {
        InputActionAsset actions;
        InputActionMap map;
        DesktopSwingModel motion;
        public void Initialize(InputActionAsset source, SimulationSettings settings)
        {
            motion = new DesktopSwingModel(settings);
            actions = Instantiate(source);
            map = actions.FindActionMap("Batting", true);
            map.Enable();
        }
        public bool Pressed(string name) => map != null && map.FindAction(name, true).WasPressedThisFrame();
        public void QueueSwing() => motion.QueueSwing();
        public BatInputFrame Sample(float dt) => motion.Sample(dt);
        public void Reset() => motion.Reset();
        void OnDisable() { map?.Disable(); }
        void OnEnable() { map?.Enable(); }
        void OnDestroy() { if (actions != null) Destroy(actions); }
    }
}
