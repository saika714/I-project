using System.Collections.Generic;
using SoftballConnect.Application;
using SoftballConnect.Content;
using SoftballConnect.Domain;
using UnityEngine;

namespace SoftballConnect.Presentation
{
    public sealed class StadiumView : MonoBehaviour
    {
        Transform ball, batRig, batModel, landingMarker, ballShadow;
        CharacterView pitcher, batter, mascot;
        Camera gameCamera;
        public PlayerViewRig ViewRig { get; private set; }
        ArcadeArt art;
        TrailRenderer trail;
        LineRenderer batDebug, pitchDebug, strikeZone;
        LineRenderer baseArrow;
        readonly List<CharacterView> runners = new List<CharacterView>();
        readonly List<LineRenderer> sparks = new List<LineRenderer>();
        readonly Queue<Vector3> history = new Queue<Vector3>();
        SimulationSettings settings;
        GameSessionController session;
        bool debug;
        float impactAge = 2;
        Vector3 contactPoint;
        public static Vector3 ToUnity(Vec3 value) => new Vector3(value.X, value.Y, value.Z);

        public static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>(); collider.enabled = false;
            if (UnityEngine.Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            return go;
        }

        public void Initialize(GameConfig config, GameSessionController game)
        {
            settings = config.CreateSettings(); session = game;
            art = gameObject.AddComponent<ArcadeArt>();
            var theme = config.Visuals;
            art.Initialize(theme);
            var root = art.Group("StadiumRoot", transform, Vector3.zero);
            var geometry = art.Group("FieldGeometry", root, Vector3.zero);
            art.Group("Home", geometry, Vector3.zero);
            art.Group("PitchRelease", geometry, new Vector3(0, 0.75f, settings.PitchDistance));
            var visuals = art.Group("StadiumVisuals", root, Vector3.zero);
            gameObject.AddComponent<StadiumEnvironment>().Build(visuals, art, theme, settings);

            pitcher = Character("Pitcher", new Vector3(0, 0, settings.PitchDistance + 0.35f), theme.AwayTeam, 180);
            batter = Character("Batter", new Vector3(-1.22f, 0, -0.12f), theme.HomeTeam, 35, true);
            if (session != null) batter.SetFirstPerson(true);
            if (session != null && session.Mode == GameMode.Match)
                for (int i = 0; i < 4; i++) runners.Add(Character("Runner" + i, Vector3.zero, theme.HomeTeam, 45, true));
            foreach (var position in new[] { new Vector3(13.4f, 0, 14), new Vector3(-13.4f, 0, 14),
                new Vector3(7, 0, 24), new Vector3(-7, 0, 24), new Vector3(0, 0, 38),
                new Vector3(-24, 0, 31), new Vector3(24, 0, 31) })
                Character("Fielder", position, theme.AwayTeam, 180);
            batRig = art.Group("BatRig", transform, Vector3.zero);
            batModel = art.Group("BatVisual", batRig, Vector3.zero);
            art.Shape("Barrel", PrimitiveType.Cylinder, batModel, new Vector3(0, 0.25f, 0), new Vector3(1, 0.75f, 1), new Color(0.57f, 0.62f, 0.65f));
            art.Shape("Grip", PrimitiveType.Cylinder, batModel, new Vector3(0, -0.75f, 0), new Vector3(0.65f, 0.25f, 0.65f), ArcadeArt.Ink);
            art.Orb("BatCap", batModel, Vector3.up, new Vector3(1, 0.10f, 1), new Color(0.24f, 0.33f, 0.41f));
            ball = art.Orb("BallView", transform, Vector3.zero, Vector3.one * settings.BallRadius * 2, new Color(1, 0.91f, 0.23f));
            trail = ball.gameObject.AddComponent<TrailRenderer>(); trail.sharedMaterial = art.Glow;
            trail.time = 0.32f; trail.startWidth = 0.095f; trail.endWidth = 0.012f;
            trail.startColor = new Color(1, 0.96f, 0.62f); trail.endColor = new Color(1, 0.73f, 0.25f, 0);
            trail.minVertexDistance = 0.04f; trail.emitting = false;
            ballShadow = art.Disc("BallShadow", transform, Vector3.zero, 0.13f, 0.005f, theme.Fence * 0.65f);
            landingMarker = art.Group("LandingMarker", transform, Vector3.zero);
            art.Arc("LandingRing", landingMarker, 0.64f, 0.73f, 0, 360, 0.025f, new Color(1, 0.81f, 0.22f));
            ConfigureCamera(config);
            baseArrow = Line("NextBaseArrow", new Color(0.25f, 1, 0.65f), 0.14f);
            baseArrow.positionCount = 5; baseArrow.enabled = false;
            batDebug = Line("BatSweepDebug", Color.yellow, 0.012f);
            pitchDebug = Line("PitchPathDebug", Color.white, 0.012f);
            var pitch = new PitchService(settings); pitchDebug.positionCount = 65;
            for (int i = 0; i < 65; i++) pitchDebug.SetPosition(i, ToUnity(pitch.PositionAt(settings.PitchDuration * i / 64)));
            strikeZone = Line("MeetGuide", new Color(0.94f, 1, 0.80f, 0.8f), 0.016f);
            strikeZone.positionCount = 5;
            strikeZone.SetPositions(new[] { new Vector3(-0.28f, 0.65f, 0.08f), new Vector3(-0.28f, 1.37f, 0.08f),
                new Vector3(0.28f, 1.37f, 0.08f), new Vector3(0.28f, 0.65f, 0.08f), new Vector3(-0.28f, 0.65f, 0.08f) });
            for (int i = 0; i < 10; i++)
            {
                var spark = Line("ContactSpark", new Color(1, 0.91f, 0.28f), 0.045f);
                spark.positionCount = 2; spark.enabled = false; sparks.Add(spark);
            }
            if (session != null)
            {
                session.StateChanged += OnStateChanged; session.BallContacted += OnContact;
                gameObject.AddComponent<ArcadeSound>().Initialize(session, theme.EffectsVolume, config.ScoreMusic, config.ScoreMusicVolume);
            }
            else
            {
                mascot = Character("MenuMascot", new Vector3(5.2f, 0, -0.2f), theme.HomeTeam, 170, true);
                art.Segment("MascotBat", mascot.transform, new Vector3(0.45f, 0.64f, 0.15f), new Vector3(0.65f, 1.7f, 0.15f), 0.055f, new Color(1, 0.73f, 0.27f));
                mascot.transform.localScale = Vector3.one;
            }
            Present(0);
        }

        void ConfigureCamera(GameConfig config)
        {
            var theme = config.Visuals;
            if (session != null)
            {
                ViewRig = new GameObject("PlayerViewRig").AddComponent<PlayerViewRig>();
                ViewRig.transform.SetParent(transform, false);
                ViewRig.Initialize(config.PlayerView, new Vector3(0, 1.0f, settings.PitchDistance * 0.4f));
                gameCamera = ViewRig.PlayerCamera;
            }
            else
            {
                var cameraObject = new GameObject("Menu Camera"); cameraObject.transform.SetParent(transform, false);
                gameCamera = cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
                cameraObject.tag = "MainCamera"; gameCamera.fieldOfView = 54;
                gameCamera.nearClipPlane = 0.06f; gameCamera.farClipPlane = 250;
                gameCamera.transform.position = new Vector3(14, 6, -8);
            }
            gameCamera.clearFlags = theme.SkyMaterial != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = theme.Sky; RenderSettings.skybox = theme.SkyMaterial;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional; sun.intensity = 1.25f; sun.color = new Color(1, 0.97f, 0.91f);
            sun.transform.rotation = Quaternion.Euler(42, -28, 0); sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.54f, 0.58f, 0.62f);
            RenderSettings.fog = true; RenderSettings.fogColor = theme.Sky;
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 95; RenderSettings.fogEndDistance = 230;
        }

        CharacterView Character(string name, Vector3 position, Color team, float yaw, bool helmet = false)
        {
            var go = art.Group(name, transform, position); go.localRotation = Quaternion.Euler(0, yaw, 0);
            var view = go.gameObject.AddComponent<CharacterView>(); view.Initialize(art, team, helmet); return view;
        }
        LineRenderer Line(string name, Color color, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(transform, false);
            line.sharedMaterial = art.Glow; line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color; line.numCapVertices = 3; return line;
        }
        void OnStateChanged(GameState state)
        {
            if (state == GameState.Prepare)
            {
                trail.Clear(); history.Clear(); impactAge = 2;
            }
        }
        void OnContact(BatContact contact) { contactPoint = ToUnity(contact.ContactPoint); impactAge = 0; }
        public void SetDebug(bool enabled) { debug = enabled; }
        public void Present(float dt)
        {
            if (session == null)
            {
                ball.gameObject.SetActive(false); ballShadow.gameObject.SetActive(false); batRig.gameObject.SetActive(false);
                landingMarker.gameObject.SetActive(false); batDebug.enabled = pitchDebug.enabled = strikeZone.enabled = false;
                if (mascot != null) mascot.PresentIdle(dt);
                gameCamera.transform.LookAt(new Vector3(0, 1.3f, 15)); return;
            }
            ball.gameObject.SetActive(session.BallVisible);
            ball.position = ToUnity(session.BallPosition);
            trail.emitting = (session.State == GameState.BallInPlay || session.State == GameState.BallInFlight) && !session.Paused;
            ballShadow.gameObject.SetActive(session.BallVisible);
            ballShadow.position = new Vector3(ball.position.x, 0.042f, ball.position.z);
            float shadowScale = Mathf.Lerp(1, 0.4f, ball.position.y / 15);
            ballShadow.localScale = new Vector3(0.26f * shadowScale, 0.0025f, 0.26f * shadowScale);
            var grip = ToUnity(session.BatPose.Grip); var tip = ToUnity(session.BatPose.Tip);
            batRig.position = grip; batModel.position = (grip + tip) * 0.5f; batModel.up = (tip - grip).normalized;
            batModel.localScale = new Vector3(settings.BatRadius * 2, settings.BatLength * 0.5f, settings.BatRadius * 2);
            pitcher.PresentPitch(session.WindupProgress, session.State == GameState.PitchWindup);
            batter.PresentBat(grip, tip, session.HasSwung);
            PresentRunning(dt);
            bool result = session.State == GameState.Feedback && session.LastResult != BallClassification.Miss;
            landingMarker.gameObject.SetActive(result); landingMarker.position = new Vector3(ball.position.x, 0.04f, ball.position.z);
            strikeZone.enabled = session.State == GameState.BallInFlight && !session.HasSwung;
            batDebug.enabled = pitchDebug.enabled = debug;
            if (debug)
            {
                var pitch = new PitchService(settings);
                for (int i = 0; i < 65; i++) pitchDebug.SetPosition(i, ToUnity(pitch.PositionAt(settings.PitchDuration * i / 64, session.CurrentPitch)));
            }
            if (debug && !session.Paused && session.State == GameState.BallInFlight)
            {
                history.Enqueue(grip); history.Enqueue(tip);
                while (history.Count > 160) { history.Dequeue(); history.Dequeue(); }
                batDebug.positionCount = history.Count; batDebug.SetPositions(history.ToArray());
            }
            ViewRig.Present(dt);
            if (!session.Paused) impactAge += dt;
            PresentImpact();
        }
        void PresentRunning(float dt)
        {
            bool running = session.State == GameState.BaseRunning;
            batRig.gameObject.SetActive(!running); batter.gameObject.SetActive(!running);
            for (int start = 0; start < runners.Count; start++)
            {
                var view = runners[start];
                bool visible = running ? session.RunningPlay.HasRunner(start) && !session.RunningPlay.RunnerScored(start) :
                    start > 0 && (session.OccupiedBases & (1 << (start - 1))) != 0;
                view.gameObject.SetActive(visible);
                if (!visible) continue;
                view.transform.position = ToUnity(running ? session.RunningPlay.Position(start) : BaseRunningPlay.BasePosition(settings.BaseDistance, start));
                int target = running ? Mathf.Min(4, start + session.RunningPlay.ReachedBase + 1) : start;
                Vector3 direction = ToUnity(BaseRunningPlay.BasePosition(settings.BaseDistance, target)) - view.transform.position;
                if (running && direction.sqrMagnitude > 0.001f) view.transform.rotation = Quaternion.LookRotation(direction);
                view.PresentRun(session.Paused || !session.RunRequested ? 0 : dt, running && session.RunRequested && !session.Paused);
            }
            bool preview = session.Mode == GameMode.Match && session.State == GameState.BallInPlay;
            baseArrow.enabled = !session.Paused && (running || preview);
            if (!baseArrow.enabled) return;
            Vector3 from = running ? ToUnity(session.RunningPlay.Position(0)) : Vector3.zero;
            Vector3 to = ToUnity(BaseRunningPlay.BasePosition(settings.BaseDistance, running ? session.RunningPlay.NextBase : 1));
            from.y = to.y = 0.16f;
            Vector3 forward = (to - from).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward) * 0.65f;
            baseArrow.SetPositions(new[] { from, to, to - forward * 1.4f + side, to, to - forward * 1.4f - side });
        }
        void PresentImpact()
        {
            for (int i = 0; i < sparks.Count; i++)
            {
                var spark = sparks[i]; spark.enabled = session.State == GameState.BallInPlay && impactAge < 0.18f;
                if (!spark.enabled) continue;
                float t = Mathf.Clamp01(impactAge / 0.18f), angle = i * Mathf.PI * 2 / sparks.Count;
                Vector3 direction = gameCamera.transform.right * Mathf.Cos(angle) + gameCamera.transform.up * Mathf.Sin(angle);
                spark.SetPosition(0, contactPoint + direction * (0.025f + t * 0.07f));
                spark.SetPosition(1, contactPoint + direction * (0.065f + t * 0.16f));
                spark.startWidth = spark.endWidth = 0.009f * (1 - t);
            }
        }
        void OnDrawGizmos()
        {
            if (!debug || session == null) return;
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(ToUnity(session.BallPosition), settings.BallRadius);
            Gizmos.DrawWireSphere(ToUnity(session.LastContact.ContactPoint), 0.12f);
            Gizmos.DrawLine(ToUnity(session.LastContact.ContactPoint), ToUnity(session.LastContact.ContactPoint + session.LastContact.ContactNormal * 0.5f));
            Gizmos.DrawLine(ToUnity(session.BatPose.Grip), ToUnity(session.BatPose.Tip));
            Gizmos.DrawWireSphere(ToUnity(session.BatPose.Tip), settings.BatRadius + (session.Assist ? settings.AssistRadius : 0));
        }
        void OnDestroy()
        {
            if (session == null) return;
            session.StateChanged -= OnStateChanged; session.BallContacted -= OnContact;
        }
    }
}
