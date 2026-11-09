using System;

namespace SoftballConnect.Domain
{
    public readonly struct BatPose
    {
        public readonly Vec3 Grip, Tip;
        public BatPose(Vec3 grip, Vec3 tip) { Grip = grip; Tip = tip; }
        public static BatPose Lerp(BatPose a, BatPose b, float t) =>
            new BatPose(Vec3.Lerp(a.Grip, b.Grip, t), Vec3.Lerp(a.Tip, b.Tip, t));
    }

    public readonly struct BatInputFrame
    {
        public readonly BatPose BatPose;
        public readonly Vec3 LinearVelocity, AngularVelocity;
        public readonly bool SwingTriggered, IsSwingActive;
        public BatInputFrame(BatPose pose, Vec3 linear, Vec3 angular, bool triggered, bool active)
        { BatPose = pose; LinearVelocity = linear; AngularVelocity = angular; SwingTriggered = triggered; IsSwingActive = active; }
    }

    public interface IBatInputSource
    {
        BatInputFrame Sample(float deltaTime);
        void Reset();
    }

    // Converts desktop intent to geometry. XR can supply tracked geometry via the same interface.
    public sealed class DesktopSwingModel : IBatInputSource
    {
        readonly SimulationSettings settings;
        float elapsed;
        bool pending, running;
        public DesktopSwingModel(SimulationSettings settings) { this.settings = settings; Reset(); }
        public void QueueSwing() { if (!running) pending = true; }
        public BatPose PoseAt(float seconds)
        {
            float phase = Vec3.Clamp(seconds / settings.SwingDuration, 0, 1);
            float angle = -1.15f + 2.3f * phase;
            float ready = Math.Max(0, 1 - phase * 4);
            // Hold the ready bat at the right shoulder, keeping the pitcher's release visible.
            var grip = new Vec3(-0.78f + ready * 0.56f, 1 + ready * 0.20f, -ready * 0.10f);
            var direction = new Vec3((float)Math.Cos(angle), ready * 1.8f, (float)Math.Sin(angle)).Normalized;
            return new BatPose(grip, grip + direction * settings.BatLength);
        }
        public BatInputFrame Sample(float deltaTime)
        {
            bool triggered = pending;
            if (pending) { running = true; elapsed = 0; pending = false; }
            var previous = PoseAt(elapsed);
            bool active = running && elapsed < settings.SwingDuration;
            if (active) elapsed = Math.Min(settings.SwingDuration, elapsed + deltaTime);
            var current = PoseAt(elapsed);
            var beforeDirection = (previous.Tip - previous.Grip).Normalized;
            var afterDirection = (current.Tip - current.Grip).Normalized;
            var axis = Vec3.Cross(beforeDirection, afterDirection);
            float angleChange = (float)Math.Atan2(axis.Magnitude, Vec3.Dot(beforeDirection, afterDirection));
            var angular = axis.Magnitude > 0.00001f ? axis.Normalized * (angleChange / Math.Max(0.00001f, deltaTime)) : new Vec3();
            return new BatInputFrame(current, (current.Tip - previous.Tip) / Math.Max(0.00001f, deltaTime),
                angular, triggered, active);
        }
        public void Reset() { elapsed = 0; running = pending = false; }
    }
}
