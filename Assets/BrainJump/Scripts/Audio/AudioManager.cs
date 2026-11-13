using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Game-wide sound player. Every clip is synthesised in code (44.1 kHz, mono) so the
    /// project ships with no audio assets at all. Clips are generated lazily on first use
    /// and cached afterwards; a small pool of AudioSources allows overlapping sounds.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>Current active instance. May be null (or a destroyed object) — callers use Instance?.</summary>
        public static AudioManager Instance { get; private set; }

        [Header("Mix")]
        [Tooltip("Global volume multiplier applied to every sound.")]
        [SerializeField] private float masterVolume = 0.8f;

        [Tooltip("When true every sound is played silently.")]
        [SerializeField] private bool muted = false;

        /// <summary>True while the game is muted.</summary>
        public bool IsMuted => muted;

        /// <summary>Global volume multiplier (0..1).</summary>
        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        // ------------------------------------------------------------------ constants

        private const int SampleRate = 44100;          // Hz, mono
        private const float FadeSeconds = 0.005f;      // short fade in/out to avoid clicks
        private const int SourcePoolSize = 4;          // simultaneous sounds

        // ------------------------------------------------------------------ state

        private AudioSource[] _sources;

        // Lazily generated, cached clips — never rebuilt while the game runs.
        private AudioClip _clickClip;
        private AudioClip _jumpClip;
        private AudioClip _landClip;
        private AudioClip _correctClip;
        private AudioClip _wrongClip;
        private AudioClip _explosionClip;
        private AudioClip _winClip;

        /// <summary>True while this component is still usable (not destroyed and initialised).</summary>
        private bool IsAlive => this != null && _sources != null;

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Duplicate manager — drop it.
                Destroy(gameObject);
                return;
            }

            BuildSourcePool();
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Creates the fixed pool of child AudioSources used to play overlapping sounds.</summary>
        private void BuildSourcePool()
        {
            _sources = new AudioSource[SourcePoolSize];

            for (int i = 0; i < SourcePoolSize; i++)
            {
                GameObject sourceObject = new GameObject("SfxSource" + i);
                sourceObject.transform.SetParent(transform, false);

                AudioSource source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;      // pure 2D audio
                source.volume = masterVolume;

                _sources[i] = source;
            }
        }

        // ------------------------------------------------------------------ mix control

        /// <summary>Mutes or unmutes all sounds. Already playing sources are silenced immediately.</summary>
        public void SetMuted(bool value)
        {
            muted = value;

            if (_sources == null)
            {
                return;
            }

            for (int i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] != null)
                {
                    _sources[i].volume = muted ? 0f : masterVolume;
                }
            }
        }

        /// <summary>Flips the mute state.</summary>
        public void ToggleMute()
        {
            SetMuted(!muted);
        }

        // ------------------------------------------------------------------ public playback API

        /// <summary>Short UI blip.</summary>
        public void PlayClick()
        {
            if (!IsAlive) return;

            if (_clickClip == null)
            {
                _clickClip = MakeTone(new[] { 880f }, new[] { 0.06f }, 0.70f);
            }

            PlayClip(_clickClip);
        }

        /// <summary>Rising two-note sweep approximating a jump.</summary>
        public void PlayJump()
        {
            if (!IsAlive) return;

            if (_jumpClip == null)
            {
                _jumpClip = MakeTone(new[] { 300f, 600f }, new[] { 0.08f, 0.08f }, 0.70f);
            }

            PlayClip(_jumpClip);
        }

        /// <summary>Low thud with a short noise transient.</summary>
        public void PlayLand()
        {
            if (!IsAlive) return;

            if (_landClip == null)
            {
                _landClip = BuildLandClip();
            }

            PlayClip(_landClip);
        }

        /// <summary>Happy ascending arpeggio for a correct answer.</summary>
        public void PlayCorrect()
        {
            if (!IsAlive) return;

            if (_correctClip == null)
            {
                _correctClip = MakeTone(
                    new[] { 523f, 659f, 784f, 1047f },
                    new[] { 0.09f, 0.09f, 0.09f, 0.09f },
                    0.65f);
            }

            PlayClip(_correctClip);
        }

        /// <summary>Descending buzz for a wrong answer.</summary>
        public void PlayWrong()
        {
            if (!IsAlive) return;

            if (_wrongClip == null)
            {
                _wrongClip = MakeTone(
                    new[] { 220f, 180f, 140f },
                    new[] { 0.12f, 0.12f, 0.12f },
                    0.60f);
            }

            PlayClip(_wrongClip);
        }

        /// <summary>Noise burst with a low rumble underneath — the player exploding.</summary>
        public void PlayExplosion()
        {
            if (!IsAlive) return;

            if (_explosionClip == null)
            {
                _explosionClip = BuildExplosionClip();
            }

            PlayClip(_explosionClip);
        }

        /// <summary>Five-note fanfare used when a level is completed.</summary>
        public void PlayWin()
        {
            if (!IsAlive) return;

            if (_winClip == null)
            {
                _winClip = MakeTone(
                    new[] { 523f, 659f, 784f, 1047f, 1319f },
                    new[] { 0.12f, 0.12f, 0.12f, 0.12f, 0.12f },
                    0.70f);
            }

            PlayClip(_winClip);
        }

        // ------------------------------------------------------------------ playback core

        /// <summary>
        /// Grabs the first idle source from the pool (or the first one if all are busy),
        /// assigns the clip and plays it at the requested volume.
        /// </summary>
        private void PlayClip(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _sources == null || _sources.Length == 0)
            {
                return;
            }

            AudioSource source = null;

            for (int i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] != null && !_sources[i].isPlaying)
                {
                    source = _sources[i];
                    break;
                }
            }

            if (source == null)
            {
                // Everything is busy — steal the first available source.
                for (int i = 0; i < _sources.Length; i++)
                {
                    if (_sources[i] != null)
                    {
                        source = _sources[i];
                        break;
                    }
                }
            }

            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = clip;
            source.volume = muted ? 0f : Mathf.Clamp01(masterVolume * volumeScale);
            source.Play();
        }

        // ------------------------------------------------------------------ synthesis

        /// <summary>
        /// Builds a clip from parallel frequency/duration arrays — one sine note per entry.
        /// Each note gets a short fade in/out so note boundaries never click.
        /// When <paramref name="noise"/> is true the buffer is filled with low-passed random
        /// noise instead of sine tones (same envelope shape).
        /// </summary>
        private AudioClip MakeTone(float[] frequencies, float[] durations, float volume, bool noise = false)
        {
            if (frequencies == null || durations == null)
            {
                return null;
            }

            int noteCount = Mathf.Min(frequencies.Length, durations.Length);
            if (noteCount == 0)
            {
                return null;
            }

            // Pre-compute the exact sample length so the buffer never needs resizing.
            int totalSamples = 0;
            for (int n = 0; n < noteCount; n++)
            {
                totalSamples += Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0.01f, durations[n]) * SampleRate));
            }

            if (totalSamples <= 0)
            {
                return null;
            }

            float[] data = new float[totalSamples];
            int cursor = 0;
            float noiseState = 0f;

            for (int n = 0; n < noteCount; n++)
            {
                int noteSamples = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0.01f, durations[n]) * SampleRate));
                int fadeSamples = Mathf.Clamp(
                    Mathf.RoundToInt(FadeSeconds * SampleRate),
                    1,
                    Mathf.Max(1, noteSamples / 2));

                float frequency = frequencies[n];
                double phaseStep = 2.0 * Mathf.PI * frequency / SampleRate;

                for (int s = 0; s < noteSamples; s++)
                {
                    // Trapezoid envelope: quick fade in, quick fade out.
                    float envelope = 1f;
                    if (s < fadeSamples)
                    {
                        envelope = (float)s / fadeSamples;
                    }
                    else if (s > noteSamples - fadeSamples)
                    {
                        envelope = (float)(noteSamples - s) / fadeSamples;
                    }

                    envelope = Mathf.Clamp01(envelope);

                    float sample;
                    if (noise)
                    {
                        float white = Random.Range(-1f, 1f);
                        noiseState = Mathf.Lerp(noiseState, white, 0.25f); // one-pole low-pass
                        sample = noiseState;
                    }
                    else
                    {
                        sample = Mathf.Sin((float)(phaseStep * s));
                    }

                    data[cursor + s] = Mathf.Clamp(sample * volume * envelope, -1f, 1f);
                }

                cursor += noteSamples;
            }

            return CreateClip(noise ? "BJ_NoiseTone" : "BJ_Tone", data);
        }

        /// <summary>White noise with a fast exponential decay — the raw material for the explosion.</summary>
        private AudioClip MakeNoiseBurst(float duration, float volume)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0.01f, duration) * SampleRate));
            int fadeInSamples = Mathf.Clamp(Mathf.RoundToInt(FadeSeconds * SampleRate), 1, sampleCount);

            float[] data = new float[sampleCount];
            float state = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-4.5f * t); // exponential decay

                if (i < fadeInSamples)
                {
                    envelope *= (float)i / fadeInSamples; // tiny attack, no click
                }

                float white = Random.Range(-1f, 1f);
                state = Mathf.Lerp(state, white, 0.30f); // soften the hiss into a "boom"

                data[i] = Mathf.Clamp(state * envelope * volume, -1f, 1f);
            }

            return CreateClip("BJ_NoiseBurst", data);
        }

        /// <summary>120 Hz thud with a very short noise transient mixed in for grit.</summary>
        private AudioClip BuildLandClip()
        {
            AudioClip tone = MakeTone(new[] { 120f }, new[] { 0.12f }, 0.90f);
            if (tone == null)
            {
                return null;
            }

            float[] data = new float[tone.samples];
            tone.GetData(data, 0);
            Destroy(tone); // temporary clip, keep memory flat on mobile

            int sampleCount = data.Length;
            float noiseState = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount > 1 ? (float)i / (sampleCount - 1) : 0f;
                float white = Random.Range(-1f, 1f);
                noiseState = Mathf.Lerp(noiseState, white, 0.20f);

                float transient = noiseState * 0.35f * Mathf.Exp(-18f * t);
                data[i] = Mathf.Clamp(data[i] + transient, -1f, 1f);
            }

            return CreateClip("BJ_Land", data);
        }

        /// <summary>Noise burst with a decaying 60 Hz rumble layered underneath.</summary>
        private AudioClip BuildExplosionClip()
        {
            AudioClip noise = MakeNoiseBurst(0.5f, 0.90f);
            if (noise == null)
            {
                return null;
            }

            float[] data = new float[noise.samples];
            noise.GetData(data, 0);
            Destroy(noise);

            int sampleCount = data.Length;
            double phaseStep = 2.0 * Mathf.PI * 60.0 / SampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount > 1 ? (float)i / (sampleCount - 1) : 0f;
                float rumble = Mathf.Sin((float)(phaseStep * i)) * 0.55f * Mathf.Exp(-3.5f * t);
                data[i] = Mathf.Clamp(data[i] + rumble, -1f, 1f);
            }

            return CreateClip("BJ_Explosion", data);
        }

        /// <summary>Wraps a sample buffer into a 44.1 kHz mono (non-streaming) AudioClip.</summary>
        private static AudioClip CreateClip(string clipName, float[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
            if (clip == null)
            {
                return null;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
