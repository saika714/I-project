using System;
using SoftballConnect.Domain;

namespace SoftballConnect.Application
{
    public enum GameState { Prepare, PitchWindup, BallInFlight, BallInPlay, Feedback, RoundResult, BaseRunning }
    public enum GameMode { Practice, Match }

    // Single owner of game progression. No Unity, input devices or scene references.
    public sealed class GameSessionController
    {
        readonly SimulationSettings settings;
        readonly IBatInputSource input;
        readonly PitchService pitch;
        float stateTime, swingStartedAt;
        bool hasSwung;
        BatPose previousBat;
        BallFlightSimulator flight;
        public GameState State { get; private set; }
        public GameMode Mode { get; }
        public PitchType CurrentPitch { get; private set; }
        public PitchType PracticePitch { get; private set; }
        public BaseRunningPlay RunningPlay { get; private set; }
        public bool RunRequested { get; set; }
        public int Score { get; private set; }
        public int LastRuns { get; private set; }
        public int OccupiedBases { get; private set; }
        public int AwardedBases { get; private set; }
        public bool Paused { get; private set; }
        public bool Assist { get; set; } = true;
        public bool HasSwung => hasSwung;
        public bool CanSwing => State == GameState.BallInFlight && !Paused && !hasSwung;
        public int BallNumber { get; private set; }
        public int Contacts { get; private set; }
        public int Resolved { get; private set; }
        public int BallsPerRound => settings.BallsPerRound;
        public float PitchProgress => State == GameState.BallInFlight ? stateTime / settings.PitchDuration : 0;
        public float WindupProgress => State == GameState.PitchWindup ? stateTime / settings.WindupDuration : 0;
        public Vec3 BallPosition { get; private set; }
        public BatPose BatPose { get; private set; }
        public BatContact LastContact { get; private set; }
        public BallClassification LastResult { get; private set; }
        public float LastDistance { get; private set; }
        public float LastTimingOffset { get; private set; }
        public bool BallVisible => State != GameState.Prepare && State != GameState.RoundResult;
        public event Action<GameState> StateChanged;
        public event Action<BatContact> BallContacted;
        public event Action<BallClassification> PlayResolved;
        public event Action<int> RunsScored;

        public GameSessionController(SimulationSettings settings, IBatInputSource input, GameMode mode = GameMode.Practice)
        { settings.Validate(); this.settings = settings; this.input = input; Mode = mode; pitch = new PitchService(settings); Restart(); }

        public void CyclePracticePitch()
        {
            if (Mode != GameMode.Practice || Paused || (State != GameState.Prepare && State != GameState.Feedback && State != GameState.RoundResult)) return;
            PracticePitch = (PitchType)(((int)PracticePitch + 1) % 3);
            if (State == GameState.Prepare) CurrentPitch = PracticePitch;
        }

        public static bool IsTransitionAllowed(GameState from, GameState to) =>
            (from == GameState.Prepare && to == GameState.PitchWindup) ||
            (from == GameState.PitchWindup && to == GameState.BallInFlight) ||
            (from == GameState.BallInFlight && (to == GameState.BallInPlay || to == GameState.Feedback)) ||
            (from == GameState.BallInPlay && (to == GameState.Feedback || to == GameState.BaseRunning)) ||
            (from == GameState.BaseRunning && to == GameState.Feedback) ||
            (from == GameState.Feedback && (to == GameState.Prepare || to == GameState.RoundResult));

        void Transition(GameState next)
        {
            if (!IsTransitionAllowed(State, next)) throw new InvalidOperationException(State + " -> " + next);
            State = next; stateTime = 0; StateChanged?.Invoke(next);
        }

        public void Restart()
        {
            Paused = false; BallNumber = Contacts = Resolved = 0;
            Score = LastRuns = OccupiedBases = AwardedBases = 0; RunningPlay = null; RunRequested = false;
            LastDistance = LastTimingOffset = 0; LastContact = default; LastResult = BallClassification.Miss;
            State = GameState.Prepare; stateTime = 0; ResetPitch(); StateChanged?.Invoke(State);
        }
        public void SetPaused(bool paused) { Paused = paused; }
        void ResetPitch()
        {
            input.Reset(); hasSwung = false; flight = null;
            RunningPlay = null; RunRequested = false; LastRuns = AwardedBases = 0;
            CurrentPitch = Mode == GameMode.Match ? (PitchType)(BallNumber % 3) : PracticePitch;
            previousBat = BatPose = input.Sample(0).BatPose;
            BallPosition = pitch.Release;
        }
        public void Next()
        {
            if (Paused || State != GameState.Feedback) return;
            if (BallNumber >= settings.BallsPerRound) Transition(GameState.RoundResult);
            else { ResetPitch(); Transition(GameState.Prepare); }
        }
        void Resolve(BallClassification result)
        {
            LastResult = result; Resolved++;
            Transition(GameState.Feedback); PlayResolved?.Invoke(result);
        }

        void FinishFlight()
        {
            LastDistance = flight.Distance; LastResult = flight.Classification;
            AwardedBases = BaseRunningPlay.BasesForHit(LastResult, LastDistance, settings);
            if (Mode == GameMode.Match && AwardedBases > 0)
            {
                RunningPlay = new BaseRunningPlay(settings, OccupiedBases, AwardedBases);
                Transition(GameState.BaseRunning);
            }
            else Resolve(LastResult);
        }

        // Driver supplies fixed 1/120 s steps. No wall clock or Unity physics dependency.
        public void Tick(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Paused || State == GameState.Feedback || State == GameState.RoundResult) return;
            stateTime += dt;
            if (State == GameState.BaseRunning)
            {
                if (!RunRequested) return;
                int runs = RunningPlay.Advance(dt);
                if (runs > 0) { Score += runs; LastRuns += runs; RunsScored?.Invoke(runs); }
                if (RunningPlay.Finished)
                {
                    OccupiedBases = RunningPlay.OccupiedBases; RunRequested = false; Resolve(LastResult);
                }
                return;
            }
            if (State == GameState.Prepare)
            {
                if (stateTime >= 0.5f) { BallNumber++; Transition(GameState.PitchWindup); }
                return;
            }
            if (State == GameState.PitchWindup)
            {
                if (stateTime >= settings.WindupDuration) Transition(GameState.BallInFlight);
                return;
            }
            var frame = input.Sample(dt);
            BatPose = frame.BatPose;
            if (State == GameState.BallInFlight)
            {
                if (frame.SwingTriggered && !hasSwung)
                {
                    hasSwung = true; swingStartedAt = stateTime - dt;
                    LastTimingOffset = swingStartedAt + settings.SwingDuration * 0.5f - settings.PitchDuration;
                }
                var before = BallPosition;
                BallPosition = pitch.PositionAt(stateTime, CurrentPitch);
                if (hasSwung && frame.IsSwingActive && BatContactSolver.TrySweep(previousBat, BatPose,
                    before, BallPosition, dt, settings, Assist,
                    swingStartedAt + settings.SwingDuration * 0.5f - settings.PitchDuration, out var contact))
                {
                    LastContact = contact; Contacts++;
                    BallPosition = contact.ContactPoint;
                    flight = new BallFlightSimulator(settings, BallPosition, BatContactSolver.ExitVelocity(contact));
                    Transition(GameState.BallInPlay); BallContacted?.Invoke(contact);
                }
                else if (stateTime > settings.PitchDuration + 0.35f)
                { LastContact = default; LastDistance = 0; Resolve(BallClassification.Miss); }
            }
            else if (State == GameState.BallInPlay)
            {
                flight.Step(dt); BallPosition = flight.Position;
                if (stateTime >= settings.MaxFlightDuration) flight.Stop();
                if (flight.Finished) FinishFlight();
            }
            previousBat = BatPose;
        }
    }
}
