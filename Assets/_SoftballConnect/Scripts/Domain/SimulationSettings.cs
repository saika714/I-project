using System;

namespace SoftballConnect.Domain
{
    [Serializable]
    public sealed class SimulationSettings
    {
        public float PitchDistance = 13.11f;
        public float BaseDistance = 18.29f;
        public float FenceDistance = 50;
        public float FenceHeight = 2.4f;
        public float PitchDuration = 1.5f;
        public float WindupDuration = 1.2f;
        public float PitchGravity = 3;
        public float BallRadius = 0.06f;
        public float BatLength = 1.05f;
        public float BatRadius = 0.045f;
        public float SweetSpot = 0.78f;
        public float SwingDuration = 0.3f;
        public float AssistRadius = 0.10f;
        public float Gravity = 9.81f;
        public float Drag = 0.035f;
        public float MaxFlightDuration = 12;
        public int BallsPerRound = 5;
        public float RunSpeed = 7;
        public float DoubleDistance = 25;
        public float TripleDistance = 40;
        public float CurveBreak = 0.9f;
        public float SliderBreak = 0.55f;

        public void Validate()
        {
            foreach (float v in new[] { PitchDistance, BaseDistance, FenceDistance, FenceHeight,
                PitchDuration, WindupDuration, BallRadius, BatLength, BatRadius, SwingDuration,
                Gravity, MaxFlightDuration, RunSpeed, DoubleDistance, TripleDistance })
                if (float.IsNaN(v) || float.IsInfinity(v) || v <= 0)
                    throw new ArgumentException("Lengths, times and gravity must be finite and positive.");
            foreach (float v in new[] { AssistRadius, Drag, PitchGravity, CurveBreak, SliderBreak })
                if (float.IsNaN(v) || float.IsInfinity(v) || v < 0)
                    throw new ArgumentException("Assist, drag and pitch gravity must be finite and nonnegative.");
            if (float.IsNaN(SweetSpot) || SweetSpot < 0 || SweetSpot > 1 || BallsPerRound < 1)
                throw new ArgumentException("Invalid sweet spot or ball count.");
            if (TripleDistance <= DoubleDistance) throw new ArgumentException("Triple distance must exceed double distance.");
        }
    }
}
