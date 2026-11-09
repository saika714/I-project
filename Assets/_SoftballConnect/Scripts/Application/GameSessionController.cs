using System;
using SoftballConnect.Domain;

namespace SoftballConnect.Application
{
    public enum GameState { Prepare, PitchWindup, BallInFlight, BallInPlay, Feedback, RoundResult }

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

        public GameSessionController(SimulationSettings settings, IBatInputSource input)
        { settings.Validate(); this.settings = settings; this.input = input; pitch = new PitchService(settings); Restart(); }

        public static bool IsTransitionAllowed(GameState from, GameState to) =>
            (from == GameState.Prepare && to == GameState.PitchWindup) ||
            (from == GameState.PitchWindup && to == GameState.BallInFlight) ||
            (from == GameState.BallInFlight && (to == GameState.BallInPlay || to == GameState.Feedback)) ||
            (from == GameState.BallInPlay && to == GameState.Feedback) ||
            (from == GameState.Feedback && (to == GameState.Prepare || to == GameState.RoundResult));

        void Transition(GameState next)
        {
            if (!IsTransitionAllowed(State, next)) throw new InvalidOperationException(State + " -> " + next);
            State = next; stateTime = 0; StateChanged?.Invoke(next);
        }

        public void Restart()
        {
            Paused = false; BallNumber = Contacts = Resolved = 0;
            LastDistance = LastTimingOffset = 0; LastContact = default; LastResult = BallClassification.Miss;
            State = GameState.Prepare; stateTime = 0; ResetPitch(); StateChanged?.Invoke(State);
        }
        public void SetPaused(bool paused) { Paused = paused; }
        void ResetPitch()
        {
            input.Reset(); hasSwung = false; flight = null;
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

        // Driver supplies fixed 1/120 s steps. No wall clock or Unity physics dependency.
        public void Tick(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Paused || State == GameState.Feedback || State == GameState.RoundResult) return;
            stateTime += dt;
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
                BallPosition = pitch.PositionAt(stateTime);
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
                if (flight.Finished) { LastDistance = flight.Distance; Resolve(flight.Classification); }
            }
            previousBat = BatPose;
        }
    }
}
