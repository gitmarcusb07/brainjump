using UnityEngine;
using TMPro;

namespace BrainJump
{
    /// <summary>
    /// Central hub for visual feedback: particle bursts (dust, confetti, explosions, sparkles, stars),
    /// camera shake / FOV kicks, and pooled floating world-space score popups.
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        /// <summary>
        /// The single active EffectsManager instance, or null if none exists.
        /// </summary>
        public static EffectsManager Instance { get; private set; }

        [Header("Particle Material")]
        [Tooltip("Shared material for all particle quads. Assigned by the SceneBuilder; falls back to a URP Lit material created in code.")]
        [SerializeField] private Material particleMaterial;

        // Palette
        private static readonly Color OffWhite = Hex(0xF7, 0xF4, 0xEC);
        private static readonly Color Gold = Hex(0xF2, 0xB8, 0x5B);
        private static readonly Color Coral = Hex(0xE9, 0x82, 0x70);
        private static readonly Color Sky = Hex(0x75, 0xB9, 0xD4);
        private static readonly Color WarmGold = Hex(0xF7, 0xC8, 0x73);

        // Particle systems
        private ParticleSystem _dust;
        private ParticleSystem _confettiGold;
        private ParticleSystem _confettiCoral;
        private ParticleSystem _confettiSky;
        private ParticleSystem _explosion;
        private ParticleSystem _sparkle;
        private ParticleSystem _star;

        // Popup pool
        private const int PopupPoolSize = 4;
        private const float PopupLifetime = 0.9f;
        private const float PopupRiseSpeed = 1.5f;

        private struct PopupEntry
        {
            public GameObject go;
            public TextMeshPro tmp;
            public float life;
        }

        private PopupEntry[] _popups;

        // Stored handlers so they can be unsubscribed
        private System.Action<int> _onPointsGained;
        private System.Action<Vector3> _onCoinCollected;
        private System.Action<PowerUpType> _onPowerUpGranted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _dust = CreateBurstSystem("Dust", OffWhite, 0.25f, 2f, 0.6f, 0.2f);
            _confettiGold = CreateBurstSystem("ConfettiGold", Gold, 0.22f, 5f, 1.0f, 1f);
            _confettiCoral = CreateBurstSystem("ConfettiCoral", Coral, 0.22f, 5f, 1.0f, 1f);
            _confettiSky = CreateBurstSystem("ConfettiSky", Sky, 0.22f, 5f, 1.0f, 1f);
            _explosion = CreateBurstSystem("Explosion", Coral, 0.35f, 7f, 0.8f, 1f);
            _sparkle = CreateBurstSystem("Sparkle", Gold, 0.18f, 3f, 0.5f, 0f);
            _star = CreateBurstSystem("Star", WarmGold, 0.3f, 4f, 0.9f, 0.3f);

            CreatePopupPool();
        }

        private void Start()
        {
            if (Instance != this) return;

            _onPointsGained = points => ShowPopup("+" + points, PopupPosition(), Gold);
            _onCoinCollected = pos => PlayCoinSparkle(pos);
            _onPowerUpGranted = type => PlayStarBurst(PopupPosition());

            ScoreManager score = ScoreManager.Instance;
            if (score != null) score.OnPointsGained += _onPointsGained;

            CoinManager coins = CoinManager.Instance;
            if (coins != null) coins.OnCoinCollected += _onCoinCollected;

            PowerUpManager powerUps = PowerUpManager.Instance;
            if (powerUps != null) powerUps.OnPowerUpGranted += _onPowerUpGranted;
        }

        private void OnDestroy()
        {
            if (_onPointsGained != null)
            {
                ScoreManager score = ScoreManager.Instance;
                if (score != null) score.OnPointsGained -= _onPointsGained;
            }
            if (_onCoinCollected != null)
            {
                CoinManager coins = CoinManager.Instance;
                if (coins != null) coins.OnCoinCollected -= _onCoinCollected;
            }
            if (_onPowerUpGranted != null)
            {
                PowerUpManager powerUps = PowerUpManager.Instance;
                if (powerUps != null) powerUps.OnPowerUpGranted -= _onPowerUpGranted;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_popups == null) return;

            float dt = Time.deltaTime;
            Camera cam = Camera.main;

            for (int i = 0; i < _popups.Length; i++)
            {
                PopupEntry entry = _popups[i];
                if (entry.go == null || !entry.go.activeSelf) continue;

                entry.life -= dt;
                if (entry.life <= 0f)
                {
                    entry.go.SetActive(false);
                }
                else
                {
                    entry.go.transform.position += Vector3.up * PopupRiseSpeed * dt;
                    if (cam != null)
                    {
                        entry.go.transform.rotation = cam.transform.rotation;
                    }
                    if (entry.tmp != null)
                    {
                        Color c = entry.tmp.color;
                        c.a = Mathf.Clamp01(entry.life / PopupLifetime);
                        entry.tmp.color = c;
                    }
                }
                _popups[i] = entry;
            }
        }

        /// <summary>
        /// Assigns the shared particle material (called by the SceneBuilder). Ignores null.
        /// </summary>
        /// <param name="mat">Material to use for particle quads.</param>
        public void WireMaterial(Material mat)
        {
            if (mat != null) particleMaterial = mat;
        }

        /// <summary>
        /// Plays a small dust puff (e.g. on landing).
        /// </summary>
        /// <param name="position">World position of the puff.</param>
        public void PlayDust(Vector3 position)
        {
            Burst(_dust, position, 10);
        }

        /// <summary>
        /// Plays a multicoloured confetti burst (gold, coral and sky).
        /// </summary>
        /// <param name="position">World position of the burst.</param>
        public void PlayConfetti(Vector3 position)
        {
            Burst(_confettiGold, position, 20);
            Burst(_confettiCoral, position, 16);
            Burst(_confettiSky, position, 16);
        }

        /// <summary>
        /// Plays an explosion burst and shakes the camera.
        /// </summary>
        /// <param name="position">World position of the explosion.</param>
        public void PlayExplosion(Vector3 position)
        {
            Burst(_explosion, position, 30);
            ShakeCamera(0.5f, 0.4f);
        }

        /// <summary>
        /// Plays a small golden sparkle (e.g. on coin pickup).
        /// </summary>
        /// <param name="position">World position of the sparkle.</param>
        public void PlayCoinSparkle(Vector3 position)
        {
            Burst(_sparkle, position, 8);
        }

        /// <summary>
        /// Plays a warm-gold star burst (e.g. on power-up grant).
        /// </summary>
        /// <param name="position">World position of the burst.</param>
        public void PlayStarBurst(Vector3 position)
        {
            Burst(_star, position, 14);
        }

        /// <summary>
        /// Adds camera shake via the CameraFollow on the main camera, if present.
        /// </summary>
        /// <param name="intensity">Shake intensity.</param>
        /// <param name="duration">Shake duration in seconds.</param>
        public void ShakeCamera(float intensity, float duration)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) return;
            follow.AddShake(intensity, duration);
        }

        /// <summary>
        /// Applies a field-of-view kick via the CameraFollow on the main camera, if present.
        /// </summary>
        /// <param name="amount">FOV kick amount in degrees.</param>
        public void FovKick(float amount)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) return;
            follow.FovKick(amount);
        }

        /// <summary>
        /// Shows a floating, billboarded, fading text popup at a world position using a pooled TextMeshPro.
        /// </summary>
        /// <param name="text">Text to display.</param>
        /// <param name="worldPosition">World position where the popup starts.</param>
        /// <param name="color">Text colour (alpha fades over the popup lifetime).</param>
        public void ShowPopup(string text, Vector3 worldPosition, Color color)
        {
            if (_popups == null || _popups.Length == 0) return;

            int index = 0;
            for (int i = 0; i < _popups.Length; i++)
            {
                if (_popups[i].go != null && !_popups[i].go.activeSelf)
                {
                    index = i;
                    break;
                }
            }

            PopupEntry entry = _popups[index];
            if (entry.go == null || entry.tmp == null) return;

            entry.tmp.text = text;
            entry.tmp.color = color;
            entry.go.transform.position = worldPosition;
            Camera cam = Camera.main;
            if (cam != null)
            {
                entry.go.transform.rotation = cam.transform.rotation;
            }
            entry.life = PopupLifetime;
            entry.go.SetActive(true);
            _popups[index] = entry;
        }

        private void CreatePopupPool()
        {
            _popups = new PopupEntry[PopupPoolSize];
            for (int i = 0; i < PopupPoolSize; i++)
            {
                GameObject go = new GameObject("ScorePopup" + i);
                go.transform.SetParent(transform, false);
                TextMeshPro tmp = go.AddComponent<TextMeshPro>();
                if (tmp != null)
                {
                    tmp.fontSize = 48f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.raycastTarget = false;
                }
                go.SetActive(false);

                _popups[i] = new PopupEntry { go = go, tmp = tmp, life = 0f };
            }
        }

        private ParticleSystem CreateBurstSystem(string name, Color color, float size, float speed, float lifetime, float gravity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            if (ps == null) return null;

            var main = ps.main;
            main.duration = lifetime;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = ResolveParticleMaterial();
            }

            ps.gameObject.SetActive(true);
            return ps;
        }

        private Material ResolveParticleMaterial()
        {
            if (particleMaterial != null) return particleMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            particleMaterial = new Material(shader);
            return particleMaterial;
        }

        private void Burst(ParticleSystem ps, Vector3 position, int count)
        {
            if (ps == null) return;
            ps.transform.position = position;
            ps.Emit(count);
        }

        private Vector3 PopupPosition()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            return player != null ? player.transform.position + Vector3.up * 2.2f : Vector3.zero;
        }

        private static Color Hex(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
