using System.Collections;
using System.IO;
using NUnit.Framework;
using SoftballConnect.Application;
using SoftballConnect.Input;
using SoftballConnect.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoftballConnect.Tests
{
    [Category("ReportCapture")]
    public sealed class ReportCaptureTests
    {
        const float Step = 1f / 120;
        sealed class FrozenHead : IHeadPoseSource
        {
            public Pose Pose;
            public Pose SampleHeadPose(float deltaTime) => Pose;
        }

        [UnityTest]
        public IEnumerator ExportStadiumPitchContactAndUi()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Report capture requires a graphics device; omit -nographics.");
            string output = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../Reports/Images"));
            yield return SceneManager.LoadSceneAsync("Stadium_PC"); yield return null;
            var root = Object.FindAnyObjectByType<GameBootstrap>(); root.enabled = false;
            var view = Object.FindAnyObjectByType<StadiumView>(); var rig = view.ViewRig;
            rig.SetHeadPoseSource(new FrozenHead { Pose = new Pose(rig.Head.localPosition, rig.Head.localRotation) });
            var canvas = Object.FindAnyObjectByType<Canvas>(); canvas.enabled = false;
            root.Session.Restart(); view.Present(0);
            yield return new WaitForSecondsRealtime(0.25f);

            // A dedicated overview is only used for the report; gameplay stays at the batter's eyes.
            var overview = new GameObject("ReportOverviewCamera").AddComponent<Camera>();
            overview.CopyFrom(rig.PlayerCamera); overview.enabled = false;
            overview.transform.position = new Vector3(0, 24, -28);
            overview.transform.LookAt(new Vector3(0, 2, 24)); overview.fieldOfView = 64;
            UnityFrameCapture.Save(overview, null, Path.Combine(output, "01_stadium.png"), 1920, 1080);
            Object.Destroy(overview.gameObject);

            var game = root.Session;
            for (int i = 0; i < 1500 && game.State != GameState.BallInFlight; i++)
            { game.Tick(Step); view.Present(Step); if (i % 12 == 0) yield return null; }
            Assert.That(game.State, Is.EqualTo(GameState.BallInFlight));
            int frame = 0;
            while (game.PitchProgress < 0.82f)
            { game.Tick(Step); view.Present(Step); if (++frame % 8 == 0) yield return null; }
            view.Present(0); yield return null;
            Assert.That(game.BallPosition.Z, Is.GreaterThan(0));
            UnityFrameCapture.Save(rig.PlayerCamera, null, Path.Combine(output, "02_pitch.png"), 1920, 1080);

            while (game.PitchProgress < 0.90f) { game.Tick(Step); view.Present(Step); }
            root.InputSource.QueueSwing();
            for (int i = 0; i < 300 && game.State == GameState.BallInFlight; i++)
            { game.Tick(Step); view.Present(Step); }
            Assert.That(game.State, Is.EqualTo(GameState.BallInPlay)); Assert.That(game.Contacts, Is.EqualTo(1));
            Vector3 contact = StadiumView.ToUnity(game.LastContact.ContactPoint);
            // Reproduce looking at contact by changing head rotation, never translating the player.
            var head = new FrozenHead { Pose = new Pose(rig.Head.localPosition,
                Quaternion.Inverse(rig.Origin.rotation) * Quaternion.LookRotation(contact - rig.PlayerCamera.transform.position)) };
            rig.SetHeadPoseSource(head); view.Present(0); yield return null;
            UnityFrameCapture.Save(rig.PlayerCamera, null, Path.Combine(output, "03_contact.png"), 1920, 1080);

            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 180 && SceneManager.GetActiveScene().name != "MainMenu"; i++) yield return null;
            yield return null; yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Object.FindAnyObjectByType<GameBootstrap>().Japanese, Is.False);
            UnityFrameCapture.Save(Object.FindAnyObjectByType<Camera>(), Object.FindAnyObjectByType<Canvas>(),
                Path.Combine(output, "04_ui.png"), 1920, 1080);
            foreach (string name in new[] { "01_stadium", "02_pitch", "03_contact", "04_ui" })
                Assert.That(new FileInfo(Path.Combine(output, name + ".png")).Length, Is.GreaterThan(10000));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
