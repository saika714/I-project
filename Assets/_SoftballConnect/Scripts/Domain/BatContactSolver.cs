using System;

namespace SoftballConnect.Domain
{
    public readonly struct BatContact
    {
        public readonly Vec3 ContactPoint, ContactNormal, RelativeVelocity;
        public readonly float TimingOffset, SweetSpotOffset, Fraction;
        public BatContact(Vec3 point, Vec3 normal, Vec3 velocity, float timing, float sweet, float fraction)
        { ContactPoint = point; ContactNormal = normal; RelativeVelocity = velocity;
          TimingOffset = timing; SweetSpotOffset = sweet; Fraction = fraction; }
    }

    public static class BatContactSolver
    {
        // Both ball and bat are interpolated at the SAME time. Spatial sweeps alone can give false hits.
        // Adaptive temporal sampling limits relative motion per sample to 1/4 of the collision radius.
        public static bool TrySweep(BatPose previous, BatPose current, Vec3 ballBefore, Vec3 ballAfter,
            float deltaTime, SimulationSettings settings, bool assist, float timing, out BatContact contact)
        {
            contact = default;
            float radius = settings.BallRadius + settings.BatRadius + (assist ? settings.AssistRadius : 0);
            float travel = (ballAfter - ballBefore).Magnitude + Math.Max(
                (current.Grip - previous.Grip).Magnitude, (current.Tip - previous.Tip).Magnitude);
            int steps = Math.Max(2, (int)Math.Ceiling(travel / (radius * 0.25f)));
            for (int i = 0; i <= steps; i++)
            {
                float f = (float)i / steps;
                var pose = BatPose.Lerp(previous, current, f);
                var ball = Vec3.Lerp(ballBefore, ballAfter, f);
                var segment = pose.Tip - pose.Grip;
                float u = segment.SqrMagnitude < 0.000001f ? 0 :
                    Vec3.Clamp(Vec3.Dot(ball - pose.Grip, segment) / segment.SqrMagnitude, 0, 1);
                var closest = pose.Grip + segment * u;
                if ((ball - closest).SqrMagnitude > radius * radius) continue;
                Vec3 batVelocity = (Vec3.Lerp(current.Grip, current.Tip, u) -
                    Vec3.Lerp(previous.Grip, previous.Tip, u)) / deltaTime;
                contact = new BatContact(ball, (ball - closest).Normalized,
                    batVelocity - (ballAfter - ballBefore) / deltaTime, timing,
                    Math.Abs(u - settings.SweetSpot), f);
                return true;
            }
            return false;
        }

        public static Vec3 ExitVelocity(BatContact contact)
        {
            float quality = Vec3.Clamp(1 - contact.SweetSpotOffset, 0.35f, 1);
            float speed = Vec3.Clamp(contact.RelativeVelocity.Magnitude * 1.65f, 10, 32) * quality;
            float direction = Vec3.Clamp(contact.TimingOffset * 8, -1.35f, 1.35f);
            float elevation = 0.18f + 0.5f * quality;
            return new Vec3((float)Math.Sin(direction) * (float)Math.Cos(elevation),
                (float)Math.Sin(elevation), (float)Math.Cos(direction) * (float)Math.Cos(elevation)) * speed;
        }
    }
}
