using System;

namespace SoftballConnect.Domain
{
    // Arcade rule: every occupied runner advances by the awarded hit distance.
    // Runs are awarded as runners actually cross home, never when the ball lands.
    public sealed class BaseRunningPlay
    {
        readonly SimulationSettings settings;
        readonly int startingBases;
        int scored;
        public int AwardedBases { get; }
        public float Progress { get; private set; }
        public int ReachedBase => Math.Min(AwardedBases, (int)Progress);
        public int NextBase => Math.Min(4, ReachedBase + 1);
        public bool Finished => Progress >= AwardedBases;
        public int OccupiedBases
        {
            get
            {
                int mask = 0;
                for (int start = 0; start <= 3; start++)
                    if (HasRunner(start) && start + AwardedBases < 4)
                        mask |= 1 << (start + AwardedBases - 1);
                return mask;
            }
        }

        public BaseRunningPlay(SimulationSettings settings, int occupiedBases, int awardedBases)
        {
            if (awardedBases < 1 || awardedBases > 4 || occupiedBases < 0 || occupiedBases > 7)
                throw new ArgumentOutOfRangeException();
            this.settings = settings; startingBases = occupiedBases; AwardedBases = awardedBases;
        }
        public bool HasRunner(int start) => start == 0 || (start >= 1 && start <= 3 && (startingBases & (1 << (start - 1))) != 0);
        public bool RunnerScored(int start) => start + Progress >= 4;
        public Vec3 Position(int start) => PositionAlong(settings.BaseDistance, start + Progress);
        public static Vec3 BasePosition(float baseDistance, int index)
        {
            float d = baseDistance / (float)Math.Sqrt(2);
            switch (index % 4)
            {
                case 1: return new Vec3(d, 0, d);
                case 2: return new Vec3(0, 0, d * 2);
                case 3: return new Vec3(-d, 0, d);
                default: return new Vec3(0, 0, 0);
            }
        }
        static Vec3 PositionAlong(float distance, float progress)
        {
            if (progress >= 4) return BasePosition(distance, 0);
            int segment = (int)progress;
            return Vec3.Lerp(BasePosition(distance, segment), BasePosition(distance, segment + 1), progress - segment);
        }
        public int Advance(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            Progress = Math.Min(AwardedBases, Progress + settings.RunSpeed * dt / settings.BaseDistance);
            int total = 0;
            for (int start = 0; start <= 3; start++)
                if (HasRunner(start) && RunnerScored(start)) total++;
            int added = total - scored; scored = total; return added;
        }
        public static int BasesForHit(BallClassification result, float distance, SimulationSettings settings)
        {
            if (result == BallClassification.HomeRun) return 4;
            if (result != BallClassification.FairGrounder && result != BallClassification.FairFly) return 0;
            return distance >= settings.TripleDistance ? 3 : distance >= settings.DoubleDistance ? 2 : 1;
        }
    }
}
