using System.Collections;
using System.IO;
using NUnit.Framework;
using SoftballConnect.Application;
using SoftballConnect.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

namespace SoftballConnect.Tests
{
    public sealed class MvpFlowTests
    {
        [UnityTest]
        public IEnumerator SpaceInputReachesSimulationAndPauseFreezesDriver()
        {
            yield return SceneManager.LoadSceneAsync("Stadium_PC"); yield return null;
            var root = Object.FindAnyObjectByType<GameBootstrap>(); var game = root.Session;
            const float dt = 1f / 120;
            for (int i = 0; i < 1000 && game.State != GameState.BallInFlight; i++) game.Tick(dt);
            while (game.PitchProgress < 0.88f) game.Tick(dt);
            var previousFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                Assert.That(game.HasSwung, Is.True, "Space must pass through the Input Action adapter and Unity Update.");
                for (int i = 0; i < 120 && game.State == GameState.BallInFlight; i++) game.Tick(dt);
                Assert.That(game.State, Is.EqualTo(GameState.BallInPlay));
                yield return null; CaptureFrame("Contact");
                root.TogglePause(); var position = game.BallPosition;
                yield return null; yield return null;
                Assert.That((position - game.BallPosition).SqrMagnitude, Is.EqualTo(0));
                root.TogglePause();
                float deadline = Time.realtimeSinceStartup + 1;
                while ((position - game.BallPosition).SqrMagnitude == 0 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((position - game.BallPosition).Magnitude, Is.GreaterThan(0));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode = previousFocus;
                InputSystem.settings.backgroundBehavior = previousBackground;
            }
        }
        static IEnumerator WaitForScene(string name)
        {
            for (int i = 0; i < 180 && SceneManager.GetActiveScene().name != name; i++) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name)); yield return null;
        }
        [UnityTest]
        public IEnumerator MenuFiveBallsResultRetryAndMenu()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForScene("MainMenu");
            var menu = Object.FindAnyObjectByType<GameBootstrap>(); Assert.That(menu.Session, Is.Null);
            Assert.That(GameObject.Find("MenuMascot"), Is.Not.Null);
            Assert.That(menu.Japanese, Is.False, "A fresh boot must use English for international participants.");
            yield return null; yield return null;
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) yield return new WaitForSecondsRealtime(0.2f);
            CaptureFrame("MainMenu_EN"); menu.ToggleLanguage(); yield return null;
            CaptureFrame("MainMenu_JA"); menu.ToggleLanguage();
            var start = GameObject.Find("PlayerUI/FeedbackCard/Primary").GetComponent<Button>(); start.onClick.Invoke();
            yield return WaitForScene("Stadium_PC");
            var root = Object.FindAnyObjectByType<GameBootstrap>(); var game = root.Session;
            Assert.That(root.Config.Visuals.SurfaceMaterial, Is.Not.Null);
            Assert.That(root.Config.Visuals.TrailMaterial, Is.Not.Null);
            Assert.That(GameObject.Find("StadiumBatch"), Is.Not.Null);
            Assert.That(GameObject.Find("Pitcher/ModelRoot/Head"), Is.Not.Null);
            foreach (var collider in GameObject.Find("GamePresentation").GetComponentsInChildren<Collider>())
                Assert.That(collider.enabled, Is.False, "Presentation must not add physics to the analytical simulation.");
            int contacts = 0; game.BallContacted += _ => contacts++;
            const float dt = 1f / 120;
            for (int ball = 1; ball <= 5; ball++)
            {
                for (int i = 0; i < 1000 && game.State != GameState.BallInFlight; i++) game.Tick(dt);
                Assert.That(game.State, Is.EqualTo(GameState.BallInFlight));
                if (ball == 1)
                {
                    yield return null; CaptureFrame("Stadium_PC");
                    root.ToggleLanguage(); yield return null; CaptureFrame("Stadium_PC_JA"); root.ToggleLanguage();
                }
                while (game.PitchProgress < 0.90f) game.Tick(dt);
                root.InputSource.QueueSwing();
                for (int i = 0; i < 3000 && game.State != GameState.Feedback; i++) game.Tick(dt);
                Assert.That(game.State, Is.EqualTo(GameState.Feedback)); Assert.That(game.Contacts, Is.EqualTo(ball));
                yield return null;
                Assert.That(GameObject.Find("PlayerUI/FeedbackCard"), Is.Not.Null);
                if (ball == 1 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                    yield return new WaitForSecondsRealtime(0.7f);
                if (ball == 1) CaptureFrame("Feedback_EN");
                root.ToggleLanguage(); yield return null;
                if (ball == 1) CaptureFrame("Feedback_JA");
                root.ToggleLanguage();
                root.Primary();
            }
            Assert.That(game.State, Is.EqualTo(GameState.RoundResult)); Assert.That(contacts, Is.EqualTo(5));
            yield return null;
            Canvas.ForceUpdateCanvases();
            var score = GameObject.Find("PlayerUI/FeedbackCard/Metric").GetComponent<Text>();
            Assert.That(score.preferredHeight, Is.LessThanOrEqualTo(score.rectTransform.rect.height + 1), "English contact score must fit the result card.");
            CaptureFrame("RoundResult_EN");
            root.ToggleLanguage(); yield return null; CaptureFrame("RoundResult_JA"); root.ToggleLanguage();
            root.Primary(); Assert.That(game.State, Is.EqualTo(GameState.Prepare)); Assert.That(game.Contacts, Is.EqualTo(0));
            root.TogglePause(); var p = game.BallPosition; yield return null; yield return null;
            CaptureFrame("Pause");
            Assert.That((p - game.BallPosition).SqrMagnitude, Is.EqualTo(0));
            root.Restart(); Assert.That(game.Paused, Is.False);
            root.Menu(); yield return WaitForScene("MainMenu");
            LogAssert.NoUnexpectedReceived();
        }

        static void CaptureFrame(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            string directory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../TestResults/Visuals"));
            UnityFrameCapture.Save(Object.FindAnyObjectByType<Camera>(), Object.FindAnyObjectByType<Canvas>(),
                Path.Combine(directory, name + ".png"), 1600, 900);
        }
    }
}
