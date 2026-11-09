using SoftballConnect.Content;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoftballConnect.Input
{
    public sealed class DesktopHeadPoseSource : MonoBehaviour, IHeadPoseSource
    {
        float height, yaw, pitch, initialYaw, sensitivity;
        public void Initialize(PlayerViewSettings settings, Vector3 lookTarget)
        {
            height = settings.EyePosition.y; sensitivity = settings.MouseSensitivity;
            Vector3 direction = lookTarget - settings.EyePosition;
            initialYaw = yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
        }
        public Pose SampleHeadPose(float deltaTime)
        {
            var mouse = Mouse.current;
            if (deltaTime > 0 && mouse != null && mouse.rightButton.isPressed)
            {
                var movement = mouse.delta.ReadValue() * sensitivity;
                yaw = Mathf.Clamp(yaw + movement.x, initialYaw - 110, initialYaw + 110);
                pitch = Mathf.Clamp(pitch - movement.y, -65, 75);
            }
            return new Pose(Vector3.up * height, Quaternion.Euler(pitch, yaw, 0));
        }
    }
}
