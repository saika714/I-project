using SoftballConnect.Content;
using SoftballConnect.Input;
using UnityEngine;

namespace SoftballConnect.Presentation
{
    // Gameplay never translates this origin or turns the head to follow a ball.
    public sealed class PlayerViewRig : MonoBehaviour
    {
        IHeadPoseSource source;
        public Transform Origin { get; private set; }
        public Transform Head { get; private set; }
        public Camera PlayerCamera { get; private set; }

        public void Initialize(PlayerViewSettings settings, Vector3 lookTarget)
        {
            Origin = new GameObject("PlayerOrigin").transform; Origin.SetParent(transform, false);
            Origin.localPosition = new Vector3(settings.EyePosition.x, 0, settings.EyePosition.z);
            Head = new GameObject("Head").transform; Head.SetParent(Origin, false);
            PlayerCamera = Head.gameObject.AddComponent<Camera>(); Head.gameObject.AddComponent<AudioListener>();
            Head.tag = "MainCamera"; PlayerCamera.fieldOfView = settings.DesktopFieldOfView;
            PlayerCamera.nearClipPlane = 0.045f; PlayerCamera.farClipPlane = 250;
            var desktop = gameObject.AddComponent<DesktopHeadPoseSource>(); desktop.Initialize(settings, lookTarget);
            SetHeadPoseSource(desktop);
        }
        public void SetHeadPoseSource(IHeadPoseSource headPoseSource)
        {
            source = headPoseSource ?? throw new System.ArgumentNullException(nameof(headPoseSource));
            Present(0);
        }
        public void Present(float deltaTime)
        {
            // Tracked head motion continues even when the game is paused or displaying results.
            Pose pose = source.SampleHeadPose(deltaTime);
            Head.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
