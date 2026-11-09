using SoftballConnect.Application;
using SoftballConnect.Content;
using SoftballConnect.Input;
using SoftballConnect.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SoftballConnect.Presentation
{
    public enum SceneRole { Bootstrap, MainMenu, StadiumPC }

    // Composition root: connects device input, pure application and replaceable views.
    public sealed class GameBootstrap : MonoBehaviour
    {
        public SceneRole Role;
        public GameConfig Config;
        public GameSessionController Session { get; private set; }
        public DesktopBatInputSource InputSource { get; private set; }
        public bool Japanese => localization != null && localization.Japanese;
        FeedbackPresenter feedback;
        StadiumView stadium;
        LocalizationService localization;
        float accumulator;
        bool debug;
        const float Step = 1f / 120;
        static bool japanese;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLanguageOnLaunch() { japanese = false; }

        void Start()
        {
            if (Role == SceneRole.Bootstrap) { japanese = false; SceneManager.LoadScene("MainMenu"); return; }
            if (Config == null || Config.DesktopActions == null || Config.Localization == null || Config.Visuals == null)
                throw new System.InvalidOperationException("GameBootstrap requires complete GameConfig references.");
            var settings = Config.CreateSettings(); settings.Validate();
            InputSource = gameObject.AddComponent<DesktopBatInputSource>(); InputSource.Initialize(Config.DesktopActions, settings);
            localization = new LocalizationService(Config.Localization); if (japanese) localization.Toggle();
            if (Role == SceneRole.StadiumPC)
            {
                Session = new GameSessionController(settings, InputSource);
            }
            stadium = new GameObject("GamePresentation").AddComponent<StadiumView>(); stadium.transform.SetParent(transform, false);
            stadium.Initialize(Config, Session);
            feedback = new GameObject("PlayerUI").AddComponent<FeedbackPresenter>(); feedback.transform.SetParent(transform, false);
            feedback.Initialize(localization, Session, Primary, Restart, Menu, TogglePause, ToggleLanguage, ToggleAssist, ToggleDebug);
            if (stadium.ViewRig != null) feedback.SetTrackingCamera(stadium.ViewRig.PlayerCamera);
        }
        void Update()
        {
            if (InputSource == null || feedback == null) return;
            if (InputSource.Pressed("Language")) ToggleLanguage();
            if (Session == null) { stadium.Present(Time.unscaledDeltaTime); if (InputSource.Pressed("Continue")) Primary(); return; }
            if (InputSource.Pressed("Menu")) { Menu(); return; }
            if (InputSource.Pressed("Restart")) { Restart(); return; }
            if (InputSource.Pressed("Pause")) TogglePause();
            if (InputSource.Pressed("Debug")) ToggleDebug();
            if (InputSource.Pressed("Assist")) ToggleAssist();
            if (InputSource.Pressed("Continue")) Primary();
            bool pointerSwing = InputSource.Pressed("PointerSwing") &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            if (Session.CanSwing && (InputSource.Pressed("Swing") || pointerSwing)) InputSource.QueueSwing();
            accumulator += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            while (accumulator >= Step) { Session.Tick(Step); accumulator -= Step; }
            stadium.Present(Time.unscaledDeltaTime); feedback.Present();
        }
        public void Primary()
        {
            if (Session == null) SceneManager.LoadScene("Stadium_PC");
            else if (Session.State == GameState.RoundResult) Restart();
            else Session.Next();
        }
        public void Restart() { if (Session == null) return; Session.Restart(); accumulator = 0; feedback.Present(true); }
        public void Menu() { SceneManager.LoadScene("MainMenu"); }
        public void TogglePause() { if (Session != null) { Session.SetPaused(!Session.Paused); feedback.Present(true); } }
        public void ToggleLanguage() { localization.Toggle(); japanese = localization.Japanese; feedback.Present(true); }
        public void ToggleAssist()
        {
            if (Session == null || (Session.State != GameState.Prepare && Session.State != GameState.Feedback && Session.State != GameState.RoundResult)) return;
            Session.Assist = !Session.Assist; feedback.Present(true);
        }
        public void ToggleDebug() { debug = !debug; stadium?.SetDebug(debug); feedback.SetDebug(debug); }
    }
}
