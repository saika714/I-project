using System.Collections;
using NUnit.Framework;
using SoftballConnect.Application;
using SoftballConnect.Input;
using SoftballConnect.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SoftballConnect.Tests
{
    public sealed class FirstPersonViewTests
    {
        const float Step = 1f / 120;
        sealed class TrackedHead : IHeadPoseSource
        {
            public Pose Pose;
            public Pose SampleHeadPose(float deltaTime) => Pose;
        }
        static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene); yield return null;
        }
        [UnityTest]
        public IEnumerator BootUsesEnglishAndJapaneseChoiceSurvivesMenuAndGame()
        {
            yield return Load("Bootstrap");
            for (int i = 0; i < 180 && SceneManager.GetActiveScene().name != "MainMenu"; i++) yield return null;
            yield return null;
            var root = Object.FindAnyObjectByType<GameBootstrap>();
            Assert.That(root.Japanese, Is.False);
            Assert.That(GameObject.Find("PlayerUI/FeedbackCard/Primary").GetComponentInChildren<Text>().text, Does.Contain("PLAY BALL"));
            root.ToggleLanguage(); Assert.That(root.Japanese, Is.True);
            root.Primary(); yield return null; yield return null;
            root = Object.FindAnyObjectByType<GameBootstrap>();
            Assert.That(root.Role, Is.EqualTo(SceneRole.StadiumPC)); Assert.That(root.Japanese, Is.True);
            root.Menu(); yield return null; yield return null;
            root = Object.FindAnyObjectByType<GameBootstrap>(); Assert.That(root.Japanese, Is.True);
            root.ToggleLanguage(); Assert.That(root.Japanese, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator CameraRemainsAtBatterThroughContactResultAndRetry()
        {
            yield return Load("Stadium_PC");
            var root = Object.FindAnyObjectByType<GameBootstrap>(); var game = root.Session;
            var rig = Object.FindAnyObjectByType<PlayerViewRig>();
            Vector3 initialEye = rig.PlayerCamera.transform.position;
            Quaternion initialRotation = rig.PlayerCamera.transform.rotation;
            Assert.That(Vector3.Distance(initialEye, root.Config.PlayerView.EyePosition), Is.LessThan(0.0001f));
            Assert.That(initialEye.y, Is.InRange(1.2f, 2.1f)); Assert.That(Mathf.Abs(initialEye.z), Is.LessThan(1.2f));
            Assert.That(GameObject.Find("Batter/ModelRoot/Head").GetComponent<Renderer>().enabled, Is.False);
            for (int i = 0; i < 1000 && game.State != GameState.BallInFlight; i++) game.Tick(Step);
            while (game.PitchProgress < 0.90f) game.Tick(Step);
            root.InputSource.QueueSwing();
            for (int i = 0; i < 300 && game.State != GameState.BallInPlay; i++) game.Tick(Step);
            Assert.That(game.State, Is.EqualTo(GameState.BallInPlay)); yield return null;
            Assert.That(Vector3.Distance(initialEye, rig.PlayerCamera.transform.position), Is.LessThan(0.0001f));
            for (int i = 0; i < 3000 && game.State != GameState.Feedback; i++) game.Tick(Step);
            Assert.That(game.State, Is.EqualTo(GameState.Feedback)); yield return null;
            Assert.That(GameObject.Find("PlayerUI/BallTracker").activeInHierarchy, Is.True);
            Assert.That(Vector3.Distance(initialEye, rig.PlayerCamera.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(initialRotation, rig.PlayerCamera.transform.rotation), Is.LessThan(0.001f));
            root.Restart(); yield return null;
            Assert.That(Vector3.Distance(initialEye, rig.PlayerCamera.transform.position), Is.LessThan(0.0001f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator ExternalHeadPoseContinuesUpdatingWhilePausedAndInFeedback()
        {
            yield return Load("Stadium_PC");
            var root = Object.FindAnyObjectByType<GameBootstrap>();
            var rig = Object.FindAnyObjectByType<PlayerViewRig>(); Vector3 origin = rig.Origin.position;
            var tracked = new TrackedHead { Pose = new Pose(new Vector3(0.03f, 1.73f, 0.02f), Quaternion.Euler(3, 12, 0)) };
            rig.SetHeadPoseSource(tracked); root.TogglePause();
            tracked.Pose = new Pose(new Vector3(-0.04f, 1.80f, 0.08f), Quaternion.Euler(7, 30, 1));
            yield return null; yield return null;
            Assert.That(Vector3.Distance(rig.Head.localPosition, tracked.Pose.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(rig.Head.localRotation, tracked.Pose.rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(origin, rig.Origin.position), Is.LessThan(0.0001f));
            root.TogglePause();
            for (int i = 0; i < 2000 && root.Session.State != GameState.Feedback; i++) root.Session.Tick(Step);
            Assert.That(root.Session.State, Is.EqualTo(GameState.Feedback));
            tracked.Pose = new Pose(new Vector3(0.08f, 1.68f, -0.02f), Quaternion.Euler(-4, -20, 0));
            yield return null;
            Assert.That(Vector3.Distance(rig.Head.localPosition, tracked.Pose.position), Is.LessThan(0.0001f));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
