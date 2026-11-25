namespace SoftballConnect.Domain
{
    public enum PitchType { Straight, Curve, Slider }

    public sealed class PitchService
    {
        readonly SimulationSettings settings;
        public PitchService(SimulationSettings settings) { this.settings = settings; }
        public Vec3 Release => new Vec3(0, 0.75f, settings.PitchDistance);
        public Vec3 Target => new Vec3(0, 1, 0);
        public Vec3 PositionAt(float time, PitchType type = PitchType.Straight)
        {
            float t = time / settings.PitchDuration;
            float bend = (float)System.Math.Sin(System.Math.PI * Vec3.Clamp(t, 0, 1));
            float x = type == PitchType.Curve ? settings.CurveBreak * bend :
                type == PitchType.Slider ? -settings.SliderBreak * bend : 0;
            return Vec3.Lerp(Release, Target, t) + new Vec3(0,
                0.5f * settings.PitchGravity * time * (settings.PitchDuration - time), 0) + new Vec3(x, 0, 0);
        }
    }
}
