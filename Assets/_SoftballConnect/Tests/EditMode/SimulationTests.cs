using System;
using NUnit.Framework;
using SoftballConnect.Application;
using SoftballConnect.Domain;

namespace SoftballConnect.Tests
{
    public sealed class SimulationTests
    {
        const float Dt = 1f / 120;
        [Test]
        public void DesktopBatLowersFromReadyWithoutChangingItsLength()
        {
            var settings = new SimulationSettings(); var motion = new DesktopSwingModel(settings);
            Assert.That(motion.PoseAt(0).Tip.Y, Is.GreaterThan(motion.PoseAt(0).Grip.Y));
            for (int i = 0; i <= 100; i++)
            {
                var pose = motion.PoseAt(settings.SwingDuration * i / 100);
                Assert.That((pose.Tip - pose.Grip).Magnitude, Is.EqualTo(settings.BatLength).Within(0.0001f));
            }
            var middle = motion.PoseAt(settings.SwingDuration * 0.5f);
            Assert.That(middle.Tip.Y, Is.EqualTo(middle.Grip.Y).Within(0.0001f));
            motion.QueueSwing(); var frame = motion.Sample(Dt);
            Assert.That(float.IsNaN(frame.AngularVelocity.Magnitude) || float.IsInfinity(frame.AngularVelocity.Magnitude), Is.False);
        }
        static void Until(GameSessionController game, GameState state, int limit = 5000)
        {
            for (int i = 0; i < limit && game.State != state; i++) game.Tick(Dt);
            Assert.That(game.State, Is.EqualTo(state));
        }
        [Test]
        public void PitchIsReproducibleAndReachesConfiguredTarget()
        {
            var config = new SimulationSettings(); var a = new PitchService(config); var b = new PitchService(config);
            for (int i = 0; i <= 120; i++)
            {
                var x = a.PositionAt(i * config.PitchDuration / 120); var y = b.PositionAt(i * config.PitchDuration / 120);
                Assert.That((x - y).SqrMagnitude, Is.EqualTo(0));
            }
            Assert.That((a.PositionAt(config.PitchDuration) - a.Target).Magnitude, Is.LessThan(0.0001f));
        }
        [Test]
        public void SweepCatchesFastCrossingThatEndpointTestsMiss()
        {
            var config = new SimulationSettings(); var bat = new BatPose(new Vec3(-1, 1, 0), new Vec3(1, 1, 0));
            Assert.That(BatContactSolver.TrySweep(bat, bat, new Vec3(0, 1, 2), new Vec3(0, 1, -2),
                Dt, config, false, 0, out var contact), Is.True);
            Assert.That(contact.Fraction, Is.GreaterThan(0.4f).And.LessThan(0.6f));
        }
        [Test]
        public void TemporalSweepRejectsPathsCrossingAtDifferentTimes()
        {
            var config = new SimulationSettings();
            var before = new BatPose(new Vec3(-1, 1, -2), new Vec3(1, 1, -2));
            var after = new BatPose(new Vec3(-1, 1, 0), new Vec3(1, 1, 0));
            Assert.That(BatContactSolver.TrySweep(before, after, new Vec3(0, 1, 0), new Vec3(0, 1, 2),
                Dt, config, true, 0, out _), Is.False);
        }
        [Test]
        public void AssistHasFiniteExplicitRadius()
        {
            var config = new SimulationSettings(); var bat = new BatPose(new Vec3(-1, 1, 0), new Vec3(1, 1, 0));
            var near = new Vec3(0, 1.16f, 0); var far = new Vec3(0, 2, 0);
            Assert.That(BatContactSolver.TrySweep(bat, bat, near, near, Dt, config, false, 0, out _), Is.False);
            Assert.That(BatContactSolver.TrySweep(bat, bat, near, near, Dt, config, true, 0, out _), Is.True);
            Assert.That(BatContactSolver.TrySweep(bat, bat, far, far, Dt, config, true, 0, out _), Is.False);
        }
        [TestCase(0f, 5f, true)]
        [TestCase(5f, 5f, true)]
        [TestCase(-5f, 5f, true)]
        [TestCase(5.01f, 5f, false)]
        [TestCase(0f, -0.01f, false)]
        public void FairFoulBoundary(float x, float z, bool fair) =>
            Assert.That(BallFlightSimulator.IsFair(new Vec3(x, 0, z)), Is.EqualTo(fair));
        [Test]
        public void FlightStopsAtInterpolatedGroundContact()
        {
            var config = new SimulationSettings(); var ball = new BallFlightSimulator(config, new Vec3(0, 1, 0), new Vec3(0, 10, 15));
            for (int i = 0; i < 5000 && !ball.Finished; i++) ball.Step(Dt);
            Assert.That(ball.Finished && ball.HasLanded, Is.True);
            Assert.That(ball.Position.Y, Is.EqualTo(config.BallRadius).Within(0.0001f));
            Assert.That(ball.Classification, Is.EqualTo(BallClassification.FairFly));
            Assert.That(ball.Distance, Is.GreaterThan(20));
        }
        [Test]
        public void FenceStopsBallAtActualCrossingHeight()
        {
            var config = new SimulationSettings { FenceDistance = 5, FenceHeight = 2.4f };
            var ball = new BallFlightSimulator(config, new Vec3(0, 1, 4.9f), new Vec3(0, 0, 20));
            ball.Step(0.1f);
            Assert.That(ball.Finished, Is.True);
            Assert.That(ball.Position.Z, Is.EqualTo(5).Within(0.0001f));
            Assert.That(ball.ClearedFence, Is.False);
        }
        [Test]
        public void FairBallAboveFenceIsHomeRunWhenItLands()
        {
            var config = new SimulationSettings { FenceDistance = 5, FenceHeight = 2 };
            var ball = new BallFlightSimulator(config, new Vec3(0, 5, 4.9f), new Vec3(0, 1, 10));
            for (int i = 0; i < 2000 && !ball.Finished; i++) ball.Step(Dt);
            Assert.That(ball.Classification, Is.EqualTo(BallClassification.HomeRun));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void TenConsecutiveTimedSwingsAndRetryStayConsistent(bool assist)
        {
            var config = new SimulationSettings { BallsPerRound = 10 };
            var input = new DesktopSwingModel(config); var game = new GameSessionController(config, input) { Assist = assist };
            int contacts = 0, resolved = 0;
            game.BallContacted += _ => contacts++; game.PlayResolved += _ => resolved++;
            for (int ball = 1; ball <= 10; ball++)
            {
                Until(game, GameState.BallInFlight);
                while (game.PitchProgress < 0.90f) game.Tick(Dt);
                input.QueueSwing();
                // Repeated intent cannot start a second swing or award another contact.
                for (int step = 0; step < 3000 && game.State != GameState.Feedback; step++)
                { input.QueueSwing(); game.Tick(Dt); }
                Assert.That(game.State, Is.EqualTo(GameState.Feedback));
                Assert.That(game.BallNumber, Is.EqualTo(ball)); Assert.That(game.Contacts, Is.EqualTo(ball));
                game.Next();
            }
            Assert.That(game.State, Is.EqualTo(GameState.RoundResult));
            Assert.That(contacts, Is.EqualTo(10)); Assert.That(resolved, Is.EqualTo(10));
            game.Restart(); Assert.That(game.BallNumber + game.Contacts + game.Resolved, Is.EqualTo(0));
            Assert.That(game.HasSwung || game.Paused, Is.False);
            Assert.That(game.State, Is.EqualTo(GameState.Prepare));
        }
        [TestCase(-0.6f)]
        [TestCase(0.25f)]
        public void EarlyOrLateSwingMisses(float offset)
        {
            var config = new SimulationSettings(); var input = new DesktopSwingModel(config); var game = new GameSessionController(config, input);
            Until(game, GameState.BallInFlight);
            while (game.State == GameState.BallInFlight && game.PitchProgress <
                (config.PitchDuration - config.SwingDuration * 0.5f + offset) / config.PitchDuration) game.Tick(Dt);
            input.QueueSwing(); Until(game, GameState.Feedback);
            Assert.That(game.LastResult, Is.EqualTo(BallClassification.Miss));
            Assert.That(game.Contacts, Is.EqualTo(0));
        }
        [Test]
        public void TakingPitchResolvesOnceAndFeedbackWaitsForExplicitNext()
        {
            var config = new SimulationSettings(); var input = new DesktopSwingModel(config); var game = new GameSessionController(config, input);
            Until(game, GameState.Feedback);
            for (int i = 0; i < 2000; i++) game.Tick(Dt);
            Assert.That(game.LastResult, Is.EqualTo(BallClassification.Miss));
            Assert.That(game.Resolved, Is.EqualTo(1)); Assert.That(game.BallNumber, Is.EqualTo(1));
            Assert.That(game.HasSwung, Is.False);
        }
        [TestCase(GameState.Prepare)]
        [TestCase(GameState.PitchWindup)]
        [TestCase(GameState.BallInFlight)]
        [TestCase(GameState.BallInPlay)]
        [TestCase(GameState.Feedback)]
        public void PauseFreezesAndRestartClearsEachActiveState(GameState state)
        {
            var config = new SimulationSettings(); var input = new DesktopSwingModel(config); var game = new GameSessionController(config, input);
            if (state == GameState.BallInPlay)
            {
                Until(game, GameState.BallInFlight); while (game.PitchProgress < 0.90f) game.Tick(Dt);
                input.QueueSwing();
            }
            Until(game, state);
            var position = game.BallPosition; var pose = game.BatPose;
            game.SetPaused(true);
            for (int i = 0; i < 500; i++) game.Tick(Dt);
            Assert.That(game.State, Is.EqualTo(state)); Assert.That((position - game.BallPosition).SqrMagnitude, Is.EqualTo(0));
            Assert.That((pose.Tip - game.BatPose.Tip).SqrMagnitude, Is.EqualTo(0));
            game.Next(); Assert.That(game.State, Is.EqualTo(state));
            game.Restart(); Assert.That(game.State, Is.EqualTo(GameState.Prepare));
            Assert.That(game.Paused || game.HasSwung, Is.False);
            Assert.That(game.Contacts + game.BallNumber + game.Resolved, Is.EqualTo(0));
        }
        [Test]
        public void StateMachineRejectsInvalidEdges()
        {
            Assert.That(GameSessionController.IsTransitionAllowed(GameState.Prepare, GameState.BallInPlay), Is.False);
            Assert.That(GameSessionController.IsTransitionAllowed(GameState.Feedback, GameState.BallInFlight), Is.False);
            Assert.That(GameSessionController.IsTransitionAllowed(GameState.BallInFlight, GameState.BallInPlay), Is.True);
        }
        [Test]
        public void InvalidConfigurationIsRejectedBeforePlay()
        {
            Assert.Throws<ArgumentException>(() => new SimulationSettings { PitchDuration = 0 }.Validate());
            Assert.Throws<ArgumentException>(() => new SimulationSettings { AssistRadius = float.NaN }.Validate());
            Assert.Throws<ArgumentException>(() => new SimulationSettings { BallsPerRound = 0 }.Validate());
        }
    }
}
