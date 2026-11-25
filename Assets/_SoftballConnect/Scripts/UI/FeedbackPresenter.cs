using System;
using System.Collections.Generic;
using SoftballConnect.Application;
using SoftballConnect.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SoftballConnect.UI
{
    public sealed class FeedbackPresenter : MonoBehaviour
    {
        static readonly Color Ink = new Color(0.06f, 0.16f, 0.26f);
        static readonly Color Cream = new Color(1, 0.98f, 0.91f);
        static readonly Color Coral = new Color(1, 0.35f, 0.26f);
        static readonly Color Teal = new Color(0.02f, 0.48f, 0.43f);
        static readonly Color Gold = new Color(1, 0.80f, 0.24f);
        LocalizationService text;
        GameSessionController session;
        Font font;
        Texture2D roundedTexture;
        Sprite rounded;
        Text title, status, instruction, result, hint, prompt, metric, eyebrow, impact, timingCaption;
        Button primary, restart, menu, pause, language, assist, debug;
        Button match, pitchType, run;
        Text modeLabel, pitchLabel, scoreLabel, nextBaseLabel;
        RectTransform diamond;
        RectTransform routeArrow;
        readonly List<Image> baseIcons = new List<Image>();
        readonly List<RectTransform> runnerIcons = new List<RectTransform>();
        RectTransform card, bar, promptPanel;
        Image progress;
        Camera trackingCamera;
        RectTransform ballTracker;
        Text ballTrackingLabel;
        readonly List<Image> ballPips = new List<Image>();
        readonly Dictionary<int, bool> results = new Dictionary<int, bool>();
        bool isMenu, debugEnabled;
        int lastRevision = -1;
        float impactAge = 2;

        public void Initialize(LocalizationService localization, GameSessionController game,
            Action startOrNext, Action onRestart, Action onMenu, Action onPause, Action onLanguage,
            Action onAssist, Action onDebug, Action onMatch = null,
            Action onRun = null, Action onPitch = null)
        {
            text = localization; session = game; isMenu = game == null;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Hiragino Sans", "Yu Gothic", "Noto Sans CJK JP", "Arial" }, 32);
            CreateRoundedSprite();
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            if (isMenu)
            {
                var tag = Panel("ClubTag", transform, new Vector2(0.055f, 0.895f), new Vector2(0.30f, 0.955f), Ink);
                Label("ClubName", tag, new Vector2(0.08f, 0), new Vector2(0.92f, 1), 19, TextAnchor.MiddleLeft, Cream).text = "SOFTBALL CONNECT";
            }
            else
            {
                var board = Panel("Scoreboard", transform, new Vector2(0.035f, 0.80f), new Vector2(0.325f, 0.955f), Ink);
                title = Label("Title", board, new Vector2(0.08f, 0.67f), new Vector2(0.92f, 0.95f), 17, TextAnchor.MiddleLeft, Gold);
                bool isMatch = session.Mode == GameMode.Match;
                status = Label("Status", board, new Vector2(0.08f, isMatch ? 0.40f : 0.30f), new Vector2(0.94f, 0.72f), isMatch ? 21 : 25, TextAnchor.MiddleLeft, Cream);
                int count = session.BallsPerRound;
                for (int i = 0; i < count; i++)
                {
                    float left = 0.08f + i * 0.84f / count;
                    var pip = Panel("Ball" + (i + 1), board, new Vector2(left, isMatch ? 0.33f : 0.13f), new Vector2(left + 0.70f / count, isMatch ? 0.39f : 0.23f), new Color(0.27f, 0.37f, 0.43f));
                    ballPips.Add(pip.GetComponent<Image>());
                }
                var badge = Panel("ModeBadge", transform, new Vector2(0.77f, 0.91f), new Vector2(0.965f, 0.955f), Cream);
                modeLabel = Label("Brand", badge, new Vector2(0.04f, 0), new Vector2(0.96f, 1), 16, TextAnchor.MiddleCenter, Ink);
                var pitchBadge = Panel("PitchBadge", transform, new Vector2(0.77f, 0.85f), new Vector2(0.965f, 0.90f), Ink);
                pitchLabel = Label("PitchType", pitchBadge, new Vector2(0.02f, 0), new Vector2(0.98f, 1), 19, TextAnchor.MiddleCenter, Gold);
                pitchType = MakeButton("SelectPitch", transform, new Vector2(0.77f, 0.79f), new Vector2(0.965f, 0.84f), onPitch, Cream, Ink, 17);
                if (isMatch)
                {
                    scoreLabel = Label("Score", board, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.31f), 23, TextAnchor.MiddleLeft, Gold);
                    CreateDiamond(onRun);
                }
            }
            var footer = Panel("Footer", transform, new Vector2(0.03f, 0.025f), new Vector2(0.97f, 0.088f), new Color(1, 0.98f, 0.91f, 0.96f));
            restart = MakeButton("Restart", footer, new Vector2(0.005f, 0.06f), new Vector2(0.16f, 0.94f), onRestart, Cream, Ink, 18);
            menu = MakeButton("Menu", footer, new Vector2(0.165f, 0.06f), new Vector2(0.30f, 0.94f), onMenu, Cream, Ink, 18);
            pause = MakeButton("Pause", footer, new Vector2(0.305f, 0.06f), new Vector2(0.48f, 0.94f), onPause, Cream, Ink, 18);
            assist = MakeButton("Assist", footer, new Vector2(0.485f, 0.06f), new Vector2(0.70f, 0.94f), onAssist, Cream, Teal, 18);
            debug = MakeButton("Debug", footer, new Vector2(0.70f, 0.06f), new Vector2(0.83f, 0.94f), onDebug, Cream, Ink, 16);
            language = MakeButton("Language", footer, new Vector2(0.835f, 0.06f), new Vector2(0.995f, 0.94f), onLanguage, Ink, Cream, 18);
            hint = Label("Hint", isMenu ? footer : transform, isMenu ? new Vector2(0.03f, 0) : new Vector2(0.2f, 0.095f),
                isMenu ? new Vector2(0.79f, 1) : new Vector2(0.8f, 0.13f), 17, TextAnchor.MiddleCenter, isMenu ? Ink : Cream);
            if (!isMenu) AddTextShadow(hint);

            card = Panel("FeedbackCard", transform, isMenu ? new Vector2(0.055f, 0.17f) : new Vector2(0.66f, 0.32f),
                isMenu ? new Vector2(0.46f, 0.85f) : new Vector2(0.965f, 0.73f), Cream);
            var shadow = card.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.04f, 0.16f, 0.23f, 0.18f); shadow.effectDistance = new Vector2(0, -9);
            eyebrow = Label("Eyebrow", card, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f), 17, TextAnchor.MiddleLeft, Coral);
            if (isMenu)
            {
                title = Label("Title", card, new Vector2(0.075f, 0.59f), new Vector2(0.95f, 0.87f), 65, TextAnchor.MiddleLeft, Ink);
                title.lineSpacing = 0.9f;
            }
            instruction = Label("Instruction", card, isMenu ? new Vector2(0.08f, 0.45f) : new Vector2(0.08f, 0.66f),
                isMenu ? new Vector2(0.92f, 0.59f) : new Vector2(0.94f, 0.86f), isMenu ? 27 : 33, TextAnchor.MiddleLeft, Ink);
            metric = Label("Metric", card, new Vector2(0.08f, 0.35f), new Vector2(0.94f, 0.67f), 64, TextAnchor.MiddleLeft, Teal);
            result = Label("Result", card, isMenu ? new Vector2(0.08f, 0.23f) : new Vector2(0.08f, 0.20f),
                isMenu ? new Vector2(0.92f, 0.45f) : new Vector2(0.92f, 0.36f), isMenu ? 21 : 18, TextAnchor.MiddleLeft, Ink);
            primary = MakeButton("Primary", card, new Vector2(0.08f, 0.065f), new Vector2(0.92f, isMenu ? 0.195f : 0.18f), startOrNext, Coral, Color.white, isMenu ? 26 : 22);
            if (isMenu)
            {
                // Keep Primary as the original practice entry for keyboard and existing integrations.
                Place(primary.GetComponent<RectTransform>(), new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.16f));
                match = MakeButton("Match", card, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.29f), onMatch, Teal, Color.white, 24);
                Place(result.rectTransform, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.45f));
                result.fontSize = 18;
            }
            promptPanel = Panel("PitchPromptPanel", transform, new Vector2(0.355f, 0.84f), new Vector2(0.68f, 0.91f), Ink);
            prompt = Label("PitchPrompt", promptPanel, new Vector2(0.03f, 0), new Vector2(0.97f, 1), 24, TextAnchor.MiddleCenter, Cream);
            bar = Panel("PitchTimingBar", transform, new Vector2(0.68f, 0.15f), new Vector2(0.965f, 0.215f), Ink);
            timingCaption = Label("TimingCaption", bar, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.98f), 13, TextAnchor.MiddleLeft, Cream);
            var track = Panel("Track", bar, new Vector2(0.05f, 0.19f), new Vector2(0.95f, 0.48f), new Color(0.24f, 0.36f, 0.42f));
            Panel("SwingIntentZone", track, new Vector2(0.84f, -0.14f), new Vector2(0.95f, 1.14f), Gold);
            progress = Panel("PitchProgress", track, new Vector2(0, -0.30f), new Vector2(0.025f, 1.30f), Color.white).GetComponent<Image>();
            impact = Label("ContactCallout", transform, new Vector2(0.37f, 0.74f), new Vector2(0.66f, 0.80f), 30, TextAnchor.MiddleCenter, Gold);
            AddTextShadow(impact);
            ballTracker = Panel("BallTracker", transform, new Vector2(0.4f, 0.7f), new Vector2(0.6f, 0.76f), Ink);
            ballTrackingLabel = Label("Direction", ballTracker, new Vector2(0.02f, 0), new Vector2(0.98f, 1), 17, TextAnchor.MiddleCenter, Cream);
            ballTracker.gameObject.SetActive(false);
            if (session != null) { session.PlayResolved += RememberResult; session.BallContacted += Contact; }
            Present(true);
        }

        static Vector2 DiamondPoint(float progress)
        {
            var position = BaseRunningPlay.BasePosition(1, (int)progress);
            var next = BaseRunningPlay.BasePosition(1, (int)progress + 1);
            var point = Vec3.Lerp(position, next, progress - (int)progress);
            return new Vector2(0.5f + point.X * 0.424264f, 0.25f + point.Z * 0.353553f);
        }
        void CreateDiamond(Action onRun)
        {
            diamond = Panel("BaseDiamond", transform, new Vector2(0.035f, 0.16f), new Vector2(0.245f, 0.47f), Ink);
            nextBaseLabel = Label("NextBase", diamond, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.98f), 18, TextAnchor.MiddleCenter, Gold);
            for (int i = 0; i < 4; i++)
            {
                Vector2 point = DiamondPoint(i);
                var icon = Panel("Base" + i, diamond, point - new Vector2(0.06f, 0.055f), point + new Vector2(0.06f, 0.055f), Cream);
                baseIcons.Add(icon.GetComponent<Image>());
                Label("Number", icon, Vector2.zero, Vector2.one, 15, TextAnchor.MiddleCenter, Ink).text = i == 0 ? "H" : i.ToString();
            }
            for (int i = 0; i < 4; i++) runnerIcons.Add(Panel("RunnerDot" + i, diamond, Vector2.zero, Vector2.one * 0.03f, Coral));
            routeArrow = Label("RouteArrow", diamond, Vector2.zero, Vector2.zero, 32, TextAnchor.MiddleCenter,
                new Color(0.25f, 1, 0.65f)).rectTransform;
            routeArrow.GetComponent<Text>().text = "→";
            run = MakeButton("Run", diamond, new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.16f), onRun, Teal, Color.white, 17);
        }
        void PresentMatch()
        {
            if (isMenu) return;
            modeLabel.text = text.Get(session.Mode == GameMode.Match ? "matchMode" : "practiceMode");
            pitchLabel.text = text.Get(session.CurrentPitch.ToString());
            SetLabel(pitchType, string.Format(text.Get("selectPitchValue"), text.Get("pitchName" + session.PracticePitch)));
            pitchType.gameObject.SetActive(session.Mode == GameMode.Practice);
            pitchType.interactable = !session.Paused && (session.State == GameState.Prepare || session.State == GameState.Feedback || session.State == GameState.RoundResult);
            if (diamond == null) return;
            scoreLabel.text = string.Format(text.Get("scoreValue"), session.Score);
            bool running = session.State == GameState.BaseRunning;
            bool guiding = running || session.State == GameState.BallInPlay;
            int next = running ? session.RunningPlay.NextBase : 1;
            nextBaseLabel.text = guiding ? string.Format(text.Get("nextBaseValue"), text.Get("base" + next)) : text.Get("baseRunners");
            routeArrow.gameObject.SetActive(guiding && !session.Paused);
            if (guiding)
            {
                Vector2 from = DiamondPoint(running ? session.RunningPlay.Progress : 0), to = DiamondPoint(next);
                Vector2 middle = (from + to) * 0.5f;
                Place(routeArrow, middle, middle); routeArrow.sizeDelta = new Vector2(48, 40);
                Vector2 direction = to - from;
                routeArrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y * diamond.rect.height, direction.x * diamond.rect.width) * Mathf.Rad2Deg);
            }
            run.gameObject.SetActive(running); run.interactable = !session.Paused;
            SetLabel(run, text.Get(session.RunRequested ? "stopRunning" : "startRunning"));
            for (int i = 0; i < 4; i++)
            {
                baseIcons[i].color = running && i == next % 4 ? new Color(0.25f, 1, 0.65f) :
                    i > 0 && (session.OccupiedBases & (1 << (i - 1))) != 0 ? Gold : Cream;
                bool show = running && session.RunningPlay.HasRunner(i) && !session.RunningPlay.RunnerScored(i);
                runnerIcons[i].gameObject.SetActive(show);
                if (show)
                {
                    Vector2 point = DiamondPoint(i + session.RunningPlay.Progress) + new Vector2(0, 0.07f);
                    Place(runnerIcons[i], point - Vector2.one * 0.02f, point + Vector2.one * 0.02f);
                }
            }
        }

        public void SetTrackingCamera(Camera camera) { trackingCamera = camera; }
        void PresentBallTracking()
        {
            bool visible = trackingCamera != null && session != null && !session.Paused &&
                (session.State == GameState.BallInPlay || (session.State == GameState.Feedback && session.LastResult != BallClassification.Miss));
            ballTracker.gameObject.SetActive(visible);
            if (!visible) return;
            Vector3 ball = SoftballConnect.Presentation.StadiumView.ToUnity(session.BallPosition);
            Vector3 point = trackingCamera.WorldToViewportPoint(ball);
            Vector3 localDirection = trackingCamera.transform.InverseTransformDirection(ball - trackingCamera.transform.position);
            string key = point.z <= 0 || point.x < 0.08f || point.x > 0.92f ? (localDirection.x >= 0 ? "ballRight" : "ballLeft") :
                point.y > 0.82f ? "ballAbove" : point.y < 0.20f ? "ballBelow" : "ballTracked";
            float x = Mathf.Clamp(point.x, 0.16f, 0.58f), y = Mathf.Clamp(point.y + 0.05f, 0.28f, 0.76f);
            if (point.z <= 0) x = localDirection.x >= 0 ? 0.58f : 0.16f;
            Place(ballTracker, new Vector2(x - 0.10f, y), new Vector2(x + 0.10f, y + 0.045f));
            float metres = new Vector2(ball.x, ball.z).magnitude;
            ballTrackingLabel.text = string.Format(text.Get(key), metres);
        }

        void CreateRoundedSprite()
        {
            const int size = 64;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            roundedTexture.name = "Arcade UI corners"; roundedTexture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 14, 0), dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 14, 0);
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(18 - Mathf.Sqrt(dx * dx + dy * dy)));
                }
            roundedTexture.SetPixels(pixels); roundedTexture.Apply();
            rounded = Sprite.Create(roundedTexture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 22);
        }
        RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; Place(rect, min, max);
            var image = go.GetComponent<Image>(); image.color = color; image.sprite = rounded; image.type = Image.Type.Sliced;
            image.raycastTarget = false; return rect;
        }
        static void Place(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        Text Label(string name, Transform parent, Vector2 min, Vector2 max, int size, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, min, max);
            var label = go.GetComponent<Text>(); label.font = font; label.fontSize = size; label.fontStyle = FontStyle.Bold;
            label.alignment = alignment; label.color = color; label.raycastTarget = false; label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
        static void AddTextShadow(Text label)
        {
            var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.04f, 0.12f, 0.20f, 0.65f);
            shadow.effectDistance = new Vector2(2, -3);
        }
        Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, Action action, Color background, Color foreground, int size)
        {
            var rect = Panel(name, parent, min, max, background); var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.highlightedColor = new Color(1, 0.92f, 0.72f); colors.pressedColor = new Color(0.75f, 0.84f, 0.88f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.7f); button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            Label("Label", rect, new Vector2(0.025f, 0), new Vector2(0.975f, 1), size, TextAnchor.MiddleCenter, foreground);
            return button;
        }
        static void SetLabel(Button button, string value) => button.GetComponentInChildren<Text>().text = value;
        void RememberResult(BallClassification value) { results[session.BallNumber] = value != BallClassification.Miss; }
        void Contact(BatContact _) { impactAge = 0; }
        public void SetDebug(bool value) { debugEnabled = value; Present(true); }

        public void Present(bool force = false)
        {
            PresentMatch();
            PresentBallTracking();
            bool active = !isMenu && !session.Paused;
            bar.gameObject.SetActive(active && session.State == GameState.BallInFlight);
            promptPanel.gameObject.SetActive(active && session.State != GameState.Feedback && session.State != GameState.RoundResult);
            if (!isMenu)
            {
                if (!session.Paused) impactAge += Time.unscaledDeltaTime;
                bool sweetWindow = session.PitchProgress >= 0.84f && session.PitchProgress <= 0.95f && !session.HasSwung;
                string key = session.State == GameState.Prepare ? "prepare" : session.State == GameState.PitchWindup ? "windup" :
                    session.State == GameState.BaseRunning ? "runHint" : session.State == GameState.BallInPlay ? "followBall" : sweetWindow ? "swingNow" : "watchBall";
                prompt.text = text.Get(key); promptPanel.GetComponent<Image>().color = sweetWindow ? Coral : Ink;
                float p = Mathf.Clamp01(session.PitchProgress);
                progress.rectTransform.anchorMin = new Vector2(p * 0.975f, -0.30f);
                progress.rectTransform.anchorMax = new Vector2(p * 0.975f + 0.025f, 1.30f);
                impact.gameObject.SetActive(active && impactAge < 0.7f && session.State == GameState.BallInPlay);
                impact.text = text.Get("niceContact");
                impact.transform.localScale = Vector3.one * (1 + Mathf.Exp(-impactAge * 14) * 0.30f);
                impact.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Clamp01((0.7f - impactAge) * 5));
                if (session.BallNumber == 0) results.Clear();
                for (int i = 0; i < ballPips.Count; i++)
                    ballPips[i].color = results.TryGetValue(i + 1, out var hit) ? (hit ? Gold : new Color(0.52f, 0.60f, 0.64f)) :
                        i + 1 == session.BallNumber ? Cream : new Color(0.27f, 0.37f, 0.43f);
            }
            else impact.gameObject.SetActive(false);
            int revision = (isMenu ? 0 : (int)session.State + session.BallNumber * 10 + session.Contacts * 1000 +
                (session.Paused ? 100000 : 0) + (session.Assist ? 200000 : 0)) + (text.Japanese ? 400000 : 0) + (debugEnabled ? 800000 : 0);
            if (!force && revision == lastRevision) return;
            lastRevision = revision;
            SetLabel(language, text.Get("language")); SetLabel(restart, text.Get("restart")); SetLabel(menu, text.Get("menu"));
            SetLabel(pause, text.Get(!isMenu && session.Paused ? "resume" : "pause"));
            SetLabel(assist, text.Get(!isMenu && !session.Assist ? "assistOff" : "assistOn"));
            SetLabel(debug, text.Get(debugEnabled ? "debugOn" : "debugOff"));
            foreach (var button in new[] { restart, menu, pause, assist }) button.gameObject.SetActive(!isMenu);
            debug.gameObject.SetActive(!isMenu && debugEnabled);
            timingCaption.text = text.Get("timingGuide");
            if (isMenu)
            {
                title.text = text.Get("menuTitle"); eyebrow.text = text.Get("clubTag");
                SetLabel(match, text.Get("playMatch"));
                instruction.text = text.Get("welcome"); result.text = text.Get("tutorial"); metric.gameObject.SetActive(false);
                hint.text = text.Get("menuHint"); SetLabel(primary, text.Get("start")); return;
            }
            assist.interactable = session.State == GameState.Prepare || session.State == GameState.Feedback || session.State == GameState.RoundResult;
            title.text = text.Get(session.Mode == GameMode.Match ? "matchMode" : "challenge");
            status.text = string.Format(text.Get("status"), Mathf.Max(1, session.BallNumber), session.BallsPerRound, session.Contacts);
            hint.text = debugEnabled ? session.State + " | " + text.Get("debugHint") : text.Get(session.State == GameState.BaseRunning ? "runControls" : "controls");
            bool feedback = session.State == GameState.Feedback, round = session.State == GameState.RoundResult;
            card.gameObject.SetActive(feedback || round || session.Paused); primary.gameObject.SetActive(!session.Paused);
            Place(card, round || session.Paused ? new Vector2(0.30f, 0.29f) : new Vector2(0.66f, 0.32f),
                round || session.Paused ? new Vector2(0.70f, 0.75f) : new Vector2(0.965f, 0.73f));
            metric.gameObject.SetActive(!session.Paused); eyebrow.text = text.Get(round ? "sessionTag" : "resultTag");
            Place(result.rectTransform, session.Paused ? new Vector2(0.08f, 0.24f) : new Vector2(0.08f, 0.20f),
                session.Paused ? new Vector2(0.92f, 0.65f) : new Vector2(0.92f, 0.36f));
            if (session.Paused) { instruction.text = text.Get("paused"); result.text = text.Get("pauseHint"); }
            else if (round)
            {
                instruction.text = text.Get("roundComplete");
                metric.text = session.Mode == GameMode.Match ? string.Format(text.Get("scoreValue"), session.Score) : string.Format(text.Get("contactScore"), session.Contacts, session.BallsPerRound);
                result.text = text.Get("roundEncouragement"); SetLabel(primary, text.Get("retry"));
            }
            else if (feedback)
            {
                instruction.text = text.Get(session.LastResult.ToString());
                metric.text = session.LastResult == BallClassification.Miss ? text.Get("nextChance") : string.Format(text.Get("distanceValue"), session.LastDistance);
                metric.fontSize = session.LastResult == BallClassification.Miss ? 45 : 64;
                string timing = !session.HasSwung ? "noSwing" : session.LastTimingOffset < -0.07f ? "early" : session.LastTimingOffset > 0.07f ? "late" : "onTime";
                result.text = session.Mode == GameMode.Match && session.AwardedBases > 0 ?
                    string.Format(text.Get("advanceResult"), text.Get("base" + session.AwardedBases), session.LastRuns) : text.Get(timing);
                SetLabel(primary, text.Get(session.BallNumber == session.BallsPerRound ? "showRound" : "next"));
            }
            if (round) metric.fontSize = 48;
        }
        void OnDestroy()
        {
            if (session != null) { session.PlayResolved -= RememberResult; session.BallContacted -= Contact; }
            if (font != null) Destroy(font); if (rounded != null) Destroy(rounded); if (roundedTexture != null) Destroy(roundedTexture);
        }
    }
}
