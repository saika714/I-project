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
        AudioClip contact, success, miss;
        public void Initialize(GameSessionController game, float volume)
        {
            session = game;
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = Mathf.Clamp01(volume);
            contact = Cue("Bat contact", 0.13f, 740, 1180, true);
            success = Cue("Result chime", 0.40f, 523.25f, 783.99f, false);
            miss = Cue("Next chance", 0.19f, 330, 261.63f, false);
            session.BallContacted += Contact; session.PlayResolved += Resolved;
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
        void Update() { if (source != null) { if (session.Paused) source.Pause(); else source.UnPause(); } }
        void OnDestroy()
        {
            if (session != null) { session.BallContacted -= Contact; session.PlayResolved -= Resolved; }
            if (contact != null) Destroy(contact); if (success != null) Destroy(success); if (miss != null) Destroy(miss);
        }
    }
}
