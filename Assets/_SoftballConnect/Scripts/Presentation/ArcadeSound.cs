using SoftballConnect.Application;
using SoftballConnect.Domain;
using UnityEngine;

namespace SoftballConnect.Presentation
{
    // Short original synthesized cues. Clips are generated once and released with the scene.
    public sealed class ArcadeSound : MonoBehaviour
    {
        GameSessionController session;
        AudioSource source;
        AudioClip contact, success, miss, generatedMusic;
        public AudioSource MusicSource { get; private set; }
        public void Initialize(GameSessionController game, float volume, AudioClip scoreMusic = null, float musicVolume = 0.35f)
        {
            session = game;
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = Mathf.Clamp01(volume);
            contact = Cue("Bat contact", 0.13f, 740, 1180, true);
            success = Cue("Result chime", 0.40f, 523.25f, 783.99f, false);
            miss = Cue("Next chance", 0.19f, 330, 261.63f, false);
            MusicSource = gameObject.AddComponent<AudioSource>(); MusicSource.playOnAwake = false;
            MusicSource.volume = Mathf.Clamp01(musicVolume);
            if (scoreMusic == null) generatedMusic = Fanfare();
            MusicSource.clip = scoreMusic != null ? scoreMusic : generatedMusic;
            session.BallContacted += Contact; session.PlayResolved += Resolved;
            session.RunsScored += Scored; session.StateChanged += StateChanged;
        }
        static AudioClip Fanfare()
        {
            const int rate = 22050;
            const float beat = 0.22f;
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 783.99f, 1046.50f, 1318.51f, 1046.50f };
            var samples = new float[Mathf.CeilToInt(rate * beat * (notes.Length + 2))];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate; int note = Mathf.Min(notes.Length - 1, (int)(t / beat));
                float age = t - note * beat;
                float envelope = Mathf.Min(1, age * 100) * Mathf.Exp(-age * 5);
                samples[i] = (Mathf.Sin(2 * Mathf.PI * notes[note] * t) * 0.5f +
                    Mathf.Sin(2 * Mathf.PI * notes[note] * 0.5f * t) * 0.18f) * envelope;
            }
            var clip = AudioClip.Create("Original scoring fanfare", samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        static AudioClip Cue(string name, float duration, float first, float second, bool percussive)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, progress = t / duration;
                float envelope = Mathf.Min(1, t * 180) * Mathf.Pow(1 - progress, percussive ? 5 : 2);
                float wave = Mathf.Sin(2 * Mathf.PI * first * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * second * t) * 0.3f;
                if (percussive) wave += Mathf.Sin(2 * Mathf.PI * (1900 * t + 170 * t * t)) * 0.18f;
                samples[i] = wave * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void Contact(BatContact _) { source.PlayOneShot(contact); }
        void Resolved(BallClassification result) { source.PlayOneShot(result == BallClassification.Miss ? miss : success); }
        void Scored(int _) { MusicSource.Play(); }
        void StateChanged(GameState state) { if (state == GameState.Prepare) { source.Stop(); MusicSource.Stop(); } }
        void Update()
        {
            if (source == null) return;
            if (session.Paused) { source.Pause(); MusicSource.Pause(); }
            else { source.UnPause(); MusicSource.UnPause(); }
        }
        void OnDestroy()
        {
            if (session != null) { session.BallContacted -= Contact; session.PlayResolved -= Resolved; }
            if (session != null) { session.RunsScored -= Scored; session.StateChanged -= StateChanged; }
            if (contact != null) Destroy(contact); if (success != null) Destroy(success); if (miss != null) Destroy(miss);
            if (generatedMusic != null) Destroy(generatedMusic);
        }
    }
}
