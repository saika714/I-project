using System;

namespace SoftballConnect.Domain
{
    public enum BallClassification { Miss, FairGrounder, FairFly, Foul, HomeRun }

    public sealed class BallFlightSimulator
    {
        readonly SimulationSettings settings;
        public Vec3 Position { get; private set; }
        public Vec3 Velocity { get; private set; }
        public Vec3 LandingPoint { get; private set; }
        public bool Finished { get; private set; }
        public bool HasLanded { get; private set; }
        public bool ClearedFence { get; private set; }
        public float PeakHeight { get; private set; }
        public BallFlightSimulator(SimulationSettings settings, Vec3 position, Vec3 velocity)
        { this.settings = settings; Position = position; Velocity = velocity; PeakHeight = position.Y; }
        public static bool IsFair(Vec3 position) => position.Z >= 0 && Math.Abs(position.X) <= position.Z + 0.00001f;
        public float Distance => (float)Math.Sqrt(LandingPoint.X * LandingPoint.X + LandingPoint.Z * LandingPoint.Z);
        public BallClassification Classification => !IsFair(LandingPoint) ? BallClassification.Foul :
            ClearedFence ? BallClassification.HomeRun : PeakHeight > 2 ? BallClassification.FairFly : BallClassification.FairGrounder;

        public void Step(float dt)
        {
            if (Finished) return;
            var previous = Position;
            Velocity = (Velocity + new Vec3(0, -settings.Gravity * dt, 0)) / (1 + settings.Drag * dt);
            var next = Position + Velocity * dt;
            PeakHeight = Math.Max(PeakHeight, next.Y);
            float before = (float)Math.Sqrt(previous.X * previous.X + previous.Z * previous.Z);
            float after = (float)Math.Sqrt(next.X * next.X + next.Z * next.Z);
            if (before <= settings.FenceDistance && after > settings.FenceDistance)
            {
                // Solve segment/circular fence intersection, including the height at crossing.
                var d = next - previous;
                float a = d.X * d.X + d.Z * d.Z;
                float b = 2 * (previous.X * d.X + previous.Z * d.Z);
                float c = before * before - settings.FenceDistance * settings.FenceDistance;
                float f = a > 0 ? (-b + (float)Math.Sqrt(Math.Max(0, b * b - 4 * a * c))) / (2 * a) : 0;
                var crossing = Vec3.Lerp(previous, next, Vec3.Clamp(f, 0, 1));
                if (crossing.Y - settings.BallRadius <= settings.FenceHeight)
                { Position = crossing; LandingPoint = crossing; Finished = true; return; }
                ClearedFence = IsFair(crossing);
            }
            if (next.Y <= settings.BallRadius)
            {
                float f = Vec3.Clamp((previous.Y - settings.BallRadius) / (previous.Y - next.Y), 0, 1);
                Position = Vec3.Lerp(previous, next, f);
                LandingPoint = Position; HasLanded = Finished = true; return;
            }
            Position = next;
        }

        public void Stop() { LandingPoint = Position; Finished = true; }
    }
}
