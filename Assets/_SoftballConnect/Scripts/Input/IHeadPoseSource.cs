using UnityEngine;

namespace SoftballConnect.Input
{
    // A pose relative to PlayerViewRig.Origin. XR adapters supply tracked poses here.
    public interface IHeadPoseSource
    {
        Pose SampleHeadPose(float deltaTime);
    }
}
