namespace SoftballConnect.Domain
{
    public sealed class PitchService
    {
        readonly SimulationSettings settings;
        public PitchService(SimulationSettings settings) { this.settings = settings; }
        public Vec3 Release => new Vec3(0, 0.75f, settings.PitchDistance);
        public Vec3 Target => new Vec3(0, 1, 0);
        public Vec3 PositionAt(float time)
        {
            float t = time / settings.PitchDuration;
            return Vec3.Lerp(Release, Target, t) + new Vec3(0,
                0.5f * settings.PitchGravity * time * (settings.PitchDuration - time), 0);
        }
    }
}
