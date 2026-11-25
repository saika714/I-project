using System;
using NUnit.Framework;
using SoftballConnect.Application;
using SoftballConnect.Domain;

namespace SoftballConnect.Tests
{
    public sealed class MatchAndPitchTests
    {
        const float Dt = 1f / 120;
        static void Until(GameSessionController game, GameState state)
        {
            for (int i = 0; i < 6000 && game.State != state; i++) game.Tick(Dt);
            Assert.That(game.State, Is.EqualTo(state));
        }
        static void Hit(GameSessionController game, DesktopSwingModel input)
        {
            Until(game, GameState.BallInFlight);
            while (game.PitchProgress < 0.90f) game.Tick(Dt);
            input.QueueSwing();
        }

        [TestCase(PitchType.Straight)]
        [TestCase(PitchType.Curve)]
        [TestCase(PitchType.Slider)]
        public void PitchTypesShareReleaseAndTargetAndStayHittable(PitchType type)
        {
            var settings = new SimulationSettings(); var pitch = new PitchService(settings);
            Assert.That((pitch.PositionAt(0, type) - pitch.Release).Magnitude, Is.LessThan(0.0001f));
            Assert.That((pitch.PositionAt(settings.PitchDuration, type) - pitch.Target).Magnitude, Is.LessThan(0.0001f));
            var input = new DesktopSwingModel(settings); var game = new GameSessionController(settings, input);
            for (int i = 0; i < (int)type; i++) game.CyclePracticePitch();
            Hit(game, input); Until(game, GameState.Feedback);
            Assert.That(game.Contacts, Is.EqualTo(1));
        }
        [Test]
        public void CurveAndSliderBendInOppositeDirectionsAndSelectionLocksDuringPitch()
        {
            var settings = new SimulationSettings(); var pitch = new PitchService(settings);
            Assert.That(pitch.PositionAt(settings.PitchDuration / 2, PitchType.Curve).X, Is.GreaterThan(0.5f));
            Assert.That(pitch.PositionAt(settings.PitchDuration / 2, PitchType.Slider).X, Is.LessThan(-0.3f));
            var game = new GameSessionController(settings, new DesktopSwingModel(settings));
            game.CyclePracticePitch(); Until(game, GameState.BallInFlight); game.CyclePracticePitch();
            Assert.That(game.CurrentPitch, Is.EqualTo(PitchType.Curve));
            Until(game, GameState.Feedback); game.CyclePracticePitch();
            Assert.That(game.CurrentPitch, Is.EqualTo(PitchType.Curve), "Feedback identifies the pitch just thrown.");
            game.Next(); Assert.That(game.CurrentPitch, Is.EqualTo(PitchType.Slider));
        }
        [TestCase(BallClassification.Miss, 50, 0)]
        [TestCase(BallClassification.Foul, 50, 0)]
        [TestCase(BallClassification.FairGrounder, 24.99f, 1)]
        [TestCase(BallClassification.FairFly, 25, 2)]
        [TestCase(BallClassification.FairFly, 40, 3)]
        [TestCase(BallClassification.HomeRun, 5, 4)]
        public void HitAwardBoundaries(BallClassification result, float distance, int expected)
        {
            Assert.That(BaseRunningPlay.BasesForHit(result, distance, new SimulationSettings()), Is.EqualTo(expected));
        }
        [Test]
        public void LoadedBasesHomeRunScoresFourOnlyAsRunnersCrossHome()
        {
            var settings = new SimulationSettings { BaseDistance = 10, RunSpeed = 10 };
            var play = new BaseRunningPlay(settings, 7, 4);
            Assert.That(play.Progress, Is.Zero);
            for (int i = 0; i < 4; i++)
            {
                Assert.That(play.Advance(0.9f), Is.Zero);
                Assert.That(play.Advance(0.11f), Is.EqualTo(1));
            }
            Assert.That(play.Finished, Is.True); Assert.That(play.OccupiedBases, Is.Zero);
            Assert.That(play.Advance(1), Is.Zero, "A finished play cannot score twice.");
        }
        [Test]
        public void SingleMovesEveryRunnerOneBaseAndPreservesOccupancy()
        {
            var settings = new SimulationSettings { BaseDistance = 10, RunSpeed = 10 };
            var play = new BaseRunningPlay(settings, 3, 1);
            Assert.That((play.Position(0) - BaseRunningPlay.BasePosition(10, 0)).Magnitude, Is.Zero);
            Assert.That(play.Advance(0.5f), Is.Zero);
            Assert.That((play.Position(0) - BaseRunningPlay.BasePosition(10, 1) * 0.5f).Magnitude, Is.LessThan(0.001f));
            Assert.That(play.Advance(0.5f), Is.Zero); Assert.That(play.OccupiedBases, Is.EqualTo(7));
        }
        [Test]
        public void MatchRequiresRunningFreezesOnPauseAndScoresExactlyOnce()
        {
            var settings = new SimulationSettings { BallsPerRound = 2 };
            var input = new DesktopSwingModel(settings); var game = new GameSessionController(settings, input, GameMode.Match);
            int runs = 0, resolved = 0; game.RunsScored += n => runs += n; game.PlayResolved += _ => resolved++;
            Hit(game, input); Until(game, GameState.BaseRunning);
            game.Next(); for (int i = 0; i < 300; i++) game.Tick(Dt);
            Assert.That(game.RunningPlay.Progress, Is.Zero); Assert.That(game.Score + resolved, Is.Zero);
            game.RunRequested = true; game.Tick(Dt); float progress = game.RunningPlay.Progress;
            game.SetPaused(true); for (int i = 0; i < 300; i++) game.Tick(Dt);
            Assert.That(game.RunningPlay.Progress, Is.EqualTo(progress));
            game.SetPaused(false); Until(game, GameState.Feedback);
            Assert.That(resolved, Is.EqualTo(1)); Assert.That(game.OccupiedBases, Is.EqualTo(4));
            game.Next(); Assert.That(game.CurrentPitch, Is.EqualTo(PitchType.Curve));
            Hit(game, input); Until(game, GameState.BaseRunning);
            Assert.That(game.LastResult, Is.EqualTo(BallClassification.HomeRun));
            Assert.That(game.Score, Is.Zero); game.RunRequested = true; Until(game, GameState.Feedback);
            Assert.That(game.Score, Is.EqualTo(2)); Assert.That(runs, Is.EqualTo(2)); Assert.That(resolved, Is.EqualTo(2));
            for (int i = 0; i < 300; i++) game.Tick(Dt);
            Assert.That(game.Score, Is.EqualTo(2));
            game.Next(); Assert.That(game.State, Is.EqualTo(GameState.RoundResult));
            game.Restart(); Assert.That(game.Score + game.OccupiedBases + game.LastRuns, Is.Zero);
            Assert.That(game.RunningPlay, Is.Null); Assert.That(game.RunRequested, Is.False);
        }
        [Test]
        public void MissNeverStartsRunningOrChangesBasesAndRestartCancelsRunning()
        {
            var settings = new SimulationSettings(); var input = new DesktopSwingModel(settings);
            var game = new GameSessionController(settings, input, GameMode.Match);
            Until(game, GameState.Feedback);
            Assert.That(game.RunningPlay, Is.Null); Assert.That(game.Score + game.OccupiedBases, Is.Zero);
            game.Next(); Hit(game, input); Until(game, GameState.BaseRunning);
            game.RunRequested = true; game.Tick(Dt); game.Restart();
            Assert.That(game.State, Is.EqualTo(GameState.Prepare)); Assert.That(game.RunningPlay, Is.Null);
            Assert.That(game.RunRequested, Is.False); Assert.That(game.Score + game.OccupiedBases, Is.Zero);
        }
        [Test]
        public void InvalidRunAndPitchSettingsFailBeforePlay()
        {
            Assert.Throws<ArgumentException>(() => new SimulationSettings { RunSpeed = 0 }.Validate());
            Assert.Throws<ArgumentException>(() => new SimulationSettings { CurveBreak = float.NaN }.Validate());
            Assert.Throws<ArgumentException>(() => new SimulationSettings { TripleDistance = 25 }.Validate());
        }
    }
}
