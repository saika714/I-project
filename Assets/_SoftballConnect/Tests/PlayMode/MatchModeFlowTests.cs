using System.Collections;
using System.IO;
using NUnit.Framework;
using SoftballConnect.Application;
using SoftballConnect.Domain;
using SoftballConnect.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SoftballConnect.Tests
{
    public sealed class MatchModeFlowTests
    {
        const float Dt = 1f / 120;
        static void Until(GameSessionController game, GameState state)
        {
            for (int i = 0; i < 6000 && game.State != state; i++) game.Tick(Dt);
            Assert.That(game.State, Is.EqualTo(state));
        }
        static void Hit(GameBootstrap root)
        {
            Until(root.Session, GameState.BallInFlight);
            while (root.Session.PitchProgress < 0.90f) root.Session.Tick(Dt);
            root.InputSource.QueueSwing(); Until(root.Session, GameState.BaseRunning);
        }
        static void Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            string path = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../TestResults/MatchModes/", name + ".png"));
            UnityFrameCapture.Save(Object.FindAnyObjectByType<Camera>(), Object.FindAnyObjectByType<Canvas>(), path, 1600, 900);
        }

        [UnityTest]
        public IEnumerator MatchMenuRunningInputArrowsScoringAndRestartAreConnected()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            GameObject.Find("PlayerUI/FeedbackCard/Match").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            var root = Object.FindAnyObjectByType<GameBootstrap>(); var game = root.Session;
            Assert.That(game.Mode, Is.EqualTo(GameMode.Match));
            Hit(root);
            game.RunRequested = true;
            while (game.RunningPlay.Progress < 0.3f) game.Tick(Dt);
            game.RunRequested = false; yield return null;
            var arrow = GameObject.Find("NextBaseArrow").GetComponent<LineRenderer>();
            Assert.That(arrow.enabled, Is.True);
            Assert.That(GameObject.Find("Runner0"), Is.Not.Null);
            Assert.That(GameObject.Find("PlayerUI/BaseDiamond/Run"), Is.Not.Null);
            Assert.That(GameObject.Find("PlayerUI/BaseDiamond/RouteArrow"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ArcadeSound>().MusicSource.clip, Is.Not.Null);
            Canvas.ForceUpdateCanvases(); Capture("Running_EN");
            root.ToggleLanguage(); yield return null; Capture("Running_JA"); root.ToggleLanguage();
            float before = game.RunningPlay.Progress;
            var previousFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                float deadline = Time.realtimeSinceStartup + 2;
                while (game.RunningPlay.Progress == before && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(game.RunningPlay.Progress, Is.GreaterThan(before));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
                before = game.RunningPlay.Progress;
                yield return null; yield return null;
                Assert.That(game.RunningPlay.Progress, Is.EqualTo(before), "Releasing W stops running.");
                GameObject.Find("PlayerUI/BaseDiamond/Run").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(game.RunRequested, Is.True);
                while (game.RunningPlay.Progress < 1.15f) game.Tick(Dt);
                yield return null;
                Assert.That(game.RunningPlay.NextBase, Is.EqualTo(2));
                Vector3 nextBase = StadiumView.ToUnity(BaseRunningPlay.BasePosition(root.Config.Simulation.BaseDistance, 2));
                nextBase.y = 0.16f;
                Assert.That(Vector3.Distance(arrow.GetPosition(1), nextBase), Is.LessThan(0.01f));
                root.ToggleLanguage(); yield return null; Capture("RunningToSecond_JA"); root.ToggleLanguage();
                root.TogglePause(); before = game.RunningPlay.Progress;
                yield return null; yield return null;
                Assert.That(game.RunningPlay.Progress, Is.EqualTo(before));
                Assert.That(arrow.enabled, Is.False);
                root.TogglePause(); game.RunRequested = true; Until(game, GameState.Feedback);
                Assert.That(game.OccupiedBases, Is.EqualTo(4));
                root.Primary(); Hit(root);
                int events = 0; game.RunsScored += n => events += n;
                game.RunRequested = true; Until(game, GameState.Feedback); yield return null;
                Assert.That(game.Score, Is.EqualTo(2)); Assert.That(events, Is.EqualTo(2));
                Assert.That(GameObject.Find("PlayerUI/Scoreboard/Score").GetComponent<Text>().text, Does.Contain("2"));
                Capture("Scored_EN"); root.ToggleLanguage(); yield return null; Capture("Scored_JA"); root.ToggleLanguage();
                root.Restart(); yield return null;
                Assert.That(game.Score + game.OccupiedBases, Is.Zero);
                Assert.That(arrow.enabled, Is.False);
                Assert.That(Object.FindAnyObjectByType<ArcadeSound>().MusicSource.isPlaying, Is.False);
                root.Menu(); yield return null; yield return null;
                // Restore Practice as the default for tests and direct stadium entry.
                Object.FindAnyObjectByType<GameBootstrap>().Primary(); yield return null; yield return null;
                Assert.That(Object.FindAnyObjectByType<GameBootstrap>().Session.Mode, Is.EqualTo(GameMode.Practice));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode = previousFocus;
                InputSystem.settings.backgroundBehavior = previousBackground;
            }
        }

        [UnityTest]
        public IEnumerator PracticePitchButtonChangesVisiblePitchWithoutStartingRunning()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            Object.FindAnyObjectByType<GameBootstrap>().Primary(); yield return null; yield return null;
            var root = Object.FindAnyObjectByType<GameBootstrap>();
            var button = GameObject.Find("PlayerUI/SelectPitch").GetComponent<Button>(); button.onClick.Invoke();
            Assert.That(root.Session.CurrentPitch, Is.EqualTo(PitchType.Curve));
            Until(root.Session, GameState.BallInFlight); yield return null;
            Assert.That(button.interactable, Is.False);
            Assert.That(GameObject.Find("PlayerUI/PitchBadge/PitchType").GetComponent<Text>().text, Does.Contain("CURVE"));
            Capture("CurvePractice_EN"); root.ToggleLanguage(); yield return null; Capture("CurvePractice_JA"); root.ToggleLanguage();
            while (root.Session.PitchProgress < 0.90f) root.Session.Tick(Dt);
            root.InputSource.QueueSwing(); Until(root.Session, GameState.Feedback);
            Assert.That(root.Session.RunningPlay, Is.Null); Assert.That(root.Session.Score, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ConfiguredScoreClipIsUsedAndRemainsOwnedByContent()
        {
            var settings = new SimulationSettings { FenceDistance = 5, FenceHeight = 0.5f };
            var input = new DesktopSwingModel(settings); var game = new GameSessionController(settings, input, GameMode.Match);
            var clip = AudioClip.Create("Replacement score music", 22050, 1, 22050, false);
            var go = new GameObject("CustomScoreSound"); var sound = go.AddComponent<ArcadeSound>();
            sound.Initialize(game, 0, clip, 0.4f);
            Assert.That(sound.MusicSource.clip, Is.SameAs(clip));
            Assert.That(sound.MusicSource.volume, Is.EqualTo(0.4f).Within(0.001f));
            Until(game, GameState.BallInFlight); while (game.PitchProgress < 0.90f) game.Tick(Dt);
            input.QueueSwing(); Until(game, GameState.BaseRunning);
            Assert.That(game.AwardedBases, Is.EqualTo(4)); game.RunRequested = true; Until(game, GameState.Feedback);
            Assert.That(game.Score, Is.EqualTo(1));
            Assert.That(sound.MusicSource.isPlaying, Is.True, "Scoring starts the configured music.");
            game.Restart(); Assert.That(sound.MusicSource.isPlaying, Is.False);
            Object.Destroy(go); yield return null;
            Assert.That(clip != null, Is.True, "Destroying a sound presenter must not destroy an authored clip.");
            Object.Destroy(clip); LogAssert.NoUnexpectedReceived();
        }
    }
}
